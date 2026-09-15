using LivePhotoVideoExtractor.App;

namespace LivePhotoVideoExtractor.Core.Tests;

public sealed class CommandLineRunnerTests
{
    [Fact]
    public async Task RunAsync_extracts_a_photo_in_automation_mode()
    {
        using var directory = new TestDirectory();
        var sourcePath = directory.Write(
            "sample.jpg",
            TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));

        var exitCode = await CommandLineRunner.RunAsync(["--extract", sourcePath]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(directory.Path, "sample.mp4")));
    }

    [Fact]
    public async Task RunAsync_rejects_unknown_arguments_without_writing_files()
    {
        using var directory = new TestDirectory();
        var sourcePath = directory.Write(
            "sample.jpg",
            TestMediaFactory.JpegWithTrailer(TestMediaFactory.MinimalMp4()));

        var exitCode = await CommandLineRunner.RunAsync(["--unknown", sourcePath]);

        Assert.Equal(64, exitCode);
        Assert.False(File.Exists(Path.Combine(directory.Path, "sample.mp4")));
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"LivePhotoVideoExtractor.CommandLineTests-{Guid.NewGuid():N}");
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
