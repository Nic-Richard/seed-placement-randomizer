using Avalonia;

namespace SeedPlacement.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<SeedPlacement.App.App>()
            .UsePlatformDetect()
            .LogToTrace();
}
