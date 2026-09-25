using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeedPlacement.Core;

/// <summary>
/// A rack as plain data. Dishes are stored by layout code, since a code recreates its layout exactly.
/// </summary>
public sealed record RunRecord(int NextNumber, IReadOnlyList<RunRecord.Dish> Dishes)
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public int Version { get; init; } = CurrentVersion;

    public sealed record Dish(int Slot, int Number, string Code, string? Label, string? SeedType = null);

    public static RunRecord From(Rack rack) => new(
        rack.NextNumber,
        rack.Slots
            .Select((d, slot) => d is null ? null : new Dish(Rack.SlotNumber(slot), d.Number, d.Layout.Code.ToString(), d.Label, d.SeedType))
            .OfType<Dish>()
            .ToList());

    public void RestoreInto(Rack rack)
    {
        var dishes = new List<(int, ShelvedDish)>();
        foreach (var d in Dishes)
        {
            if (!LayoutCode.TryParse(d.Code, out var code)) throw new FormatException($"'{d.Code}' is not a layout code.");
            dishes.Add((d.Slot - 1, new ShelvedDish(d.Number, SeedSampler.Generate(code), d.Label, d.SeedType)));
        }
        rack.Restore(dishes, NextNumber);
    }

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>Reads a saved run, or returns null when the text is not a run this version understands.</summary>
    public static RunRecord? FromJson(string json)
    {
        try
        {
            var record = JsonSerializer.Deserialize<RunRecord>(json, Json);
            return record is { Version: CurrentVersion, Dishes: not null } ? record : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
