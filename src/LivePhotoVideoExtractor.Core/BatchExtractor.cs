namespace LivePhotoVideoExtractor.Core;

public sealed class BatchExtractor
{
    private readonly MotionPhotoExtractor _extractor;

    public BatchExtractor(MotionPhotoExtractor extractor)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    }

    public async Task<IReadOnlyList<ExtractionResult>> ExtractAsync(
        IEnumerable<string> sourcePaths,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);

        var paths = NormalizeAndDeduplicate(sourcePaths);
        var results = new List<ExtractionResult>(paths.Count);
        var successCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _extractor.ExtractAsync(path, cancellationToken).ConfigureAwait(false);
            results.Add(result);

            switch (result.Status)
            {
                case ExtractionStatus.Success:
                    successCount++;
                    break;
                case ExtractionStatus.NoEmbeddedVideo:
                case ExtractionStatus.UnsupportedInput:
                    skippedCount++;
                    break;
                case ExtractionStatus.Failed:
                    failedCount++;
                    break;
                default:
                    throw new InvalidOperationException($"未知导出状态：{result.Status}");
            }

            progress?.Report(new BatchProgress(
                results.Count,
                paths.Count,
                successCount,
                skippedCount,
                failedCount,
                result));
        }

        return results;
    }

    private static IReadOnlyList<string> NormalizeAndDeduplicate(IEnumerable<string> sourcePaths)
    {
        var paths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourcePath in sourcePaths)
        {
            var normalized = NormalizePath(sourcePath);
            if (seen.Add(normalized))
            {
                paths.Add(normalized);
            }
        }

        return paths;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path ?? string.Empty;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return path;
        }
    }
}
