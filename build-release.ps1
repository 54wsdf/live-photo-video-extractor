[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$localDotnet = Join-Path $projectRoot '.dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) {
    $localDotnet
} else {
    (Get-Command dotnet.exe -ErrorAction Stop).Source
}

$solution = Join-Path $projectRoot 'LivePhotoVideoExtractor.sln'
$appProject = Join-Path $projectRoot 'src\LivePhotoVideoExtractor.App\LivePhotoVideoExtractor.App.csproj'
$publishDirectory = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts\publish'))
$expectedPrefix = $projectRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar

if (-not $publishDirectory.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe publish directory: $publishDirectory"
}

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDirectory | Out-Null
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

Write-Host 'Restoring projects...'
& $dotnet restore $solution
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}

Write-Host 'Running tests...'
& $dotnet test $solution --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE"
}

Write-Host 'Publishing standalone Windows executable...'
& $dotnet publish $appProject `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --no-restore `
    --output $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$publishedFiles = @(Get-ChildItem -LiteralPath $publishDirectory -File)
$executables = @($publishedFiles | Where-Object { $_.Extension -eq '.exe' })
if ($executables.Count -ne 1) {
    throw "Expected exactly one executable, found $($executables.Count)."
}

$publishedFile = $executables[0]
$unexpectedFiles = @($publishedFiles | Where-Object { $_.FullName -ne $publishedFile.FullName })
if ($unexpectedFiles.Count -gt 0) {
    $names = ($unexpectedFiles | Select-Object -ExpandProperty Name) -join ', '
    throw "Unexpected files in single-file publish directory: $names"
}

Write-Host ''
Write-Host 'Release build complete:'
Write-Host $publishedFile.FullName
Write-Host ("Size: {0:N0} bytes" -f $publishedFile.Length)
