using System.Security.Cryptography;

namespace SeedPlacement.Core;

/// <summary>
/// 40 bits written as eight Crockford base-32 characters: a 32-bit sampler seed, the seed count and
/// the spacing step. The code alone recreates the layout.
/// </summary>
public readonly record struct LayoutCode(uint Seed, PlacementSettings Settings)
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static LayoutCode CreateRandom(PlacementSettings settings) =>
        new(BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(4)), settings.Normalized());

    public override string ToString()
    {
        var s = Settings.Normalized();
        var bits = ((ulong)Seed << 8) | ((ulong)(s.SeedCount - 1) << 4) | (uint)s.SpacingIndex;
        Span<char> chars = stackalloc char[9];
        for (var i = 7; i >= 0; i--)
        {
            chars[i < 4 ? i : i + 1] = Alphabet[(int)(bits & 31)];
            bits >>= 5;
        }
        chars[4] = '-';
        return new string(chars);
    }

    public static bool TryParse(string? text, out LayoutCode code)
    {
        code = default;
        if (text is null) return false;
        ulong bits = 0;
        var count = 0;
        foreach (var raw in text.Trim().ToUpperInvariant())
        {
            if (raw is '-' or ' ') continue;
            var c = raw switch { 'O' => '0', 'I' or 'L' => '1', _ => raw };
            var value = Alphabet.IndexOf(c);
            if (value < 0 || ++count > 8) return false;
            bits = (bits << 5) | (uint)value;
        }
        if (count != 8) return false;
        var seedCount = (int)((bits >> 4) & 15) + 1;
        var spacingIndex = (int)(bits & 15);
        var settings = PlacementSettings.FromIndex(seedCount, spacingIndex);
        if (settings.Normalized() != settings) return false;
        code = new LayoutCode((uint)(bits >> 8), settings);
        return true;
    }
}
