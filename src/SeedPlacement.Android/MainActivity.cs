using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace SeedPlacement.Android;

[Activity(
    Label = "Seed Placement",
    Theme = "@style/SeedTheme",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Phones only get the stacked portrait layout; tablets are wide enough for the desktop one either way.
        if (Resources?.Configuration?.SmallestScreenWidthDp < 600) RequestedOrientation = ScreenOrientation.SensorPortrait;
        base.OnCreate(savedInstanceState);
    }
}
