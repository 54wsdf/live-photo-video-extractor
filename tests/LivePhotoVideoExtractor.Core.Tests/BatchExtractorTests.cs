namespace LivePhotoVideoExtractor.Core.Tests;

public sealed class BatchExtractorTests
{
    [Fact]
    public async Task ExtractAsync_normalizes_and_deduplicates_paths_without_changing_order()
    {
        using var directory = new TestDirectory();
        var firstPath = directory.Write(
            "first.jpg",
            TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));
        var secondPath = directory.Write("second.jpg", TestMediaFactory.JpegWithTrailer(Array.Empty<byte>()));
        var batch = new BatchExtractor(new MotionPhotoExtractor());

        var results = await batch.ExtractAsync(
            [firstPath, firstPath.ToUpperInvariant(), secondPath],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Collection(
            results,
            first => Assert.Equal(Path.GetFullPath(firstPath), first.SourcePath),
            second => Assert.Equal(Path.GetFullPath(secondPath), second.SourcePath));
    }

    [Fact]
    public async Task ExtractAsync_continues_after_one_file_fails()
    {
        using var directory = new TestDirectory();
        var missingPath = Path.Combine(directory.Path, "missing.jpg");
        var validPath = directory.Write(
            "valid.jpg",
            TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));
        var batch = new BatchExtractor(new MotionPhotoExtractor());

        var results = await batch.ExtractAsync(
            [missingPath, validPath],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Count);
        Assert.Equal(ExtractionStatus.Failed, results[0].Status);
        Assert.Equal(ExtractionStatus.Success, results[1].Status);
    }

    [Fact]
    public async Task ExtractAsync_reports_accurate_progress_counts()
    {
        using var directory = new TestDirectory();
        var validPath = directory.Write(
            "valid.jpg",
            TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));
        var stillPath = directory.Write("still.jpg", TestMediaFactory.JpegWithTrailer(Array.Empty<byte>()));
        var unsupportedPath = directory.Write("image.png", new byte[] { 1, 2, 3 });
        var missingPath = Path.Combine(directory.Path, "missing.jpg");
        var progress = new ProgressCollector();
        var batch = new BatchExtractor(new MotionPhotoExtractor());

        var results = await batch.ExtractAsync(
            [validPath, stillPath, unsupportedPath, missingPath],
            progress,
            TestContext.Current.CancellationToken);

        Assert.Equal(4, results.Count);
        Assert.Equal(4, progress.Items.Count);
        var final = progress.Items[^1];
        Assert.Equal(4, final.Completed);
        Assert.Equal(4, final.Total);
        Assert.Equal(1, final.SuccessCount);
        Assert.Equal(2, final.SkippedCount);
        Assert.Equal(1, final.FailedCount);
        Assert.Same(results[^1], final.Current);
    }

    private sealed class ProgressCollector : IProgress<BatchProgress>
    {
        public List<BatchProgress> Items { get; } = [];

        public void Report(BatchProgress value)
        {
            Items.Add(value);
        }
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"LivePhotoVideoExtractor.BatchTests-{Guid.NewGuid():N}");
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
