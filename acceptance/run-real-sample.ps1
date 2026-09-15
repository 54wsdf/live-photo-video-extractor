[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceJpeg,

    [Parameter(Mandatory = $true)]
    [string]$ScratchDirectory,

    [string]$ExtractorExe
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourcePath = [IO.Path]::GetFullPath($SourceJpeg)
$scratchPath = [IO.Path]::GetFullPath($ScratchDirectory)

if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Source JPEG not found: $sourcePath"
}

if (Test-Path -LiteralPath $scratchPath) {
    throw "Scratch directory must not already exist: $scratchPath"
}

if ([string]::IsNullOrWhiteSpace($ExtractorExe)) {
    $publishedExecutables = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'artifacts\publish') -Filter '*.exe' -File)
    if ($publishedExecutables.Count -ne 1) {
        throw "Expected exactly one published executable, found $($publishedExecutables.Count)."
    }

    $extractorPath = $publishedExecutables[0].FullName
} else {
    $extractorPath = [IO.Path]::GetFullPath($ExtractorExe)
}

if (-not (Test-Path -LiteralPath $extractorPath -PathType Leaf)) {
    throw "Extractor executable not found: $extractorPath"
}

New-Item -ItemType Directory -Path $scratchPath | Out-Null
$hashBefore = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$copiedSource = Join-Path $scratchPath ([IO.Path]::GetFileName($sourcePath))
Copy-Item -LiteralPath $sourcePath -Destination $copiedSource
$copiedHash = (Get-FileHash -LiteralPath $copiedSource -Algorithm SHA256).Hash
if ($copiedHash -ne $hashBefore) {
    throw 'Scratch copy hash does not match the source.'
}

$quotedSource = '"' + $copiedSource + '"'
$process = Start-Process `
    -FilePath $extractorPath `
    -ArgumentList @('--extract', $quotedSource) `
    -Wait `
    -PassThru `
    -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    throw "Packaged extractor failed with exit code $($process.ExitCode)."
}

$outputPath = Join-Path $scratchPath ([IO.Path]::GetFileNameWithoutExtension($copiedSource) + '.mp4')
if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
    throw "Expected MP4 was not created: $outputPath"
}

$bytes = [IO.File]::ReadAllBytes($outputPath)
if ($bytes.Length -lt 24) {
    throw 'Extracted MP4 is unexpectedly small.'
}

function Get-UInt32BigEndian {
    param([byte[]]$Data, [int]$Offset)
    return [uint32](
        ([uint32]$Data[$Offset] -shl 24) -bor
        ([uint32]$Data[$Offset + 1] -shl 16) -bor
        ([uint32]$Data[$Offset + 2] -shl 8) -bor
        [uint32]$Data[$Offset + 3])
}

function Get-UInt64BigEndian {
    param([byte[]]$Data, [int]$Offset)
    $high = [uint64](Get-UInt32BigEndian -Data $Data -Offset $Offset)
    $low = [uint64](Get-UInt32BigEndian -Data $Data -Offset ($Offset + 4))
    return ($high -shl 32) -bor $low
}

$offset = [int64]0
$foundFtyp = $false
$foundMoov = $false
$foundMdat = $false
$boxIndex = 0
while ($bytes.Length - $offset -ge 8) {
    $shortSize = Get-UInt32BigEndian -Data $bytes -Offset ([int]$offset)
    $type = [Text.Encoding]::ASCII.GetString($bytes, [int]$offset + 4, 4)
    $headerSize = [int64]8

    if ($shortSize -eq 1) {
        if ($bytes.Length - $offset -lt 16) {
            throw 'Truncated extended-size MP4 box.'
        }

        $size = [int64](Get-UInt64BigEndian -Data $bytes -Offset ([int]$offset + 8))
        $headerSize = 16
    } elseif ($shortSize -eq 0) {
        $size = [int64]($bytes.Length - $offset)
    } else {
        $size = [int64]$shortSize
    }

    if ($size -lt $headerSize -or $size -gt $bytes.Length - $offset) {
        throw "Invalid MP4 box at offset $offset."
    }

    if ($boxIndex -eq 0 -and $type -ne 'ftyp') {
        throw 'The first MP4 box is not ftyp.'
    }

    $foundFtyp = $foundFtyp -or $type -eq 'ftyp'
    $foundMoov = $foundMoov -or $type -eq 'moov'
    $foundMdat = $foundMdat -or $type -eq 'mdat'
    $offset += $size
    $boxIndex++
}

if ($offset -ne $bytes.Length -or -not $foundFtyp -or -not $foundMoov -or -not $foundMdat) {
    throw 'Extracted MP4 did not pass the top-level box validation.'
}

$sourceTimestamp = (Get-Item -LiteralPath $copiedSource).LastWriteTime
$outputFile = Get-Item -LiteralPath $outputPath
if ($outputFile.CreationTime -ne $sourceTimestamp -or $outputFile.LastWriteTime -ne $sourceTimestamp) {
    throw 'Extracted MP4 timestamps do not match the source photo.'
}

$hashAfter = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
if ($hashAfter -ne $hashBefore) {
    throw 'Original source hash changed during acceptance testing.'
}

if ([IO.Path]::GetDirectoryName($outputPath) -eq [IO.Path]::GetDirectoryName($sourcePath)) {
    throw 'Acceptance output was written beside the original source.'
}

Write-Host 'REAL_SAMPLE_ACCEPTANCE=PASS'
Write-Host ("SOURCE_SHA256={0}" -f $hashBefore)
Write-Host ("OUTPUT={0}" -f $outputPath)
Write-Host ("OUTPUT_BYTES={0}" -f $outputFile.Length)
