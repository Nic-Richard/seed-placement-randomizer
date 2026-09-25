using System.Globalization;
using Avalonia.Platform;
using SeedPlacement.App.ViewModels;
using SeedPlacement.Core;
using SkiaSharp;

namespace SeedPlacement.App.Export;

/// <summary>
/// Printable seed templates at true scale: set a dish on its circle and put each seed on its mark.
/// Two dishes per page, on US Letter in regions that use it and A4 everywhere else.
/// </summary>
public static class TemplatePdf
{
    private const float PointsPerMm = 72f / 25.4f;
    private const float ScaleBarMm = 50;

    // Countries whose standard paper is US Letter (CLDR). Metric use is no guide: Canada is metric but prints on Letter.
    private static readonly HashSet<string> LetterRegions =
        ["US", "CA", "MX", "BZ", "CL", "CO", "CR", "GT", "NI", "PA", "PH", "PR", "SV", "VE"];

    public static bool UsesLetter(RegionInfo region) => LetterRegions.Contains(region.TwoLetterISORegionName);

    public static void Write(Stream output, Rack rack)
    {
        using var buffer = new MemoryStream();
        Render(buffer, rack, UsesLetter(RegionInfo.CurrentRegion));
        output.Write(PdfActualSize.Apply(buffer.ToArray()));
    }

    private static void Render(Stream output, Rack rack, bool letter)
    {
        var page = letter ? new SKSize(612, 792) : new SKSize(595.28f, 841.89f);
        var dishes = Enumerable.Range(0, Rack.SlotCount)
            .Where(s => rack.Slots[s] is not null)
            .Select(s => (Slot: s, Dish: rack.Slots[s]!))
            .ToList();

        using var regular = LoadFont("AtkinsonHyperlegible-Regular.ttf");
        using var bold = LoadFont("AtkinsonHyperlegible-Bold.ttf");
        using var document = SKDocument.CreatePdf(output, new SKDocumentPdfMetadata
        {
            Title = "Seed placement templates",
            Creator = "Seed Placement Randomizer",
        });

        for (var i = 0; i < dishes.Count; i += 2)
        {
            var canvas = document.BeginPage(page.Width, page.Height);
            var half = page.Height / 2;
            DrawDish(canvas, dishes[i].Slot, dishes[i].Dish, new SKRect(0, 0, page.Width, half), regular, bold);
            if (i + 1 < dishes.Count)
            {
                using var fold = new SKPaint { Color = new SKColor(200, 200, 200), StrokeWidth = 0.5f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash([4, 4], 0) };
                canvas.DrawLine(36, half, page.Width - 36, half, fold);
                DrawDish(canvas, dishes[i + 1].Slot, dishes[i + 1].Dish, new SKRect(0, half, page.Width, page.Height), regular, bold);
            }
            DrawScaleBar(canvas, page, regular);
            document.EndPage();
        }
        document.Close();
    }

    private static void DrawDish(SKCanvas canvas, int slot, ShelvedDish dish, SKRect area, SKTypeface regular, SKTypeface bold)
    {
        var layout = dish.Layout;
        var ink = new SKColor(30, 34, 38);
        var faint = new SKColor(150, 156, 160);
        using var title = new SKFont(bold, 16);
        using var body = new SKFont(regular, 10);
        using var small = new SKFont(bold, 8);
        using var text = new SKPaint { Color = ink, IsAntialias = true };
        using var muted = new SKPaint { Color = new SKColor(95, 102, 108), IsAntialias = true };

        var left = area.Left + 40;
        canvas.DrawText($"{dish.DisplayLabel} goes in slot {Rack.SlotNumber(slot)}", left, area.Top + 44, title, text);
        canvas.DrawText($"{DishItem.Describe(layout, dish.SeedType)}. Layout code {layout.Code}", left, area.Top + 60, body, muted);

        var center = new SKPoint(area.MidX, area.Top + 60 + (area.Height - 60) / 2 + 4);
        var radius = (float)layout.Dish.RadiusMm * PointsPerMm;
        var usable = (float)layout.Dish.UsableRadiusMm * PointsPerMm;

        using var rim = new SKPaint { Color = ink, StrokeWidth = 0.8f, Style = SKPaintStyle.Stroke, IsAntialias = true };
        using var margin = new SKPaint { Color = faint, StrokeWidth = 0.5f, Style = SKPaintStyle.Stroke, IsAntialias = true, PathEffect = SKPathEffect.CreateDash([3, 3], 0) };
        canvas.DrawCircle(center, radius, rim);
        canvas.DrawCircle(center, usable, margin);
        using var cross = new SKPaint { Color = faint, StrokeWidth = 0.5f, IsAntialias = true };
        canvas.DrawLine(center.X - 4, center.Y, center.X + 4, center.Y, cross);
        canvas.DrawLine(center.X, center.Y - 4, center.X, center.Y + 4, cross);

        var top = new SKPoint(center.X, center.Y - radius - 5);
        using var arrow = new SKPath();
        arrow.MoveTo(top.X, top.Y);
        arrow.LineTo(top.X - 5, top.Y - 7);
        arrow.LineTo(top.X + 5, top.Y - 7);
        arrow.Close();
        canvas.DrawPath(arrow, text);
        canvas.DrawText("Top", top.X + 9, top.Y - 1, SKTextAlign.Left, body, muted);

        using var mark = new SKPaint { Color = ink, StrokeWidth = 0.9f, Style = SKPaintStyle.Stroke, IsAntialias = true };
        using var dot = new SKPaint { Color = ink, IsAntialias = true };
        for (var i = 0; i < layout.Seeds.Count; i++)
        {
            var p = layout.Seeds[i];
            var at = new SKPoint(center.X + (float)p.X * PointsPerMm, center.Y - (float)p.Y * PointsPerMm);
            var ring = 2.5f * PointsPerMm;
            canvas.DrawCircle(at, ring, mark);
            canvas.DrawCircle(at, 0.9f, dot);
            canvas.DrawText((i + 1).ToString(CultureInfo.InvariantCulture), at.X + ring + 2, at.Y - ring + 2, small, text);
        }
    }

    private static void DrawScaleBar(SKCanvas canvas, SKSize page, SKTypeface regular)
    {
        using var font = new SKFont(regular, 8);
        using var paint = new SKPaint { Color = new SKColor(95, 102, 108), StrokeWidth = 0.8f, IsAntialias = true };
        var length = ScaleBarMm * PointsPerMm;
        var x = 40f;
        var y = page.Height - 26;
        canvas.DrawLine(x, y, x + length, y, paint);
        canvas.DrawLine(x, y - 4, x, y + 4, paint);
        canvas.DrawLine(x + length, y - 4, x + length, y + 4, paint);
        canvas.DrawText("Print at actual size (100%), not fit to page. This line should measure 50 mm.", x + length + 10, y + 3, SKTextAlign.Left, font, paint);
    }

    private static SKTypeface LoadFont(string file)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://SeedPlacementRandomizer/Assets/Fonts/{file}"));
        return SKTypeface.FromStream(stream);
    }
}
