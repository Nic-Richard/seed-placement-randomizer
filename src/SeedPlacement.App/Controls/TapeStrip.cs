using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SeedPlacement.App.Controls;

/// <summary>A strip of masking tape with torn ends, drawn behind its content.</summary>
public sealed class TapeStrip : Decorator
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<TapeStrip, Color>(nameof(Color), Color.Parse("#E7DCC0"));

    // Fractions of the height where each torn end steps in; a fixed pattern keeps labels stable.
    private static readonly double[] LeftTear = [0.0, 0.05, 0.02, 0.07, 0.03, 0.06, 0.01];
    private static readonly double[] RightTear = [0.04, 0.0, 0.06, 0.02, 0.07, 0.01, 0.05];

    static TapeStrip()
    {
        AffectsRender<TapeStrip>(ColorProperty);
    }

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var shape = Outline(w, h);
        using (context.PushTransform(Matrix.CreateTranslation(0, 2)))
        {
            context.DrawGeometry(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), null, shape);
        }

        var c = Color;
        var fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Shade(c, 1.03), 0),
                new GradientStop(c, 0.5),
                new GradientStop(Shade(c, 0.94), 1),
            },
        };
        context.DrawGeometry(fill, null, shape);
        base.Render(context);
    }

    private static StreamGeometry Outline(double w, double h)
    {
        var tooth = Math.Min(h * 0.35, 8);
        var geometry = new StreamGeometry();
        using var g = geometry.Open();
        g.BeginFigure(new Point(LeftTear[0] * tooth * 4, 0), true);
        for (var i = 1; i < RightTear.Length; i++)
        {
            var y = h * (i - 1) / (RightTear.Length - 1);
            g.LineTo(new Point(w - RightTear[i] * tooth * 4, y));
        }
        g.LineTo(new Point(w - RightTear[0] * tooth * 4, h));
        for (var i = LeftTear.Length - 1; i >= 1; i--)
        {
            var y = h * (i - 1) / (LeftTear.Length - 1);
            g.LineTo(new Point(LeftTear[i] * tooth * 4, y));
        }
        g.EndFigure(true);
        return geometry;
    }

    private static Color Shade(Color c, double f) =>
        Color.FromRgb(
            (byte)Math.Clamp(c.R * f, 0, 255),
            (byte)Math.Clamp(c.G * f, 0, 255),
            (byte)Math.Clamp(c.B * f, 0, 255));
}
