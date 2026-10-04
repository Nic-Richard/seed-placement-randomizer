namespace SeedPlacement.App;

/// <summary>Sends a run file through the system share sheet, on platforms that have one.</summary>
public static class RunSharing
{
    /// <summary>Takes the file name and its contents. Null where sharing is not available.</summary>
    public static Func<string, string, Task>? Share { get; set; }

    public static bool IsAvailable => Share is not null;
}
