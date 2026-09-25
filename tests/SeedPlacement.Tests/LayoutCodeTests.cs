using SeedPlacement.Core;

namespace SeedPlacement.Tests;

public class LayoutCodeTests
{
    [Theory]
    [InlineData(0u, 1, 15.0)]
    [InlineData(uint.MaxValue, 10, 20.0)]
    [InlineData(3_141_592_653u, 5, 17.5)]
    public void Round_trips(uint seed, int count, double spacing)
    {
        var code = new LayoutCode(seed, new PlacementSettings(count, spacing));

        Assert.True(LayoutCode.TryParse(code.ToString(), out var parsed));
        Assert.Equal(code, parsed);
    }

    [Fact]
    public void Formats_as_two_groups_of_four()
    {
        var text = new LayoutCode(42, PlacementSettings.Default).ToString();

        Assert.Matches("^[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}$", text);
    }

    [Fact]
    public void Parsing_forgives_case_spacing_and_lookalike_characters()
    {
        var code = new LayoutCode(1_000_001, new PlacementSettings(3, 16));
        var typed = code.ToString().ToLowerInvariant().Replace('-', ' ').Replace('0', 'o').Replace('1', 'l');

        Assert.True(LayoutCode.TryParse(typed, out var parsed));
        Assert.Equal(code, parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABCD-EFG")]
    [InlineData("ABCD-EFGHJ")]
    [InlineData("ABCD-EFGU")]
    [InlineData("0000-00AF")]
    public void Rejects_malformed_codes(string text) => Assert.False(LayoutCode.TryParse(text, out _));
}
