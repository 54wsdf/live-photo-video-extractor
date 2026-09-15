namespace LivePhotoVideoExtractor.Core;

public sealed record BatchProgress(
    int Completed,
    int Total,
    int SuccessCount,
    int SkippedCount,
    int FailedCount,
    ExtractionResult Current);
