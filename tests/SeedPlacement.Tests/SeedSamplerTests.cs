using SeedPlacement.Core;

namespace SeedPlacement.Tests;

public class SeedSamplerTests
{
    private static readonly DishSpec Dish = DishSpec.Standard90;

    public static TheoryData<int, double> AllSettings()
    {
        var data = new TheoryData<int, double>();
        for (var n = PlacementSettings.MinSeeds; n <= PlacementSettings.MaxSeeds; n++)
        {
            foreach (var spacing in new[] { 15.0, 17.5, 20.0 }) data.Add(n, spacing);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllSettings))]
    public void Every_layout_respects_spacing_and_rim(int count, double spacing)
    {
        var settings = new PlacementSettings(count, spacing);
        var runs = count >= 9 && spacing >= 17.5 ? 3 : 40;
        for (uint seed = 1; seed <= runs; seed++)
        {
            var layout = SeedSampler.Generate(new LayoutCode(seed * 2_654_435_761u, settings));

            Assert.Equal(count, layout.Seeds.Count);
            Assert.Equal(count, layout.Looks.Count);
            foreach (var p in layout.Seeds)
            {
                Assert.True(Math.Sqrt(p.X * p.X + p.Y * p.Y) <= Dish.UsableRadiusMm + 1e-9);
            }
            for (var i = 0; i < count; i++)
            {
                for (var j = i + 1; j < count; j++)
                {
                    Assert.True(layout.Seeds[i].DistanceTo(layout.Seeds[j]) >= spacing - 1e-9);
                }
            }
        }
    }

    [Fact]
    public void Same_code_reproduces_the_same_layout()
    {
        var code = new LayoutCode(123_456_789, new PlacementSettings(7, 16.5));

        var a = SeedSampler.Generate(code);
        var b = SeedSampler.Generate(code);

        Assert.Equal(a.Seeds, b.Seeds);
        Assert.Equal(a.Looks, b.Looks);
        Assert.Equal(a.Method, b.Method);
    }

    [Fact]
    public void Known_code_produces_a_fixed_layout()
    {
        // Guards against changes to the PRNG or sampler that would break codes people have recorded.
        var layout = SeedSampler.Generate(new LayoutCode(42, PlacementSettings.Default));
        var first = layout.Seeds[0];

        Assert.Equal(SamplingMethod.Exact, layout.Method);
        Assert.Equal(3.2636003228844706, first.X, 9);
        Assert.Equal(27.700062000059802, first.Y, 9);
    }

    [Fact]
    public void The_standard_bioassay_uses_exact_sampling()
    {
        for (uint seed = 0; seed < 50; seed++)
        {
            var layout = SeedSampler.Generate(new LayoutCode(seed, PlacementSettings.Default));
            Assert.Equal(SamplingMethod.Exact, layout.Method);
        }
    }

    [Fact]
    public void Single_seed_is_uniform_over_the_area_of_the_dish()
    {
        const int samples = 40_000;
        const int bins = 10;
        var radial = new int[bins];
        var angular = new int[bins];
        var r = Dish.UsableRadiusMm;

        for (uint seed = 0; seed < samples; seed++)
        {
            var p = SeedSampler.Generate(new LayoutCode(seed, new PlacementSettings(1, 15))).Seeds[0];
            // Equal-area rings: (r/R)^2 is uniform when density is uniform per unit area.
            var area = (p.X * p.X + p.Y * p.Y) / (r * r);
            radial[Math.Min(bins - 1, (int)(area * bins))]++;
            var angle = (Math.Atan2(p.Y, p.X) + Math.PI) / Math.Tau;
            angular[Math.Min(bins - 1, (int)(angle * bins))]++;
        }

        // Critical value for 9 degrees of freedom at p = 0.001.
        Assert.True(ChiSquare(radial) < 27.88, $"radial chi-square {ChiSquare(radial):F2}");
        Assert.True(ChiSquare(angular) < 27.88, $"angular chi-square {ChiSquare(angular):F2}");
    }

    [Fact]
    public void Markov_chain_matches_the_exact_distribution()
    {
        const int runs = 1_500;
        var settings = new PlacementSettings(6, 15);
        var r = Dish.UsableRadiusMm;
        double exact = 0, chain = 0;

        for (uint seed = 0; seed < runs; seed++)
        {
            exact += MeanArea(SeedSampler.Generate(new LayoutCode(seed, settings)).Seeds, r);

            var rng = new Xoshiro256(seed + 1_000_000UL);
            var seeds = new SeedPosition[settings.SeedCount];
            Assert.True(SeedSampler.TryGreedy(rng, seeds, r, settings.SpacingMm));
            SeedSampler.RunChain(rng, seeds, r, settings.SpacingMm);
            chain += MeanArea(seeds, r);
        }

        Assert.InRange(chain / runs - exact / runs, -0.015, 0.015);
    }

    [Fact]
    public void Crowded_settings_fall_back_to_the_chain_and_stay_valid()
    {
        var layout = SeedSampler.Generate(new LayoutCode(7, new PlacementSettings(10, 20)));

        Assert.Equal(SamplingMethod.MarkovChain, layout.Method);
        Assert.Equal(10, layout.Seeds.Count);
    }

    private static double MeanArea(IEnumerable<SeedPosition> seeds, double r) =>
        seeds.Average(p => (p.X * p.X + p.Y * p.Y) / (r * r));

    private static double ChiSquare(int[] observed)
    {
        var expected = observed.Sum() / (double)observed.Length;
        return observed.Sum(o => (o - expected) * (o - expected) / expected);
    }
}
