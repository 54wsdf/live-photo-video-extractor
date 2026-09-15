namespace LivePhotoVideoExtractor.Core;

public enum ExtractionStatus
{
    Success,
    NoEmbeddedVideo,
    UnsupportedInput,
    Failed,
}

public sealed record ExtractionResult(
    string SourcePath,
    string? OutputPath,
    ExtractionStatus Status,
    string Message);
