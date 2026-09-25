using System.Text.Json;
using SeedPlacement.App.Controls;

namespace SeedPlacement.App;

/// <summary>Appearance preferences kept between launches in the user's app-data folder.</summary>
public sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SeedPlacementRandomizer",
        "settings.json");

    public string? Palette { get; set; }

    public SeedKind SeedKind { get; set; }

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    // Preferences are a convenience; failing to write them must never interrupt the user.
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}
