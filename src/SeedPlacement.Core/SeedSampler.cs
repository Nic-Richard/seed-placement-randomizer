namespace SeedPlacement.Core;

public static class SeedSampler
{
    public const int SeedVariants = 6;

    // Attempt counts, not time budgets, so a layout code always resolves the same way.
    internal const int ExactAttemptBudget = 4_000_000;
    internal const int ChainStepsPerSeed = 40_000;
    private const int GreedyTriesPerSeed = 2_000;
    private const int GreedyRestarts = 10_000;
    private const ulong LookStream = 0x5EED_1004_C0DE_0001UL;

    public static DishLayout Generate(PlacementSettings settings, DishSpec? dish = null) =>
        Generate(LayoutCode.CreateRandom(settings), dish);

    public static DishLayout Generate(LayoutCode code, DishSpec? dish = null)
    {
        dish ??= DishSpec.Standard90;
        var settings = code.Settings.Normalized();
        var rng = new Xoshiro256(code.Seed);
        var seeds = new SeedPosition[settings.SeedCount];
        var radius = dish.UsableRadiusMm;
        var spacing = settings.SpacingMm;

        var method = SamplingMethod.Exact;
        if (!TryExact(rng, seeds, radius, spacing))
        {
            if (!TryGreedy(rng, seeds, radius, spacing))
            {
                throw new InvalidOperationException(
                    $"{settings.SeedCount} seeds cannot be spaced {spacing} mm apart in this dish.");
            }
            RunChain(rng, seeds, radius, spacing);
            method = SamplingMethod.MarkovChain;
        }

        var lookRng = new Xoshiro256(code.Seed ^ LookStream);
        var looks = new SeedLook[seeds.Length];
        for (var i = 0; i < looks.Length; i++)
        {
            looks[i] = new SeedLook(
                lookRng.NextDouble() * 360,
                lookRng.NextInt(SeedVariants),
                0.92 + lookRng.NextDouble() * 0.16);
        }

        return new DishLayout(code with { Settings = settings }, dish, seeds, looks, method);
    }

    /// <summary>
    /// Draws every seed independently and uniformly over the disc and restarts the whole layout on the
    /// first conflict. Abandoning early is equivalent to drawing all seeds and then rejecting, so the
    /// accepted layout is exactly uniform over valid layouts.
    /// </summary>
    private static bool TryExact(Xoshiro256 rng, SeedPosition[] seeds, double radius, double spacing)
    {
        var spacingSq = spacing * spacing;
        for (var attempt = 0; attempt < ExactAttemptBudget; attempt++)
        {
            var placed = 0;
            while (placed < seeds.Length)
            {
                var p = UniformInDisc(rng, radius);
                if (Conflicts(seeds, placed, p, spacingSq, skip: -1)) break;
                seeds[placed++] = p;
            }
            if (placed == seeds.Length) return true;
        }
        return false;
    }

    internal static bool TryGreedy(Xoshiro256 rng, SeedPosition[] seeds, double radius, double spacing)
    {
        var spacingSq = spacing * spacing;
        for (var restart = 0; restart < GreedyRestarts; restart++)
        {
            var placed = 0;
            for (var tries = 0; placed < seeds.Length && tries < GreedyTriesPerSeed * seeds.Length; tries++)
            {
                var p = UniformInDisc(rng, radius);
                if (!Conflicts(seeds, placed, p, spacingSq, skip: -1)) seeds[placed++] = p;
            }
            if (placed == seeds.Length) return true;
        }
        return false;
    }

    /// <summary>
    /// Metropolis moves with a proposal drawn uniformly over the whole disc. The proposal is symmetric
    /// and the target is flat over valid layouts, so a move is accepted exactly when it stays valid,
    /// and the chain converges to the same uniform distribution the exact sampler draws from.
    /// </summary>
    internal static void RunChain(Xoshiro256 rng, SeedPosition[] seeds, double radius, double spacing)
    {
        var spacingSq = spacing * spacing;
        var steps = ChainStepsPerSeed * seeds.Length;
        for (var step = 0; step < steps; step++)
        {
            var i = rng.NextInt(seeds.Length);
            var p = UniformInDisc(rng, radius);
            if (!Conflicts(seeds, seeds.Length, p, spacingSq, skip: i)) seeds[i] = p;
        }
    }

    private static bool Conflicts(SeedPosition[] seeds, int count, SeedPosition p, double spacingSq, int skip)
    {
        for (var j = 0; j < count; j++)
        {
            if (j == skip) continue;
            var dx = seeds[j].X - p.X;
            var dy = seeds[j].Y - p.Y;
            if (dx * dx + dy * dy < spacingSq) return true;
        }
        return false;
    }

    // Square root keeps the density uniform per unit area rather than bunching seeds at the center.
    internal static SeedPosition UniformInDisc(Xoshiro256 rng, double radius)
    {
        var r = radius * Math.Sqrt(rng.NextDouble());
        var theta = rng.NextDouble() * Math.Tau;
        return new SeedPosition(r * Math.Cos(theta), r * Math.Sin(theta));
    }
}
