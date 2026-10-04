namespace SeedPlacement.App;

public sealed record ExportFile(string Name, string MimeType, Action<Stream> Write);

/// <summary>Set by the Android app; null on the desktop, which uses its file dialogs.</summary>
public static class PhoneFiles
{
    /// <summary>Sends a run file through the share sheet. Takes the file name and its contents.</summary>
    public static Func<string, string, Task>? ShareRun { get; set; }

    /// <summary>Saves files into a named folder in Downloads and returns where they went.</summary>
    public static Func<string, IReadOnlyList<ExportFile>, Task<string>>? SaveToDownloads { get; set; }
}
