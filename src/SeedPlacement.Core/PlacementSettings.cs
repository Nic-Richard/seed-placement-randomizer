namespace SeedPlacement.Core;

public sealed record PlacementSettings(int SeedCount, double SpacingMm)
{
    public const int MinSeeds = 1;
    public const int MaxSeeds = 10;
    public const double MinSpacingMm = 15;
    public const double MaxSpacingMm = 20;
    public const double SpacingStepMm = 0.5;

    public static PlacementSettings Default { get; } = new(5, 15);

    public int SpacingIndex => (int)Math.Round((SpacingMm - MinSpacingMm) / SpacingStepMm);

    public static PlacementSettings FromIndex(int seedCount, int spacingIndex) =>
        new(seedCount, MinSpacingMm + spacingIndex * SpacingStepMm);

    public PlacementSettings Normalized() =>
        FromIndex(
            Math.Clamp(SeedCount, MinSeeds, MaxSeeds),
            Math.Clamp(SpacingIndex, 0, (int)((MaxSpacingMm - MinSpacingMm) / SpacingStepMm)));
}
