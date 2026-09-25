using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SeedPlacement.App.Controls;

/// <summary>A chrome wire shelf seen from above, with corner posts.</summary>
public sealed class WireShelf : Control
{
    private const double Post = 13;
    private const double Frame = 7;

    public static readonly StyledProperty<Color> GapColorProperty =
        AvaloniaProperty.Register<WireShelf, Color>(nameof(GapColor), Color.FromArgb(190, 8, 10, 12));

    private static readonly IPen Wire = new Pen(new SolidColorBrush(Color.FromArgb(200, 118, 126, 133)), 2);
    private static readonly IPen WireShine = new Pen(new SolidColorBrush(Color.FromArgb(120, 225, 230, 235)), 0.7);
    private static readonly IPen WireShade = new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)), 1.2);

    private static readonly IBrush Chrome = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#E8ECEF"), 0),
            new GradientStop(Color.Parse("#9AA2A9"), 0.35),
            new GradientStop(Color.Parse("#D5DADE"), 0.6),
            new GradientStop(Color.Parse("#6F777E"), 1),
        },
    };

    static WireShelf()
    {
        AffectsRender<WireShelf>(GapColorProperty);
    }

    public Color GapColor
    {
        get => GetValue(GapColorProperty);
        set => SetValue(GapColorProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size).Deflate(Post / 2);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), null,
            bounds.Translate(new Vector(0, 8)).Inflate(6), 18, 18);
        context.DrawRectangle(new SolidColorBrush(GapColor), null, bounds, 10, 10);

        var inner = bounds.Deflate(Frame);
        var spacing = 15.0;
        for (var y = inner.Top + spacing / 2; y < inner.Bottom; y += spacing)
        {
            var from = new Point(inner.Left, y);
            var to = new Point(inner.Right, y);
            context.DrawLine(WireShade, from + new Vector(0, 1.6), to + new Vector(0, 1.6));
            context.DrawLine(Wire, from, to);
            context.DrawLine(WireShine, from - new Vector(0, 0.6), to - new Vector(0, 0.6));
        }

        var support = new Pen(Chrome, 4.5);
        for (var i = 1; i <= 3; i++)
        {
            var x = inner.Left + inner.Width * i / 4;
            context.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)), 5), new Point(x + 1.5, inner.Top), new Point(x + 1.5, inner.Bottom));
            context.DrawLine(support, new Point(x, inner.Top), new Point(x, inner.Bottom));
        }

        context.DrawRectangle(null, new Pen(Chrome, Frame), bounds.Deflate(Frame / 2), 8, 8);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 1), bounds.Deflate(1), 9, 9);

        var post = new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(0.35, 0.3, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse("#FAFBFC"), 0),
                new GradientStop(Color.Parse("#A7AEB4"), 0.5),
                new GradientStop(Color.Parse("#4E555B"), 1),
            },
        };
        foreach (var corner in new[] { bounds.TopLeft, bounds.TopRight, bounds.BottomLeft, bounds.BottomRight })
        {
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)), null, corner + new Vector(2, 4), Post, Post);
            context.DrawEllipse(post, new Pen(new SolidColorBrush(Color.FromArgb(160, 30, 34, 38)), 1), corner, Post, Post);
        }
    }
}
