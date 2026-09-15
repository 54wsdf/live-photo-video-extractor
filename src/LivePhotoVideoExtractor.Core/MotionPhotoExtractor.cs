using System.Buffers;

namespace LivePhotoVideoExtractor.Core;

internal delegate Task SegmentCopyDelegate(
    Stream source,
    Stream destination,
    long length,
    CancellationToken cancellationToken);

public sealed class MotionPhotoExtractor
{
    private const int CopyBufferSize = 128 * 1024;
    private readonly SegmentCopyDelegate _copySegmentAsync;
    private readonly IVideoOrientationNormalizer? _orientationNormalizer;

    public MotionPhotoExtractor(IVideoOrientationNormalizer? orientationNormalizer = null)
        : this(CopySegmentAsync, orientationNormalizer)
    {
    }

    internal MotionPhotoExtractor(
        SegmentCopyDelegate copySegmentAsync,
        IVideoOrientationNormalizer? orientationNormalizer = null)
    {
        _copySegmentAsync = copySegmentAsync ?? throw new ArgumentNullException(nameof(copySegmentAsync));
        _orientationNormalizer = orientationNormalizer;
    }

    public async Task<ExtractionResult> ExtractAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Failed(sourcePath ?? string.Empty, "没有提供照片路径。");
        }

        string fullSourcePath;
        try
        {
            fullSourcePath = Path.GetFullPath(sourcePath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return Failed(sourcePath, $"照片路径无效：{exception.Message}");
        }

        if (!HasSupportedExtension(fullSourcePath))
        {
            return new ExtractionResult(
                fullSourcePath,
                null,
                ExtractionStatus.UnsupportedInput,
                "仅支持 JPG/JPEG 动态照片。");
        }

        if (!File.Exists(fullSourcePath))
        {
            return Failed(fullSourcePath, "照片不存在或当前无法访问。");
        }

        string? temporaryPath = null;
        string? normalizedTemporaryPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var source = new FileStream(
                fullSourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var segment = MotionPhotoParser.Locate(source);
            if (segment is null)
            {
                return new ExtractionResult(
                    fullSourcePath,
                    null,
                    ExtractionStatus.NoEmbeddedVideo,
                    "这是普通静态照片，未找到可导出的视频。");
            }

            var directory = Path.GetDirectoryName(fullSourcePath)
                ?? throw new IOException("无法确定照片所在目录。");
            var baseName = Path.GetFileNameWithoutExtension(fullSourcePath);
            temporaryPath = Path.Combine(
                directory,
                $".{baseName}.{Guid.NewGuid():N}.livephoto.raw.mp4");

            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                CopyBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                source.Position = segment.Value.Offset;
                await _copySegmentAsync(
                    source,
                    destination,
                    segment.Value.Length,
                    cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var validationStream = new FileStream(
                temporaryPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize,
                FileOptions.SequentialScan))
            {
                if (!MotionPhotoParser.IsValidMp4(validationStream))
                {
                    throw new InvalidDataException("导出的视频结构校验失败。");
                }
            }

            var orientationNormalized = false;
            if (_orientationNormalizer is not null)
            {
                normalizedTemporaryPath = Path.Combine(
                    directory,
                    $".{baseName}.{Guid.NewGuid():N}.livephoto.normalized.mp4");
                orientationNormalized = await _orientationNormalizer.NormalizeIfNeededAsync(
                    temporaryPath,
                    normalizedTemporaryPath,
                    cancellationToken).ConfigureAwait(false);

                if (orientationNormalized)
                {
                    if (!File.Exists(normalizedTemporaryPath))
                    {
                        throw new InvalidDataException("方向校正未生成输出视频。");
                    }

                    await using var normalizedValidationStream = new FileStream(
                        normalizedTemporaryPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        CopyBufferSize,
                        FileOptions.SequentialScan);
                    if (!MotionPhotoParser.IsValidMp4(normalizedValidationStream))
                    {
                        throw new InvalidDataException("方向校正后的视频结构校验失败。");
                    }

                    TryDeleteOwnedTemporaryFile(temporaryPath);
                    temporaryPath = normalizedTemporaryPath;
                    normalizedTemporaryPath = null;
                }
            }

            var sourceTimestamp = File.GetLastWriteTime(fullSourcePath);
            File.SetCreationTime(temporaryPath, sourceTimestamp);
            File.SetLastWriteTime(temporaryPath, sourceTimestamp);

            var outputPath = MoveWithoutOverwrite(temporaryPath, directory, baseName);
            temporaryPath = null;
            return new ExtractionResult(
                fullSourcePath,
                outputPath,
                ExtractionStatus.Success,
                orientationNormalized
                    ? $"已导出并校正方向：{Path.GetFileName(outputPath)}"
                    : $"已无损导出：{Path.GetFileName(outputPath)}");
        }
        catch (OperationCanceledException)
        {
            return Failed(fullSourcePath, "处理已取消。");
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return Failed(fullSourcePath, exception.Message);
        }
        finally
        {
            if (temporaryPath is not null)
            {
                TryDeleteOwnedTemporaryFile(temporaryPath);
            }

            if (normalizedTemporaryPath is not null)
            {
                TryDeleteOwnedTemporaryFile(normalizedTemporaryPath);
            }
        }
    }

    private static bool HasSupportedExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static string MoveWithoutOverwrite(string temporaryPath, string directory, string baseName)
    {
        for (var copyNumber = 0; copyNumber < int.MaxValue; copyNumber++)
        {
            var fileName = copyNumber == 0
                ? $"{baseName}.mp4"
                : $"{baseName}_live_{copyNumber}.mp4";
            var candidate = Path.Combine(directory, fileName);

            try
            {
                File.Move(temporaryPath, candidate, overwrite: false);
                return candidate;
            }
            catch (IOException) when (File.Exists(candidate))
            {
                // Another file already owns this name. Try the next numbered name.
            }
        }

        throw new IOException("无法为导出视频分配可用文件名。");
    }

    private static async Task CopySegmentAsync(
        Stream source,
        Stream destination,
        long length,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            var remaining = length;
            while (remaining > 0)
            {
                var requested = (int)Math.Min(buffer.Length, remaining);
                var bytesRead = await source.ReadAsync(
                    buffer.AsMemory(0, requested),
                    cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("动态照片的视频数据不完整。");
                }

                await destination.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken).ConfigureAwait(false);
                remaining -= bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void TryDeleteOwnedTemporaryFile(string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static ExtractionResult Failed(string sourcePath, string message)
    {
        return new ExtractionResult(sourcePath, null, ExtractionStatus.Failed, message);
    }
}
