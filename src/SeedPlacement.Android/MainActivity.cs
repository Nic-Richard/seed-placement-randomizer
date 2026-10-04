using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace SeedPlacement.Android;

[Activity(
    Label = "Seed Placer",
    Theme = "@style/SeedTheme",
    Icon = "@mipmap/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
    public static MainActivity? Current { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Current = this;
        // The phone layout is portrait only; tablets use the desktop layout in either orientation.
        if (Resources?.Configuration?.SmallestScreenWidthDp < 600) RequestedOrientation = ScreenOrientation.SensorPortrait;
        base.OnCreate(savedInstanceState);
    }
}
