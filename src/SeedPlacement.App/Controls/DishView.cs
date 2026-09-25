using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SeedPlacement.Core;

namespace SeedPlacement.App.Controls;

/// <summary>A top-down glass petri dish with its seeds, drawn to scale from a layout.</summary>
public sealed class DishView : Control
{
    public static readonly StyledProperty<DishLayout?> LayoutProperty =
        AvaloniaProperty.Register<DishView, DishLayout?>(nameof(Layout));

    /// <summary>Seeds land in order as this rises from 0 to <see cref="RevealEnd"/>.</summary>
    public static readonly StyledProperty<double> RevealProperty =
        AvaloniaProperty.Register<DishView, double>(nameof(Reveal), double.MaxValue);

    /// <summary>Tilt of the dish in -1..1 on each axis; moves the reflections against the key light.</summary>
    public static readonly StyledProperty<Vector> TiltProperty =
        AvaloniaProperty.Register<DishView, Vector>(nameof(Tilt));

    public static readonly StyledProperty<int> HighlightedSeedProperty =
        AvaloniaProperty.Register<DishView, int>(nameof(HighlightedSeed), -1);

    public static readonly StyledProperty<bool> ShowNumbersProperty =
        AvaloniaProperty.Register<DishView, bool>(nameof(ShowNumbers));

    public static readonly StyledProperty<bool> IsEmptyGhostProperty =
        AvaloniaProperty.Register<DishView, bool>(nameof(IsEmptyGhost));

    /// <summary>Inherited, so setting it once on the window restyles every dish.</summary>
    public static readonly AttachedProperty<SeedKind> SeedKindProperty =
        AvaloniaProperty.RegisterAttached<DishView, Visual, SeedKind>("SeedKind", inherits: true);

    public static readonly StyledProperty<Color> AccentProperty =
        AvaloniaProperty.Register<DishView, Color>(nameof(Accent), Color.Parse("#A8DB6E"));

    public static readonly StyledProperty<Color> OnAccentProperty =
        AvaloniaProperty.Register<DishView, Color>(nameof(OnAccent), Color.Parse("#13200A"));

    public static readonly StyledProperty<Color> GhostProperty =
        AvaloniaProperty.Register<DishView, Color>(nameof(Ghost), Colors.White);

    private const double SeedSpriteFill = 0.84;
    private const double WallMm = 1.8;
    private const double Stagger = 0.55;
    private const double LightAngle = -128;

    private static readonly Dictionary<SeedKind, Bitmap[]> Sprites = [];

    private static readonly Lazy<Bitmap> Paper = new(() => Load("paper.png"));

    private static readonly Typeface NumberFace =
        new(new FontFamily("avares://SeedPlacementRandomizer/Assets/Fonts#Atkinson Hyperlegible"), FontStyle.Normal, FontWeight.Bold);

    static DishView()
    {
        AffectsRender<DishView>(
            LayoutProperty, RevealProperty, TiltProperty, HighlightedSeedProperty, ShowNumbersProperty,
            IsEmptyGhostProperty, SeedKindProperty, AccentProperty, OnAccentProperty, GhostProperty);
    }

    public DishLayout? Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public double Reveal
    {
        get => GetValue(RevealProperty);
        set => SetValue(RevealProperty, value);
    }

    public Vector Tilt
    {
        get => GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    public int HighlightedSeed
    {
        get => GetValue(HighlightedSeedProperty);
        set => SetValue(HighlightedSeedProperty, value);
    }

    public bool ShowNumbers
    {
        get => GetValue(ShowNumbersProperty);
        set => SetValue(ShowNumbersProperty, value);
    }

    /// <summary>Draws only a faint outline where a dish would sit.</summary>
    public bool IsEmptyGhost
    {
        get => GetValue(IsEmptyGhostProperty);
        set => SetValue(IsEmptyGhostProperty, value);
    }

    public SeedKind SeedKind
    {
        get => GetValue(SeedKindProperty);
        set => SetValue(SeedKindProperty, value);
    }

    public Color Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public Color OnAccent
    {
        get => GetValue(OnAccentProperty);
        set => SetValue(OnAccentProperty, value);
    }

    /// <summary>Colour of the outline drawn for an empty slot.</summary>
    public Color Ghost
    {
        get => GetValue(GhostProperty);
        set => SetValue(GhostProperty, value);
    }

    public static SeedKind GetSeedKind(Visual visual) => visual.GetValue(SeedKindProperty);

    public static void SetSeedKind(Visual visual, SeedKind value) => visual.SetValue(SeedKindProperty, value);

    public static double RevealEnd(int seedCount) => Math.Max(0, seedCount - 1) * Stagger + 1;

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0) return;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var dish = Layout?.Dish ?? DishSpec.Standard90;
        var radius = size * 0.47;
        var k = radius / dish.RadiusMm;

        if (IsEmptyGhost)
        {
            var g = Ghost;
            var dash = new Pen(new SolidColorBrush(Color.FromArgb(70, g.R, g.G, g.B)), Math.Max(1, size / 260),
                new DashStyle([4, 5], 0));
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(10, g.R, g.G, g.B)), dash, center, radius, radius);
            return;
        }

        var light = Direction(LightAngle - Tilt.X * 40 + Tilt.Y * 25);
        var innerRadius = radius - WallMm * k;
        var paperRadius = innerRadius - 0.5 * k;

        DrawShadow(context, center, radius, light);
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(30, 200, 220, 232)), null, center, radius, radius);
        DrawPaper(context, center, paperRadius);
        if (Layout is { } layout) DrawSeeds(context, layout, center, k, light);
        DrawSheen(context, center, innerRadius, light);
        DrawRim(context, center, radius, innerRadius, light);
    }

    private static void DrawShadow(DrawingContext context, Point center, double radius, Vector light)
    {
        var offset = -light * radius * 0.03;
        var brush = new RadialGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Color.FromArgb(150, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(110, 0, 0, 0), 0.80),
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0),
            },
        };
        var r = radius * 1.03;
        context.DrawEllipse(brush, null, center + offset, r, r);
    }

    private static void DrawPaper(DrawingContext context, Point center, double radius)
    {
        var disc = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        using (context.PushGeometryClip(new EllipseGeometry(disc)))
        {
            context.DrawImage(Paper.Value, new Rect(Paper.Value.Size), disc);
        }

        var edge = new RadialGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0, 40, 45, 50), 0.0),
                new GradientStop(Color.FromArgb(0, 40, 45, 50), 0.68),
                new GradientStop(Color.FromArgb(30, 40, 45, 50), 0.92),
                new GradientStop(Color.FromArgb(80, 25, 28, 32), 1.0),
            },
        };
        context.DrawEllipse(edge, null, center, radius, radius);
    }

    private void DrawSeeds(DrawingContext context, DishLayout layout, Point center, double k, Vector light)
    {
        var kind = SeedKindInfo.Of(SeedKind);
        var sprites = SpritesFor(kind);
        var shadowDir = -light;
        for (var i = 0; i < layout.Seeds.Count; i++)
        {
            var p = Math.Clamp(Reveal - i * Stagger, 0, 1);
            if (p <= 0) continue;
            var e = EaseOutBack(p);
            var fall = 1 - Math.Clamp(e, 0, 1);
            var seed = layout.Seeds[i];
            var look = layout.Looks[i];
            var at = new Point(center.X + seed.X * k, center.Y - seed.Y * k);
            var length = kind.LengthMm * k * look.Scale;
            var scale = 1 + 0.55 * (1 - e);
            var angle = (look.AngleDegrees + fall * 30) * Math.PI / 180;

            var shadowOffset = shadowDir * k * (0.45 + fall * 6);
            var shadowAlpha = (byte)(95 * Math.Clamp(p * 1.4, 0, 1) * (1 - fall * 0.5));
            var shadow = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(shadowAlpha, 30, 25, 15), 0.0),
                    new GradientStop(Color.FromArgb((byte)(shadowAlpha * 0.55), 30, 25, 15), 0.55),
                    new GradientStop(Color.FromArgb(0, 30, 25, 15), 1.0),
                },
            };
            var shadowMatrix = Matrix.CreateRotation(angle) * Matrix.CreateTranslation(at.X + shadowOffset.X, at.Y + shadowOffset.Y);
            using (context.PushTransform(shadowMatrix))
            {
                var spread = 1 + fall * 0.5;
                context.DrawEllipse(shadow, null, default, length * 0.58 * spread, length / kind.Aspect * 0.62 * spread);
            }

            var spriteWidth = length / SeedSpriteFill * scale;
            var sprite = sprites[look.Variant % sprites.Length];
            var matrix = Matrix.CreateRotation(angle) * Matrix.CreateTranslation(at.X, at.Y);
            using (context.PushTransform(matrix))
            using (context.PushOpacity(Math.Clamp(p * 3, 0, 1)))
            {
                context.DrawImage(sprite, new Rect(sprite.Size),
                    new Rect(-spriteWidth / 2, -spriteWidth / 2, spriteWidth, spriteWidth));
            }

            if (i == HighlightedSeed)
            {
                var ring = new Pen(new SolidColorBrush(Accent), Math.Max(1.5, k * 0.45));
                var ringRadius = Math.Max(kind.LengthMm * 0.78, 4.5) * k;
                context.DrawEllipse(null, ring, at, ringRadius, ringRadius);
            }

            if (ShowNumbers && p >= 1) DrawNumber(context, i + 1, at, k, Math.Max(kind.LengthMm, 6), i == HighlightedSeed);
        }
    }

    private void DrawNumber(DrawingContext context, int number, Point at, double k, double seedMm, bool highlighted)
    {
        var text = new FormattedText(number.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, NumberFace, Math.Max(9, k * 2.2),
            highlighted ? new SolidColorBrush(OnAccent) : Brushes.White);
        var pill = new Point(at.X + seedMm * 0.58 * k, at.Y - seedMm * 0.47 * k);
        var r = Math.Max(text.Width, text.Height) / 2 + k * 0.6;
        context.DrawEllipse(
            new SolidColorBrush(highlighted ? Accent : Color.FromArgb(200, 22, 26, 30)),
            null, pill, r, r);
        context.DrawText(text, new Point(pill.X - text.Width / 2, pill.Y - text.Height / 2));
    }

    private static void DrawSheen(DrawingContext context, Point center, double radius, Vector light)
    {
        var brush = new RadialGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Color.FromArgb(34, 255, 255, 255), 0.0),
                new GradientStop(Color.FromArgb(10, 255, 255, 255), 0.55),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 1.0),
            },
        };
        var at = center + light * radius * 0.42;
        context.DrawEllipse(brush, null, at, radius * 0.62, radius * 0.46);
    }

    private static void DrawRim(DrawingContext context, Point center, double radius, double innerRadius, Vector light)
    {
        var band = radius - innerRadius;
        var mid = (radius + innerRadius) / 2;
        var rimBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5 + light.X * 0.5, 0.5 + light.Y * 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5 - light.X * 0.5, 0.5 - light.Y * 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(150, 240, 248, 255), 0.0),
                new GradientStop(Color.FromArgb(60, 210, 225, 235), 0.4),
                new GradientStop(Color.FromArgb(70, 200, 215, 225), 0.75),
                new GradientStop(Color.FromArgb(125, 235, 242, 248), 1.0),
            },
        };
        var hairline = Math.Max(1, band * 0.12);
        context.DrawEllipse(null, new Pen(rimBrush, band), center, mid, mid);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)), hairline), center, radius + hairline / 2, radius + hairline / 2);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), hairline), center, radius - hairline / 2, radius - hairline / 2);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(85, 255, 255, 255)), hairline), center, innerRadius, innerRadius);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), hairline), center, innerRadius - hairline, innerRadius - hairline);

        var lightAngle = Math.Atan2(light.Y, light.X);
        var glint = new Pen(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)), Math.Max(1, band * 0.42), lineCap: PenLineCap.Round);
        context.DrawGeometry(null, glint, Arc(center, radius - band * 0.38, lightAngle - 0.42, lightAngle + 0.30));
        var small = new Pen(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), Math.Max(1, band * 0.3), lineCap: PenLineCap.Round);
        context.DrawGeometry(null, small, Arc(center, radius - band * 0.38, lightAngle + 0.45, lightAngle + 0.58));
        var back = new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), Math.Max(1, band * 0.3), lineCap: PenLineCap.Round);
        context.DrawGeometry(null, back, Arc(center, innerRadius + band * 0.2, lightAngle + Math.PI - 0.32, lightAngle + Math.PI + 0.32));
    }

    private static StreamGeometry Arc(Point center, double radius, double from, double to)
    {
        var geometry = new StreamGeometry();
        using var g = geometry.Open();
        g.BeginFigure(center + new Vector(Math.Cos(from), Math.Sin(from)) * radius, false);
        g.ArcTo(center + new Vector(Math.Cos(to), Math.Sin(to)) * radius, new Size(radius, radius), 0,
            to - from > Math.PI, SweepDirection.Clockwise);
        g.EndFigure(false);
        return geometry;
    }

    private static Vector Direction(double degrees)
    {
        var a = degrees * Math.PI / 180;
        return new Vector(Math.Cos(a), Math.Sin(a));
    }

    private static double EaseOutBack(double t)
    {
        const double c1 = 1.4;
        const double c3 = c1 + 1;
        return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
    }

    private static Bitmap[] SpritesFor(SeedKindInfo kind)
    {
        if (!Sprites.TryGetValue(kind.Kind, out var sprites))
        {
            sprites = Enumerable.Range(0, SeedSampler.SeedVariants)
                .Select(i => new Bitmap(AssetLoader.Open(kind.SpriteUri(i))))
                .ToArray();
            Sprites[kind.Kind] = sprites;
        }
        return sprites;
    }

    private static Bitmap Load(string name) =>
        new(AssetLoader.Open(new Uri($"avares://SeedPlacementRandomizer/Assets/{name}")));
}
