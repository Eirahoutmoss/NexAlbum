using System.Globalization;

namespace NexAlbum.Core.Common;

/// <summary>
/// sRGB renk. Dosyalarda "#RRGGBB" veya "#RRGGBBAA" olarak yazılır.
/// </summary>
public readonly record struct RgbaColor(byte R, byte G, byte B, byte A = 255)
{
    public static RgbaColor White { get; } = new(255, 255, 255);

    public static RgbaColor Black { get; } = new(0, 0, 0);

    public static RgbaColor Transparent { get; } = new(0, 0, 0, 0);

    public static RgbaColor Parse(string hex)
    {
        if (!TryParse(hex, out var color))
        {
            throw new FormatException($"Geçersiz renk: '{hex}'. Beklenen biçim #RRGGBB veya #RRGGBBAA.");
        }

        return color;
    }

    public static bool TryParse(string? hex, out RgbaColor color)
    {
        color = default;
        if (hex is null || hex.Length is not (7 or 9) || hex[0] != '#')
        {
            return false;
        }

        var span = hex.AsSpan(1);
        if (!TryByte(span[..2], out var r) || !TryByte(span[2..4], out var g) || !TryByte(span[4..6], out var b))
        {
            return false;
        }

        byte a = 255;
        if (span.Length == 8 && !TryByte(span[6..8], out a))
        {
            return false;
        }

        color = new RgbaColor(r, g, b, a);
        return true;
    }

    public string ToHex() => A == 255
        ? string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}")
        : string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}{A:X2}");

    public override string ToString() => ToHex();

    private static bool TryByte(ReadOnlySpan<char> s, out byte value) =>
        byte.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
}
