using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SeedPlacement.App.Styles;
using SeedPlacement.App.ViewModels;
using SeedPlacement.App.Views;

namespace SeedPlacement.App;

public sealed class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Palette.Named(AppSettings.Load().Palette).Apply(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = new MainViewModel(AppSettings.Load(), new RunStore()) };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            single.MainView = new MainView { DataContext = new MainViewModel(AppSettings.Load(), new RunStore()) };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
