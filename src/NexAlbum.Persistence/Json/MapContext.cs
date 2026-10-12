using NexAlbum.Core.Geometry;

namespace NexAlbum.Persistence.Json;

/// <summary>
/// Okuma sırasında eksik/geçersiz alanları yol bilgisiyle raporlayan yardımcılar.
/// </summary>
internal static class MapContext
{
    public static readonly TextEnum<LengthUnit> Units = new(
        (LengthUnit.Millimeter, "mm"),
        (LengthUnit.Centimeter, "cm"),
        (LengthUnit.Inch, "inch"),
        (LengthUnit.Pixel, "px"));

    public static T Required<T>(T? value, string path)
        where T : class
        => value ?? throw new FormatException($"'{path}' alanı eksik.");

    public static T RequiredValue<T>(T? value, string path)
        where T : struct
        => value ?? throw new FormatException($"'{path}' alanı eksik.");

    /// <summary>Birimli bir değeri milimetreye çevirir. Birim yazılmamışsa mm kabul edilir; piksel reddedilir (DPI bilinmez).</summary>
    public static double ToMm(double value, string? unit, string path)
    {
        var parsed = Units.Parse(unit, path + ".unit", LengthUnit.Millimeter);
        if (parsed == LengthUnit.Pixel)
        {
            throw new FormatException($"'{path}' piksel cinsinden; fiziksel ölçüye çevirmek için DPI bilinmiyor.");
        }

        return Length.ToMm(value, parsed);
    }

    public static double? ToMmOrNull(double? value, string? unit, string path) =>
        value is { } v ? ToMm(v, unit, path) : null;

    /// <summary>Alt nesne eşlemesini yol bilgisiyle sarar; domain doğrulama hatalarını anlamlı biçime çevirir.</summary>
    public static T At<T>(string path, Func<T> map)
    {
        try
        {
            return map();
        }
        catch (ArgumentException ex)
        {
            throw new FormatException($"'{path}': {ex.Message}", ex);
        }
    }
}
