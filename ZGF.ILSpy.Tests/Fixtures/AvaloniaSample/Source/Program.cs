using Avalonia;

namespace AvaloniaSample;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
