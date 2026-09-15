using LivePhotoVideoExtractor.Core;

namespace LivePhotoVideoExtractor.App;

public static class CommandLineRunner
{
    public static async Task<int> RunAsync(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count < 2
            || !arguments[0].Equals("--extract", StringComparison.OrdinalIgnoreCase))
        {
            return 64;
        }

        var paths = arguments.Skip(1).ToArray();
        var batch = new BatchExtractor(new MotionPhotoExtractor());
        var results = await batch.ExtractAsync(paths).ConfigureAwait(false);

        if (results.Any(result => result.Status == ExtractionStatus.Failed))
        {
            return 1;
        }

        return results.All(result => result.Status == ExtractionStatus.Success) ? 0 : 2;
    }
}
