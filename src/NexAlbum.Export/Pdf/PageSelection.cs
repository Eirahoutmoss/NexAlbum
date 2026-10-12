using System.Globalization;

namespace NexAlbum.Export.Pdf;

/// <summary>"all" ya da "1-5,8,10-12" biçimindeki sayfa seçimini çözer (1 tabanlı, sıralı, tekrarsız).</summary>
public static class PageSelection
{
    public static IReadOnlyList<int> Parse(string selection, int pageCount)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentOutOfRangeException.ThrowIfNegative(pageCount);
        var text = selection.Trim();
        if (text.Length == 0 || string.Equals(text, "all", StringComparison.OrdinalIgnoreCase) || text == "tümü")
        {
            return [.. Enumerable.Range(1, pageCount)];
        }

        var pages = new SortedSet<int>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var dash = part.IndexOf('-', StringComparison.Ordinal);
            var from = Number(dash < 0 ? part : part[..dash], selection);
            var to = dash < 0 ? from : Number(part[(dash + 1)..], selection);
            if (from < 1 || to > pageCount || from > to)
            {
                throw new FormatException($"Geçersiz sayfa aralığı: '{part}' (albümde {pageCount} sayfa var).");
            }

            for (var p = from; p <= to; p++)
            {
                pages.Add(p);
            }
        }

        return [.. pages];
    }

    private static int Number(string text, string selection) =>
        int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
            ? n
            : throw new FormatException($"Geçersiz sayfa seçimi: '{selection}'.");
}
