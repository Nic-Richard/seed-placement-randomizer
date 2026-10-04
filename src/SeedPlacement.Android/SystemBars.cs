using Android.Views;
using Avalonia;

namespace SeedPlacement.Android;

internal static class SystemBars
{
    public static Thickness? SafeArea()
    {
        var activity = MainActivity.Current;
        if (activity?.Window?.DecorView?.RootWindowInsets is not { } insets) return null;
        var density = activity.Resources?.DisplayMetrics?.Density ?? 1;
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
            return new Thickness(bars.Left / density, bars.Top / density, bars.Right / density, bars.Bottom / density);
        }
#pragma warning disable CA1422 // The only way to read the bars on Android 10.
        return new Thickness(insets.SystemWindowInsetLeft / density, insets.SystemWindowInsetTop / density,
            insets.SystemWindowInsetRight / density, insets.SystemWindowInsetBottom / density);
#pragma warning restore CA1422
    }
}
