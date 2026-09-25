using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace SeedPlacement.App.Styles;

public sealed record Palette(
    string Name,
    bool IsLight,
    string Bench,
    string Panel,
    string Ink,
    string Muted,
    string Faint,
    string Accent,
    string AccentHover,
    string AccentPressed,
    string OnAccent,
    string Track,
    string Surface,
    string Overlay,
    string ShelfGap,
    string Spotlight,
    string Danger)
{
    public static Palette BenchBlack { get; } = new(
        "Black epoxy", false,
        Bench: "#121517", Panel: "#D9141719",
        Ink: "#E9EDEF", Muted: "#9AA4AA", Faint: "#66717A",
        Accent: "#E7DCC0", AccentHover: "#F1E8D2", AccentPressed: "#D3C6A4", OnAccent: "#2A2620",
        Track: "#2A3136", Surface: "#F21A1E21", Overlay: "#C40A0C0E",
        ShelfGap: "#BE080A0C", Spotlight: "#22FFF6E6", Danger: "#E47A66");

    public static Palette Slate { get; } = new(
        "Slate", false,
        Bench: "#18202B", Panel: "#D9121922",
        Ink: "#E6ECF2", Muted: "#98A6B5", Faint: "#62707F",
        Accent: "#E7DCC0", AccentHover: "#F1E8D2", AccentPressed: "#D3C6A4", OnAccent: "#2A2620",
        Track: "#26313E", Surface: "#F2182029", Overlay: "#C40B1016",
        ShelfGap: "#BE090D12", Spotlight: "#1EE6F0FF", Danger: "#F08A78");

    public static Palette Soapstone { get; } = new(
        "Soapstone", false,
        Bench: "#211F1C", Panel: "#D91A1816",
        Ink: "#EFEAE2", Muted: "#ABA296", Faint: "#756D63",
        Accent: "#E7DCC0", AccentHover: "#F1E8D2", AccentPressed: "#D3C6A4", OnAccent: "#2A2620",
        Track: "#35302A", Surface: "#F2211E1B", Overlay: "#C40E0C0A",
        ShelfGap: "#BE0C0A08", Spotlight: "#26FFE9C8", Danger: "#E8826C");

    public static Palette Greenhouse { get; } = new(
        "Greenhouse", false,
        Bench: "#122019", Panel: "#D90E1813",
        Ink: "#E8F0EA", Muted: "#9BB0A2", Faint: "#62776A",
        Accent: "#E7DCC0", AccentHover: "#F1E8D2", AccentPressed: "#D3C6A4", OnAccent: "#2A2620",
        Track: "#22332A", Surface: "#F2142019", Overlay: "#C4080F0B",
        ShelfGap: "#BE06100B", Spotlight: "#22F0FFE6", Danger: "#EA8A70");

    public static Palette CleanRoom { get; } = new(
        "Clean room", true,
        Bench: "#DDE1E4", Panel: "#EBF7F8F9",
        Ink: "#1C2226", Muted: "#56616A", Faint: "#87919A",
        Accent: "#353B40", AccentHover: "#454C52", AccentPressed: "#262B2F", OnAccent: "#F4F2EC",
        Track: "#D0D5D9", Surface: "#F8FFFFFF", Overlay: "#B8DDE1E4",
        ShelfGap: "#99AEB5BB", Spotlight: "#50FFFFFF", Danger: "#C4513C");

    public static IReadOnlyList<Palette> All { get; } = [BenchBlack, Slate, Soapstone, Greenhouse, CleanRoom];

    public static Palette Default => BenchBlack;

    public static Palette Named(string? name) => All.FirstOrDefault(p => p.Name == name) ?? Default;

    public override string ToString() => Name;

    public void Apply(Application app)
    {
        var r = app.Resources;
        var line = IsLight ? Colors.Black : Colors.White;

        Brush("BenchBrush", Bench);
        Brush("PanelBrush", Panel);
        Brush("InkBrush", Ink);
        Brush("MutedBrush", Muted);
        Brush("FaintBrush", Faint);
        Brush("AccentBrush", Accent);
        Brush("AccentHoverBrush", AccentHover);
        Brush("AccentPressedBrush", AccentPressed);
        Brush("OnAccentBrush", OnAccent);
        Brush("TrackBrush", Track);
        Brush("SurfaceBrush", Surface);
        Brush("OverlayBrush", Overlay);
        Brush("DangerBrush", Danger);
        r["AccentSoftBrush"] = new SolidColorBrush(WithAlpha(Color.Parse(Accent), 0x24));
        r["HairlineBrush"] = new SolidColorBrush(WithAlpha(line, 0x1F));
        r["ControlBorderBrush"] = new SolidColorBrush(WithAlpha(line, 0x33));
        r["ControlBorderHoverBrush"] = new SolidColorBrush(WithAlpha(line, 0x55));
        r["ControlHoverBrush"] = new SolidColorBrush(WithAlpha(line, 0x14));
        r["ControlDisabledBrush"] = new SolidColorBrush(WithAlpha(line, 0x18));
        r["SunkenBrush"] = new SolidColorBrush(WithAlpha(Colors.Black, IsLight ? (byte)0x0D : (byte)0x24));

        r["TapeColor"] = Color.Parse("#E7DCC0");
        r["AccentColor"] = Color.Parse(Accent);
        r["OnAccentColor"] = Color.Parse(OnAccent);
        r["GhostColor"] = IsLight ? Color.Parse("#1C2226") : Colors.White;
        r["ShelfGapColor"] = Color.Parse(ShelfGap);

        r["SpotlightBrush"] = new RadialGradientBrush
        {
            Center = new RelativePoint(0.6, 0.32, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.6, 0.32, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.8, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.95, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse(Spotlight), 0),
                new GradientStop(WithAlpha(Color.Parse(Spotlight), 0x08), 0.4),
                new GradientStop(WithAlpha(Colors.Black, IsLight ? (byte)0x08 : (byte)0x1A), 0.7),
                new GradientStop(WithAlpha(Colors.Black, IsLight ? (byte)0x30 : (byte)0x70), 1),
            },
        };

        Brush("SliderTrackFill", Track);
        Brush("SliderTrackFillPointerOver", Track);
        Brush("SliderTrackFillPressed", Track);
        Brush("SliderTrackValueFill", Accent);
        Brush("SliderTrackValueFillPointerOver", AccentHover);
        Brush("SliderTrackValueFillPressed", AccentPressed);
        Brush("SliderThumbBackground", IsLight ? "#FFFFFF" : "#F2F5F6");
        Brush("SliderThumbBackgroundPointerOver", "#FFFFFF");
        Brush("SliderThumbBackgroundPressed", IsLight ? "#E6EAED" : "#DDE3E6");
        Brush("SliderTickBarFill", Faint);

        app.RequestedThemeVariant = IsLight ? ThemeVariant.Light : ThemeVariant.Dark;

        void Brush(string key, string color) => r[key] = new SolidColorBrush(Color.Parse(color));
    }

    private static Color WithAlpha(Color c, byte a) => Color.FromArgb(a, c.R, c.G, c.B);
}
