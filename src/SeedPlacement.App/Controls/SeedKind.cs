namespace SeedPlacement.App.Controls;

/// <summary>What is growing in a dish. It sets how seeds are drawn and is recorded, but never affects placement.</summary>
public enum SeedKind
{
    Cucumber,
    Wheat,
    Lettuce,
    Radish,
    BarnyardGrass,
}

public sealed record SeedKindInfo(SeedKind Kind, string Name, double LengthMm, double Aspect)
{
    public static IReadOnlyList<SeedKindInfo> All { get; } =
    [
        new(SeedKind.Cucumber, "Cucumber", 9, 2.45),
        new(SeedKind.Wheat, "Wheat", 6.5, 2.15),
        new(SeedKind.Lettuce, "Lettuce", 4.2, 3.4),
        new(SeedKind.Radish, "Radish", 3.3, 1.18),
        new(SeedKind.BarnyardGrass, "Barnyard grass", 3.3, 1.75),
    ];

    public static SeedKindInfo Of(SeedKind kind) => All[(int)kind];

    /// <summary>Finds a type by the name stored with a dish; dishes saved without one are cucumber.</summary>
    public static SeedKindInfo Named(string? name) =>
        All.FirstOrDefault(k => string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public string AssetKey => Name.ToLowerInvariant().Replace(' ', '-');

    public Uri SpriteUri(int variant) =>
        new($"avares://SeedPlacementRandomizer/Assets/Seeds/{AssetKey}-{variant}.png");
}
