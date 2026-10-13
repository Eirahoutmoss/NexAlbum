using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Design;

/// <summary>
/// 4×5 renk matrisi (satır: R, G, B, A; sütun: r, g, b, a, kaydırma). Değerler 0–1 ölçeğindedir (kaydırma da), önçarpımsız
/// renge uygulanır — SkiaSharp'ın renk matrisiyle aynı düzen. Ekran, PDF ve video aynı matrisi kullanır; böylece önizleme ile
/// baskı aynı görünür (efekt belgesi §4.3, §7).
/// </summary>
public sealed class ColorMatrix
{
    /// <summary>Göreli parlaklık ağırlıkları (Rec. 709).</summary>
    private const double Lr = 0.2126;
    private const double Lg = 0.7152;
    private const double Lb = 0.0722;

    private readonly double[] m;

    private ColorMatrix(double[] values) => m = values;

    public static ColorMatrix Identity => new([1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0]);

    public double this[int row, int column] => m[(row * 5) + column];

    public bool IsIdentity => Values.SequenceEqual(Identity.Values);

    public IReadOnlyList<double> Values => m;

    /// <summary>
    /// Ayarlardan matris; sıra efekt belgesi §4.3: pozlama → parlaklık → kontrast → beyaz dengesi (sıcaklık) → doygunluk /
    /// siyah-beyaz.
    /// </summary>
    public static ColorMatrix From(ImageAdjustments adjustments)
    {
        ArgumentNullException.ThrowIfNull(adjustments);
        var result = Identity;
        if (adjustments.Exposure != 0)
        {
            result = Scale(Math.Pow(2, adjustments.Exposure * 1.5)).After(result);
        }

        if (adjustments.Brightness != 0)
        {
            result = Offset(adjustments.Brightness * 0.25).After(result);
        }

        if (adjustments.Contrast != 0)
        {
            var k = 1 + adjustments.Contrast;
            result = Offset(0.5 * (1 - k)).After(Scale(k)).After(result);
        }

        if (adjustments.Temperature != 0)
        {
            var t = adjustments.Temperature * 0.14;
            result = new ColorMatrix([1 + t, 0, 0, 0, 0, 0, 1 + (t * 0.15), 0, 0, 0, 0, 0, 1 - t, 0, 0, 0, 0, 0, 1, 0]).After(result);
        }

        var saturation = adjustments.BlackWhite ? 0 : 1 + adjustments.Saturation;
        if (Math.Abs(saturation - 1) > 1e-12)
        {
            result = Saturate(saturation).After(result);
        }

        return result;
    }

    /// <summary>Doygunluk: 0 siyah-beyaz, 1 değişmez, 2 iki kat.</summary>
    public static ColorMatrix Saturate(double s)
    {
        var a = 1 - s;
        return new([
            (a * Lr) + s, a * Lg, a * Lb, 0, 0,
            a * Lr, (a * Lg) + s, a * Lb, 0, 0,
            a * Lr, a * Lg, (a * Lb) + s, 0, 0,
            0, 0, 0, 1, 0,
        ]);
    }

    /// <summary>Bileşke: önce <paramref name="before"/>, ardından bu matris uygulanır (this ∘ before).</summary>
    public ColorMatrix After(ColorMatrix before)
    {
        ArgumentNullException.ThrowIfNull(before);
        var r = new double[20];
        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 5; col++)
            {
                double sum = col == 4 ? this[row, 4] : 0;
                for (var k = 0; k < 4; k++)
                {
                    sum += this[row, k] * before[k, col];
                }

                r[(row * 5) + col] = sum;
            }
        }

        return new ColorMatrix(r);
    }

    /// <summary>(r, g, b) rengine uygular (0–1, kırpılır).</summary>
    public (double R, double G, double B) Apply(double r, double g, double b)
    {
        double Row(int i) => Math.Clamp((this[i, 0] * r) + (this[i, 1] * g) + (this[i, 2] * b) + this[i, 3] + this[i, 4], 0, 1);
        return (Row(0), Row(1), Row(2));
    }

    /// <summary>SkiaSharp için 20 elemanlı dizi.</summary>
    public float[] ToFloats() => [.. m.Select(v => (float)v)];

    private static ColorMatrix Scale(double k) => new([k, 0, 0, 0, 0, 0, k, 0, 0, 0, 0, 0, k, 0, 0, 0, 0, 0, 1, 0]);

    private static ColorMatrix Offset(double d) => new([1, 0, 0, 0, d, 0, 1, 0, 0, d, 0, 0, 1, 0, d, 0, 0, 0, 1, 0]);
}
