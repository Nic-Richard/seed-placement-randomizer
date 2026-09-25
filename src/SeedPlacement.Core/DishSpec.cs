namespace SeedPlacement.Core;

/// <summary>Physical dish geometry in millimetres. Seed positions are measured to the seed center.</summary>
public sealed record DishSpec(double DiameterMm, double RimMarginMm)
{
    public static DishSpec Standard90 { get; } = new(90, 10);

    public double RadiusMm => DiameterMm / 2;

    public double UsableRadiusMm => RadiusMm - RimMarginMm;
}
