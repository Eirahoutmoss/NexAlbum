using NexAlbum.Core.Common;

namespace NexAlbum.Core.Projects;

/// <summary>Metnin okunurluk etkisi (Karar 17 §11, Karar 09 §8 Rev. 2).</summary>
public enum TextEffectKind
{
    None,

    /// <summary>Sağ alta düşen yumuşak gölge.</summary>
    Shadow,

    /// <summary>Harflerin çevresinde yumuşak ışık (hale): koyu yazı açık zeminde, açık yazı koyu zeminde öne çıkar.</summary>
    Glow,

    /// <summary>Harflerin çevresinde ince, keskin çizgi.</summary>
    Outline,

    /// <summary>Yazının arkasında yarı saydam zemin bandı.</summary>
    Band,

    /// <summary>Kabartma: üst-solda ışık, alt-sağda gölge; harf kâğıttan kabarık görünür.</summary>
    Emboss,
}

/// <summary>Metin etkisi: tür, renk ve güç (0–1). Ölçüler punto ile birlikte büyür; her albüm boyutunda aynı orandadır.</summary>
public sealed record TextEffect
{
    public static TextEffect None { get; } = new();

    public TextEffectKind Kind { get; init; } = TextEffectKind.None;

    /// <summary>Etkinin rengi (gölge/hale/kontur/bant; kabartmada gölge tarafı).</summary>
    public RgbaColor Color { get; init; } = RgbaColor.Black;

    /// <summary>Güç: 0 (görünmez) – 1 (en belirgin).</summary>
    public double Strength { get; init => field = Guard.InRange(value, 0, 1, nameof(Strength)); } = 0.6;

    public bool IsNone => Kind == TextEffectKind.None || Strength <= 0;

    /// <summary>Türe uygun varsayılan renk: koyu yazının hale/kontur/bandı açık, açık yazınınki koyu; gölge ve kabartma koyu.</summary>
    public static RgbaColor DefaultColor(TextEffectKind kind, RgbaColor text) => kind switch
    {
        TextEffectKind.Glow or TextEffectKind.Outline or TextEffectKind.Band => IsDark(text) ? RgbaColor.White : RgbaColor.Black,
        _ => RgbaColor.Black,
    };

    /// <summary>
    /// "Okunur yap": koyu yazıya beyaz hale, açık yazıya koyu gölge. Fotoğrafın üstündeki yazının hangi zeminde de
    /// seçilebilmesi için en az müdahaleli etki.
    /// </summary>
    public static TextEffect Readable(RgbaColor text) => IsDark(text)
        ? new TextEffect { Kind = TextEffectKind.Glow, Color = RgbaColor.White, Strength = 0.85 }
        : new TextEffect { Kind = TextEffectKind.Shadow, Color = RgbaColor.Black, Strength = 0.75 };

    /// <summary>Göreli parlaklık (sRGB, WCAG) 0,4'ün altındaysa koyu sayılır.</summary>
    public static bool IsDark(RgbaColor c) => Luminance(c) < 0.4;

    public static double Luminance(RgbaColor c)
    {
        static double Channel(byte v)
        {
            var s = v / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(c.R)) + (0.7152 * Channel(c.G)) + (0.0722 * Channel(c.B));
    }
}
