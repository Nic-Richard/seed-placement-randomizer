namespace SeedPlacement.Core;

/// <summary>A seed center in millimetres from the dish center, +Y toward the top of the dish.</summary>
public readonly record struct SeedPosition(double X, double Y)
{
    public double DistanceTo(SeedPosition other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

/// <summary>Cosmetic only: how a seed sprite is drawn. Never affects placement.</summary>
public readonly record struct SeedLook(double AngleDegrees, int Variant, double Scale);

public enum SamplingMethod
{
    /// <summary>Whole-layout rejection sampling: exactly uniform over valid layouts.</summary>
    Exact,

    /// <summary>Hard-disk Markov chain from a valid start, used when exact sampling is too slow.</summary>
    MarkovChain,
}

public sealed record DishLayout(
    LayoutCode Code,
    DishSpec Dish,
    IReadOnlyList<SeedPosition> Seeds,
    IReadOnlyList<SeedLook> Looks,
    SamplingMethod Method)
{
    public PlacementSettings Settings => Code.Settings;
}
