namespace LivePhotoVideoExtractor.App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        if (arguments.Length > 0)
        {
            return CommandLineRunner.RunAsync(arguments).GetAwaiter().GetResult();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
