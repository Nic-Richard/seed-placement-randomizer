namespace SeedPlacement.App.Controls;

/// <summary>How seeds are drawn. Purely visual: it never affects placement or spacing.</summary>
public enum SeedKind
{
    Cucumber,
    Wheat,
    Lettuce,
    Radish,
    Sunflower,
}

public sealed record SeedKindInfo(SeedKind Kind, string Name, double LengthMm, double Aspect)
{
    public static IReadOnlyList<SeedKindInfo> All { get; } =
    [
        new(SeedKind.Cucumber, "Cucumber", 9, 2.45),
        new(SeedKind.Wheat, "Wheat", 6.5, 2.15),
        new(SeedKind.Lettuce, "Lettuce", 4.2, 3.4),
        new(SeedKind.Radish, "Radish", 3.3, 1.18),
        new(SeedKind.Sunflower, "Sunflower", 10, 2.05),
    ];

    public static SeedKindInfo Of(SeedKind kind) => All[(int)kind];

    public string AssetKey => Name.ToLowerInvariant();

    public Uri SpriteUri(int variant) =>
        new($"avares://SeedPlacementRandomizer/Assets/Seeds/{AssetKey}-{variant}.png");
}
