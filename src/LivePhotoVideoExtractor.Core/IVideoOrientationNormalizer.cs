namespace LivePhotoVideoExtractor.Core;

public interface IVideoOrientationNormalizer
{
    Task<bool> NormalizeIfNeededAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken);
}
