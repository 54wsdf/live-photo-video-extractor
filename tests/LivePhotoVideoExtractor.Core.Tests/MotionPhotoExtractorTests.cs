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
    public async Task ExtractAsync_uses_orientation_normalized_video_when_required()
    {
        using var directory = new TestDirectory();
        var rawMp4 = TestMediaFactory.MinimalMp4();
        var normalizedMp4 = TestMediaFactory.Combine(
            TestMediaFactory.Box("ftyp", "isom\0\0\0\0"u8.ToArray()),
            TestMediaFactory.Box("moov", Array.Empty<byte>()),
            TestMediaFactory.Box("mdat", new byte[] { 9, 8, 7, 6 }));
        var sourcePath = directory.Write(
            "landscape.jpg",
            TestMediaFactory.JpegWithTrailer(rawMp4));
        var normalizer = new TestOrientationNormalizer(normalizedMp4);
        var extractor = new MotionPhotoExtractor(normalizer);

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.Success, result.Status);
        Assert.True(normalizer.WasCalled);
        Assert.Contains("校正方向", result.Message);
        Assert.Equal(
            normalizedMp4,
            await File.ReadAllBytesAsync(
                Assert.IsType<string>(result.OutputPath),
                TestContext.Current.CancellationToken));
        Assert.DoesNotContain(
            Directory.GetFiles(directory.Path),
            path => Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExtractAsync_keeps_raw_video_when_normalizer_reports_no_rotation()
    {
        using var directory = new TestDirectory();
        var rawMp4 = TestMediaFactory.MinimalMp4();
        var sourcePath = directory.Write(
            "portrait.jpg",
            TestMediaFactory.JpegWithTrailer(rawMp4));
        var normalizer = new TestOrientationNormalizer(rawMp4, normalize: false);
        var extractor = new MotionPhotoExtractor(normalizer);

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.Success, result.Status);
        Assert.True(normalizer.WasCalled);
        Assert.Contains("无损导出", result.Message);
        Assert.Equal(
            rawMp4,
            await File.ReadAllBytesAsync(
                Assert.IsType<string>(result.OutputPath),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExtractAsync_cleans_temporary_files_after_normalization_failure()
    {
        using var directory = new TestDirectory();
        var mp4 = TestMediaFactory.MinimalMp4();
        var sourcePath = directory.Write(
            "landscape.jpg",
            TestMediaFactory.JpegWithTrailer(mp4));
        var normalizer = new TestOrientationNormalizer(
            mp4,
            failAfterWrite: true);
        var extractor = new MotionPhotoExtractor(normalizer);

        var result = await extractor.ExtractAsync(
            sourcePath,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionStatus.Failed, result.Status);
        Assert.Null(result.OutputPath);
        Assert.DoesNotContain(
            Directory.GetFiles(directory.Path),
            path => Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal));
        Assert.True(File.Exists(sourcePath));
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

    private sealed class TestOrientationNormalizer(
        byte[] normalizedMp4,
        bool normalize = true,
        bool failAfterWrite = false)
        : IVideoOrientationNormalizer
    {
        public bool WasCalled { get; private set; }

        public async Task<bool> NormalizeIfNeededAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            if (!normalize)
            {
                return false;
            }

            await File.WriteAllBytesAsync(destinationPath, normalizedMp4, cancellationToken);
            if (failAfterWrite)
            {
                throw new InvalidDataException("Synthetic normalization failure.");
            }

            return true;
        }
    }
}
