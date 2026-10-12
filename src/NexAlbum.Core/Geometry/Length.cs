using NexAlbum.Core.Common;

namespace NexAlbum.Core.Geometry;

/// <summary>
/// Birim dönüşümleri ve çözünürlük hesapları. Domain'in temel birimi milimetredir (Karar 09).
/// </summary>
public static class Length
{
    /// <summary>1 inç = 25,4 mm (tanım gereği kesin).</summary>
    public const double MmPerInch = 25.4;

    /// <summary>Ölçü karşılaştırmalarında kullanılan tolerans (mm).</summary>
    public const double Tolerance = 1e-6;

    /// <summary>Verilen birimdeki değeri milimetreye çevirir. Piksel için DPI zorunludur; uydurulmaz.</summary>
    public static double ToMm(double value, LengthUnit unit, double? dpi = null)
    {
        Guard.Finite(value, nameof(value));
        return unit switch
        {
            LengthUnit.Millimeter => value,
            LengthUnit.Centimeter => value * 10,
            LengthUnit.Inch => value * MmPerInch,
            LengthUnit.Pixel => value / RequireDpi(dpi) * MmPerInch,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Bilinmeyen birim."),
        };
    }

    /// <summary>Milimetreyi verilen birime çevirir. Piksel için DPI zorunludur.</summary>
    public static double FromMm(double mm, LengthUnit unit, double? dpi = null)
    {
        Guard.Finite(mm, nameof(mm));
        return unit switch
        {
            LengthUnit.Millimeter => mm,
            LengthUnit.Centimeter => mm / 10,
            LengthUnit.Inch => mm / MmPerInch,
            LengthUnit.Pixel => mm / MmPerInch * RequireDpi(dpi),
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Bilinmeyen birim."),
        };
    }

    /// <summary>
    /// Belirli bir fiziksel uzunluğa yayılan piksel sayısının etkin çözünürlüğü (DPI).
    /// </summary>
    public static double EffectiveDpi(double pixels, double mm)
    {
        Guard.NonNegative(pixels, nameof(pixels));
        Guard.Positive(mm, nameof(mm));
        return pixels / (mm / MmPerInch);
    }

    /// <summary>İki ölçünün tolerans içinde eşit olup olmadığı.</summary>
    public static bool NearlyEqual(double a, double b) => Math.Abs(a - b) <= Tolerance;

    private static double RequireDpi(double? dpi)
    {
        if (dpi is null)
        {
            throw new ArgumentException("Piksel dönüşümü için DPI gerekli; varsayılan değer uydurulmaz.", nameof(dpi));
        }

        return Guard.Positive(dpi.Value, nameof(dpi));
    }
}
