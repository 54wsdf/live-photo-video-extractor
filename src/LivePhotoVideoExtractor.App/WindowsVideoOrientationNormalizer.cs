using LivePhotoVideoExtractor.Core;
using Windows.Media.Editing;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace LivePhotoVideoExtractor.App;

public sealed class WindowsVideoOrientationNormalizer : IVideoOrientationNormalizer
{
    private readonly IWindowsVideoOrientationBackend _backend;

    public WindowsVideoOrientationNormalizer()
        : this(new WindowsMediaVideoOrientationBackend())
    {
    }

    internal WindowsVideoOrientationNormalizer(IWindowsVideoOrientationBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    public async Task<bool> NormalizeIfNeededAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        if (!await _backend.RequiresNormalizationAsync(
                sourcePath,
                cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        await _backend.RenderNormalizedAsync(
            sourcePath,
            destinationPath,
            cancellationToken).ConfigureAwait(false);
        return true;
    }
}

internal interface IWindowsVideoOrientationBackend
{
    Task<bool> RequiresNormalizationAsync(
        string sourcePath,
        CancellationToken cancellationToken);

    Task RenderNormalizedAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken);
}

internal sealed class WindowsMediaVideoOrientationBackend : IWindowsVideoOrientationBackend
{
    public async Task<bool> RequiresNormalizationAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var source = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(sourcePath));
            var properties = await source.Properties.GetVideoPropertiesAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return properties.Orientation != VideoOrientation.Normal;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"无法读取视频方向：{exception.Message}",
                exception);
        }
    }

    public async Task RenderNormalizedAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var source = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(sourcePath));
            var fullDestinationPath = Path.GetFullPath(destinationPath);
            var destinationFolder = await StorageFolder.GetFolderFromPathAsync(
                Path.GetDirectoryName(fullDestinationPath)
                    ?? throw new IOException("无法确定方向校正文件的目录。"));
            var destination = await destinationFolder.CreateFileAsync(
                Path.GetFileName(fullDestinationPath),
                CreationCollisionOption.FailIfExists);

            var clip = await MediaClip.CreateFromFileAsync(source);
            var videoProperties = clip.GetVideoEncodingProperties();
            if (videoProperties.Width == 0 || videoProperties.Height == 0)
            {
                throw new InvalidDataException("无法读取视频画面尺寸。");
            }

            var composition = new MediaComposition();
            composition.Clips.Add(clip);
            var profile = composition.CreateDefaultEncodingProfile();
            profile.Video.Width = videoProperties.Width;
            profile.Video.Height = videoProperties.Height;

            if (videoProperties.Bitrate > 0)
            {
                profile.Video.Bitrate = videoProperties.Bitrate;
            }

            if (videoProperties.FrameRate.Numerator > 0
                && videoProperties.FrameRate.Denominator > 0)
            {
                profile.Video.FrameRate.Numerator = videoProperties.FrameRate.Numerator;
                profile.Video.FrameRate.Denominator = videoProperties.FrameRate.Denominator;
            }

            if (videoProperties.PixelAspectRatio.Numerator > 0
                && videoProperties.PixelAspectRatio.Denominator > 0)
            {
                profile.Video.PixelAspectRatio.Numerator = videoProperties.PixelAspectRatio.Numerator;
                profile.Video.PixelAspectRatio.Denominator = videoProperties.PixelAspectRatio.Denominator;
            }

            var failureReason = await composition.RenderToFileAsync(
                destination,
                MediaTrimmingPreference.Precise,
                profile);
            cancellationToken.ThrowIfCancellationRequested();
            if (failureReason != TranscodeFailureReason.None)
            {
                throw new InvalidDataException($"Windows 视频方向校正失败：{failureReason}");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"无法校正视频方向：{exception.Message}",
                exception);
        }
    }
}
