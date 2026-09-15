using System.Xml.Linq;

namespace LivePhotoVideoExtractor.Core.Tests;

public sealed class AppIconContractTests
{
    private static readonly int[] ExpectedSizes = [16, 24, 32, 48, 64, 128, 256];

    [Fact]
    public void Generated_icon_assets_match_the_windows_contract()
    {
        var projectRoot = FindProjectRoot();
        var assetDirectory = Path.Combine(
            projectRoot,
            "src",
            "LivePhotoVideoExtractor.App",
            "Assets");
        var sourcePath = Path.Combine(assetDirectory, "app-icon-source.png");
        var iconPath = Path.Combine(assetDirectory, "app.ico");

        Assert.True(File.Exists(sourcePath), $"Missing icon source: {sourcePath}");
        Assert.True(File.Exists(iconPath), $"Missing Windows icon: {iconPath}");

        using var stream = File.OpenRead(iconPath);
        using var reader = new BinaryReader(stream);

        Assert.Equal(0, reader.ReadUInt16());
        Assert.Equal(1, reader.ReadUInt16());
        var imageCount = reader.ReadUInt16();
        Assert.Equal(ExpectedSizes.Length, imageCount);

        var actualSizes = new List<int>();
        for (var index = 0; index < imageCount; index++)
        {
            var width = reader.ReadByte();
            var height = reader.ReadByte();
            actualSizes.Add(width == 0 ? 256 : width);
            Assert.Equal(width, height);
            reader.ReadBytes(14);
        }

        Assert.Equal(ExpectedSizes, actualSizes);
    }

    [Fact]
    public void WinForms_project_embeds_the_generated_icon()
    {
        var projectRoot = FindProjectRoot();
        var projectPath = Path.Combine(
            projectRoot,
            "src",
            "LivePhotoVideoExtractor.App",
            "LivePhotoVideoExtractor.App.csproj");
        var document = XDocument.Load(projectPath);

        var iconValue = document
            .Descendants("ApplicationIcon")
            .Select(element => element.Value)
            .SingleOrDefault();

        Assert.Equal(@"Assets\app.ico", iconValue);
    }

    private static string FindProjectRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LivePhotoVideoExtractor.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate LivePhotoVideoExtractor.sln.");
    }
}
