using System.Globalization;
using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Layout;

/// <summary>Satırdaki bir parça: harf aralığı yoksa satırın tamamı, varsa tek harf (grafem). X satır başına göredir (mm).</summary>
public sealed record TextPiece(string Text, double X);

/// <summary>Yerleşmiş satır: mutlak sol kenar ve satır kutusunun üstü (mm), genişlik.</summary>
public sealed record TextLineLayout(IReadOnlyList<TextPiece> Pieces, double Left, double Top, double Width);

/// <summary>Metnin yerleşimi: satırlar, satır yüksekliği (mm) ve yazının kapladığı alan (zemin bandı için).</summary>
public sealed record TextBlockLayout(IReadOnlyList<TextLineLayout> Lines, double Step, double SizeMm, RectMm InkBox);

/// <summary>Bir kopya: yazı (dx, dy) kadar kaydırılıp bu renkle çizilir. Etkiler kopyaların üst üste binmesiyle oluşur.</summary>
public readonly record struct TextPass(double Dx, double Dy, RgbaColor Color);

/// <summary>
/// Ekran ve PDF'in ortak metin yerleşimi ve etki planı (Karar 17 §11). Ölçüm çizene aittir (WPF ya da PDFsharp);
/// satır kırma, hizalama, harf aralığı ve etki kopyaları burada hesaplanır; böylece iki çıktı aynı görünür.
/// </summary>
public static class TextLayout
{
    /// <summary>Kopyaların halkadaki sayısı (gölge/hale yumuşaklığı ile çizim maliyeti arasında denge).</summary>
    public const int RingCopies = 12;

    /// <param name="text">Metin öğesi.</param>
    /// <param name="measure">Verilen metnin genişliği (mm, harf aralıksız).</param>
    public static TextBlockLayout Compute(TextElement text, Func<string, double> measure)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(measure);
        var size = text.Font.Size * TextPreset.MmPerPoint;
        var spacing = text.LetterSpacing * size;
        var b = text.Bounds;
        double Width(string s) => spacing == 0 || s.Length == 0
            ? measure(s)
            : Graphemes(s).Sum(g => measure(g)) + (spacing * (new StringInfo(s).LengthInTextElements - 1));

        var raw = TextLines.Wrap(text.Content, b.Width, Width);
        var step = size * (text.LineHeight ?? TextLines.DefaultLineHeight);
        var total = raw.Count * step;
        var top = text.VerticalAlignment switch
        {
            VerticalAlignment.Middle => b.Y + ((b.Height - total) / 2),
            VerticalAlignment.Bottom => b.Bottom - total,
            _ => b.Y,
        };

        var lines = new List<TextLineLayout>(raw.Count);
        double inkLeft = double.MaxValue, inkRight = double.MinValue;
        for (var i = 0; i < raw.Count; i++)
        {
            var line = raw[i];
            var width = Width(line);
            var left = text.Alignment switch
            {
                TextAlignment.Center => b.X + ((b.Width - width) / 2),
                TextAlignment.Right => b.Right - width,
                _ => b.X,
            };
            List<TextPiece> pieces = [];
            if (line.Length > 0)
            {
                if (spacing == 0)
                {
                    pieces.Add(new TextPiece(line, 0));
                }
                else
                {
                    var x = 0.0;
                    foreach (var g in Graphemes(line))
                    {
                        pieces.Add(new TextPiece(g, x));
                        x += measure(g) + spacing;
                    }
                }

                inkLeft = Math.Min(inkLeft, left);
                inkRight = Math.Max(inkRight, left + width);
            }

            lines.Add(new TextLineLayout(pieces, left, top + (i * step), width));
        }

        var ink = inkLeft <= inkRight
            ? new RectMm(inkLeft, top, Math.Max(inkRight - inkLeft, 1e-6), Math.Max(total, 1e-6))
            : new RectMm(b.X, top, b.Width, Math.Max(total, 1e-6));
        return new TextBlockLayout(lines, step, size, ink);
    }

    /// <summary>
    /// Yazının altına çizilecek kopyalar (sırasıyla). Gölge ve hale, halka biçiminde dizilmiş çok sayıda düşük opaklıklı
    /// kopyayla yumuşatılır; PDF'te bulanıklık olmadığından bu yol ekranla aynı sonucu verir. Kontur opak halkadır.
    /// </summary>
    public static IReadOnlyList<TextPass> Passes(TextEffect effect, double sizeMm)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect.IsNone || effect.Kind == TextEffectKind.Band)
        {
            return [];
        }

        var s = effect.Strength;
        var c = effect.Color;
        List<TextPass> passes = [];
        switch (effect.Kind)
        {
            case TextEffectKind.Shadow:
                var d = sizeMm * 0.055;
                var blur = sizeMm * 0.07;
                Soft(passes, d, d, [blur * 0.5, blur], c, s * 0.85);
                break;

            case TextEffectKind.Glow:
                var r = sizeMm * 0.12;
                Soft(passes, 0, 0, [r / 3, r * 2 / 3, r], c, s * 0.95);
                break;

            case TextEffectKind.Outline:
                var w = sizeMm * (0.02 + (0.05 * s));
                Ring(passes, 0, 0, w / 2, RingCopies / 2, c);
                Ring(passes, 0, 0, w, RingCopies + 4, c);
                break;

            case TextEffectKind.Emboss:
                var e = sizeMm * (0.015 + (0.025 * s));
                passes.Add(new TextPass(e, e, WithAlpha(c, 0.75 * s)));
                passes.Add(new TextPass(-e * 0.8, -e * 0.8, WithAlpha(RgbaColor.White, 0.85 * s)));
                break;

            default:
                break;
        }

        return passes;
    }

    /// <summary>Zemin bandı: yazının çevresinde payla genişletilmiş dikdörtgen (köşe yarıçapı ayrıca verilir); yoksa null.</summary>
    public static (RectMm Box, double Radius, RgbaColor Color)? Band(TextEffect effect, TextBlockLayout layout)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(layout);
        if (effect.Kind != TextEffectKind.Band || effect.IsNone)
        {
            return null;
        }

        var ph = layout.SizeMm * 0.45;
        var pv = layout.SizeMm * 0.15;
        var ink = layout.InkBox;
        return (new RectMm(ink.X - ph, ink.Y - pv, ink.Width + (2 * ph), ink.Height + (2 * pv)), layout.SizeMm * 0.18, WithAlpha(effect.Color, effect.Strength));
    }

    /// <summary>Metni grafemlere (ekranda tek karakter görünen birimlere) böler.</summary>
    public static IEnumerable<string> Graphemes(string s)
    {
        var e = StringInfo.GetTextElementEnumerator(s);
        while (e.MoveNext())
        {
            yield return e.GetTextElement();
        }
    }

    private static void Soft(List<TextPass> passes, double cx, double cy, double[] radii, RgbaColor color, double target)
    {
        // n kopya üst üste binince toplam örtücülük 1-(1-a)^n olur; merkezde hedefe ulaşacak kopya opaklığı seçilir.
        var count = 1 + (radii.Length * RingCopies);
        var alpha = 1 - Math.Pow(1 - Math.Clamp(target, 0, 0.99), 1.0 / count);
        var c = WithAlpha(color, alpha * (color.A / 255.0));
        passes.Add(new TextPass(cx, cy, c));
        foreach (var r in radii)
        {
            Ring(passes, cx, cy, r, RingCopies, c);
        }
    }

    private static void Ring(List<TextPass> passes, double cx, double cy, double radius, int count, RgbaColor color)
    {
        for (var i = 0; i < count; i++)
        {
            var a = 2 * Math.PI * i / count;
            passes.Add(new TextPass(cx + (radius * Math.Cos(a)), cy + (radius * Math.Sin(a)), color));
        }
    }

    private static RgbaColor WithAlpha(RgbaColor c, double alpha) =>
        c with { A = (byte)Math.Clamp(Math.Round(alpha * 255), 0, 255) };
}
