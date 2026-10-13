namespace NexAlbum.Core.Layout;

/// <summary>
/// Metni kutu genişliğine göre satırlara böler (sözcük kaydırma). Ölçüm çizim katmanından verilir (PDF: PDFsharp,
/// ekran: WPF); kural her yerde aynıdır: elle satır sonu korunur, sözcükler sığdığı kadar yan yana dizilir,
/// kutudan uzun tek sözcük bölünmez (taşar; ön kontrol uyarır).
/// </summary>
public static class TextLines
{
    /// <summary>Satır aralığı verilmemişse punto katı.</summary>
    public const double DefaultLineHeight = 1.25;

    public static IReadOnlyList<string> Wrap(string content, double width, Func<string, double> measure)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(measure);
        var lines = new List<string>();
        foreach (var paragraph in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var current = words[0];
            for (var i = 1; i < words.Length; i++)
            {
                var candidate = current + " " + words[i];
                if (measure(candidate) <= width)
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(current);
                    current = words[i];
                }
            }

            lines.Add(current);
        }

        return lines;
    }
}
