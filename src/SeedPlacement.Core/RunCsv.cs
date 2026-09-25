using System.Globalization;
using System.Text;

namespace SeedPlacement.Core;

/// <summary>One row per seed, ordered by slot, for spreadsheets and lab records.</summary>
public static class RunCsv
{
    public const string Header = "slot,dish,label,seed,x_mm,y_mm,layout_code,seeds_per_dish,min_spacing_mm";

    public static string Write(Rack rack)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder().AppendLine(Header);
        for (var slot = 0; slot < Rack.SlotCount; slot++)
        {
            if (rack.Slots[slot] is not { } dish) continue;
            var layout = dish.Layout;
            for (var i = 0; i < layout.Seeds.Count; i++)
            {
                var p = layout.Seeds[i];
                sb.Append(Rack.SlotNumber(slot)).Append(',')
                    .Append(dish.Number).Append(',')
                    .Append(Escape(dish.DisplayLabel)).Append(',')
                    .Append(i + 1).Append(',')
                    .Append(p.X.ToString("0.0", inv)).Append(',')
                    .Append(p.Y.ToString("0.0", inv)).Append(',')
                    .Append(layout.Code).Append(',')
                    .Append(layout.Seeds.Count).Append(',')
                    .Append(layout.Settings.SpacingMm.ToString("0.0", inv))
                    .AppendLine();
            }
        }
        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\n', '\r']) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";
}
