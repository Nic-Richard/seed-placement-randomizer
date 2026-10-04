using Android.Content;
using Android.Provider;
using SeedPlacement.App;

namespace SeedPlacement.Android;

internal static class Downloads
{
    private const string AppFolder = "Seed Placement";

    // MediaStore lets an app add files to Downloads without a storage permission (Android 10 and later).
    public static Task<string> SaveAsync(string folder, IReadOnlyList<ExportFile> files)
    {
        var resolver = global::Android.App.Application.Context.ContentResolver!;
        var downloads = global::Android.OS.Environment.DirectoryDownloads;
        var relativePath = $"{downloads}/{AppFolder}/{folder}";
        foreach (var file in files)
        {
            var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, file.Name);
            values.Put(MediaStore.IMediaColumns.MimeType, file.MimeType);
            values.Put(MediaStore.IMediaColumns.RelativePath, relativePath);
            var uri = resolver.Insert(MediaStore.Downloads.ExternalContentUri, values)
                ?? throw new IOException($"Couldn't create {file.Name}.");
            using var stream = resolver.OpenOutputStream(uri) ?? throw new IOException($"Couldn't write {file.Name}.");
            file.Write(stream);
        }
        return Task.FromResult($"Downloads/{AppFolder}/{folder}");
    }
}
