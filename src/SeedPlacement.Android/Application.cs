using Android.Runtime;
using Avalonia.Android;
using SeedPlacement.App;

namespace SeedPlacement.Android;

[global::Android.App.Application]
public sealed class Application : AvaloniaAndroidApplication<App.App>
{
    private Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        RunSharing.Share = ShareSheet.ShareAsync;
        base.OnCreate();
    }
}
