using System.Security.Cryptography;

namespace LivePhotoVideoExtractor.Core.Tests;

public sealed class MotionPhotoExtractorTests
{
    [Fact]
    public async Task ExtractAsync_writes_exact_mp4_bytes_beside_source()
    {
        using var directory = new TestDirectory();
        var mp4 = TestMediaFactory.MinimalMp4();
        var sourcePath = directory.Write("photo.jpg", TestMediaFactory.JpegWithTrailer(mp4));
        var extractor = new MotionPhotoExtractor();

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.Success, result.Status);
        var outputPath = Assert.IsType<string>(result.OutputPath);
        Assert.Equal(Path.Combine(directory.Path, "photo.mp4"), outputPath);
        Assert.Equal(
            mp4,
            await File.ReadAllBytesAsync(outputPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExtractAsync_numbers_output_without_overwriting_existing_video()
    {
        using var directory = new TestDirectory();
        var sourcePath = directory.Write(
            "photo.jpg",
            TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));
        var existingBytes = new byte[] { 9, 8, 7 };
        var existingPath = directory.Write("photo.mp4", existingBytes);
        var extractor = new MotionPhotoExtractor();

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.Success, result.Status);
        Assert.Equal(Path.Combine(directory.Path, "photo_live_1.mp4"), result.OutputPath);
        Assert.Equal(
            existingBytes,
            await File.ReadAllBytesAsync(existingPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExtractAsync_preserves_source_and_copies_its_timestamp()
    {
        using var directory = new TestDirectory();
        var sourcePath = directory.Write(
            "photo.jpeg",
            TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));
        var timestamp = new DateTime(2026, 5, 4, 18, 5, 25, DateTimeKind.Local);
        File.SetLastWriteTime(sourcePath, timestamp);
        var hashBefore = await Sha256Async(
            sourcePath,
            TestContext.Current.CancellationToken);
        var extractor = new MotionPhotoExtractor();

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.Success, result.Status);
        Assert.Equal(
            hashBefore,
            await Sha256Async(sourcePath, TestContext.Current.CancellationToken));
        var outputPath = Assert.IsType<string>(result.OutputPath);
        Assert.Equal(File.GetLastWriteTime(sourcePath), File.GetLastWriteTime(outputPath));
        Assert.Equal(File.GetLastWriteTime(sourcePath), File.GetCreationTime(outputPath));
    }

    [Fact]
    public async Task ExtractAsync_removes_owned_temporary_file_after_copy_failure()
    {
        using var directory = new TestDirectory();
        var sourcePath = directory.Write(
            "photo.jpg",
            TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));
        var extractor = new MotionPhotoExtractor(
            async (_, destination, _, cancellationToken) =>
            {
                await destination.WriteAsync(new byte[] { 1, 2 }, cancellationToken);
                throw new IOException("Synthetic copy failure.");
            });

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.Failed, result.Status);
        Assert.Null(result.OutputPath);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.livephoto.tmp"));
        Assert.True(File.Exists(sourcePath));
    }

    [Fact]
    public async Task ExtractAsync_reports_no_video_for_static_jpeg()
    {
        using var directory = new TestDirectory();
        var sourcePath = directory.Write("still.jpg", TestMediaFactory.JpegWithTrailer(Array.Empty<byte>()));
        var extractor = new MotionPhotoExtractor();

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.NoEmbeddedVideo, result.Status);
        Assert.Null(result.OutputPath);
        Assert.Single(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task ExtractAsync_rejects_non_jpeg_input()
    {
        using var directory = new TestDirectory();
        var sourcePath = directory.Write("photo.png", TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));
        var extractor = new MotionPhotoExtractor();

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.UnsupportedInput, result.Status);
        Assert.Null(result.OutputPath);
    }

    private static async Task<string> Sha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"LivePhotoVideoExtractor.Tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Write(string fileName, byte[] bytes)
        {
            var path = System.IO.Path.Combine(Path, fileName);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
