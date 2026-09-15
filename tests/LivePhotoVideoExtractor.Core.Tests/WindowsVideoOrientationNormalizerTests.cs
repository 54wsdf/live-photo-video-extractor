using LivePhotoVideoExtractor.App;

namespace LivePhotoVideoExtractor.Core.Tests;

public sealed class WindowsVideoOrientationNormalizerTests
{
    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, true, 1)]
    public async Task NormalizeIfNeededAsync_only_renders_videos_with_rotation_metadata(
        bool requiresNormalization,
        bool expectedResult,
        int expectedRenderCount)
    {
        var backend = new TestOrientationBackend(requiresNormalization);
        var normalizer = new WindowsVideoOrientationNormalizer(backend);

        var result = await normalizer.NormalizeIfNeededAsync(
            "input.mp4",
            "output.mp4",
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedRenderCount, backend.RenderCount);
    }

    private sealed class TestOrientationBackend(bool requiresNormalization)
        : IWindowsVideoOrientationBackend
    {
        public int RenderCount { get; private set; }

        public Task<bool> RequiresNormalizationAsync(
            string sourcePath,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(requiresNormalization);
        }

        public Task RenderNormalizedAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            RenderCount++;
            return Task.CompletedTask;
        }
    }
}
