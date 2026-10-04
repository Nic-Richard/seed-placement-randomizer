using Android.Content;
using AndroidX.Core.Content;
using Avalonia.Android;
using JavaFile = Java.IO.File;

namespace SeedPlacement.Android;

internal static class ShareSheet
{
    public static async Task ShareAsync(string name, string contents)
    {
        var context = global::Android.App.Application.Context;
        var folder = new JavaFile(context.CacheDir, "shared");
        folder.Mkdirs();
        var file = new JavaFile(folder, name);
        await File.WriteAllTextAsync(file.AbsolutePath, contents);

        // Other apps can only read the file through a content URI that this app grants them.
        var uri = FileProvider.GetUriForFile(context, $"{context.PackageName}.files", file);
        var send = new Intent(Intent.ActionSend);
        send.SetType("application/octet-stream");
        send.PutExtra(Intent.ExtraStream, uri);
        send.ClipData = ClipData.NewRawUri(name, uri);
        send.AddFlags(ActivityFlags.GrantReadUriPermission);
        var chooser = Intent.CreateChooser(send, "Share the run");
        chooser!.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(chooser);
    }
}
