using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SeedPlacement.App.Export;

/// <summary>
/// Marks a PDF so viewers open their print dialog at actual size instead of fit-to-page, which would
/// shrink a 1:1 template. The catalog is replaced through an incremental update appended to the file,
/// so the original bytes and cross-reference table stay valid.
/// </summary>
public static partial class PdfActualSize
{
    public static byte[] Apply(byte[] pdf)
    {
        var text = Encoding.Latin1.GetString(pdf);
        var trailerAt = text.LastIndexOf("trailer", StringComparison.Ordinal);
        if (trailerAt < 0) return pdf;
        var trailer = text[trailerAt..];

        var root = RootRef().Match(trailer);
        var size = SizeEntry().Match(trailer);
        var start = StartXref().Match(trailer);
        if (!root.Success || !size.Success || !start.Success) return pdf;

        var id = root.Groups[1].Value;
        var catalog = new Regex($@"(?:^|\n){id} 0 obj\s*<<(.*?)>>\s*endobj", RegexOptions.Singleline).Match(text);
        if (!catalog.Success || catalog.Groups[1].Value.Contains("/ViewerPreferences", StringComparison.Ordinal)) return pdf;

        var info = InfoRef().Match(trailer);
        var offset = pdf.Length + 1;
        var update = new StringBuilder()
            .Append('\n')
            .Append(CultureInfo.InvariantCulture, $"{id} 0 obj\n<<{catalog.Groups[1].Value}\n/ViewerPreferences <</PrintScaling /None>>>>\nendobj\n");
        var xrefAt = pdf.Length + Encoding.Latin1.GetByteCount(update.ToString());
        update
            .Append(CultureInfo.InvariantCulture, $"xref\n0 1\n0000000000 65535 f \n{id} 1\n{offset:D10} 00000 n \n")
            .Append(CultureInfo.InvariantCulture, $"trailer\n<</Size {size.Groups[1].Value}\n/Root {id} 0 R\n")
            .Append(info.Success ? $"/Info {info.Groups[1].Value} 0 R\n" : "")
            .Append(CultureInfo.InvariantCulture, $"/Prev {start.Groups[1].Value}>>\nstartxref\n{xrefAt}\n%%EOF\n");

        return [.. pdf, .. Encoding.Latin1.GetBytes(update.ToString())];
    }

    [GeneratedRegex(@"/Root (\d+) 0 R")]
    private static partial Regex RootRef();

    [GeneratedRegex(@"/Info (\d+) 0 R")]
    private static partial Regex InfoRef();

    [GeneratedRegex(@"/Size (\d+)")]
    private static partial Regex SizeEntry();

    [GeneratedRegex(@"startxref\s+(\d+)")]
    private static partial Regex StartXref();
}
