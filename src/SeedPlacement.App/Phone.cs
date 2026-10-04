using Avalonia;

namespace SeedPlacement.App;

public sealed record ExportFile(string Name, string MimeType, Action<Stream> Write);

/// <summary>Set by the Android app; null on the desktop, which uses its file dialogs and has no system bars.</summary>
public static class Phone
{
    /// <summary>Sends a run file through the share sheet. Takes the file name and its contents.</summary>
    public static Func<string, string, Task>? ShareRun { get; set; }

    /// <summary>Saves files into a named folder in Downloads and returns where they went.</summary>
    public static Func<string, IReadOnlyList<ExportFile>, Task<string>>? SaveToDownloads { get; set; }

    /// <summary>Space taken by the status and navigation bars, in device-independent pixels, once known.</summary>
    public static Func<Thickness?>? SafeArea { get; set; }
}
