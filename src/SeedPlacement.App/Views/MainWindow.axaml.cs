using Avalonia.Controls;
using Avalonia.Input;

namespace SeedPlacement.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != Key.F11) return;
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
        e.Handled = true;
    }
}
