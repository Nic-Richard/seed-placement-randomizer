using SeedPlacement.Core;

namespace SeedPlacement.App;

/// <summary>Keeps the current run in the user's app-data folder so closing the app loses nothing.</summary>
public sealed class RunStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SeedPlacementRandomizer",
        "run.json");

    public RunRecord? Load()
    {
        try
        {
            return File.Exists(FilePath) ? RunRecord.FromJson(File.ReadAllText(FilePath)) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // Written to a temporary file first so a crash mid-write never leaves a half-saved run.
    public void Save(Rack rack)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, RunRecord.From(rack).ToJson());
            File.Move(temp, FilePath, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}
