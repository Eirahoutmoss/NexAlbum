using NexAlbum.Core.Common;

namespace NexAlbum.Core.Projects;

/// <summary>Renk geçişi. Açı: 0° soldan sağa, 90° yukarıdan aşağıya.</summary>
public sealed record GradientSpec(RgbaColor From, RgbaColor To, double AngleDegrees = 0);

/// <summary>
/// Sayfa veya arka plan öğesinin dolgusu (Karar 09 §9).
/// </summary>
public sealed record BackgroundFill
{
    public BackgroundMode Mode { get; init; } = BackgroundMode.Solid;

    public RgbaColor? Color { get; init; } = RgbaColor.White;

    public GradientSpec? Gradient { get; init; }

    /// <summary>Görsel modda kullanılan varlık kimliği.</summary>
    public string? ImageAssetId { get; init; }

    public double Opacity { get; init => field = Guard.InRange(value, 0, 1, nameof(Opacity)); } = 1;

    public static BackgroundFill White { get; } = new();

    public static BackgroundFill Solid(RgbaColor color) => new() { Color = color };
}

/// <summary>Çerçeve.</summary>
public sealed record BorderSpec
{
    public BorderSpec(double widthMm, RgbaColor color)
    {
        WidthMm = Guard.NonNegative(widthMm, nameof(widthMm));
        Color = color;
    }

    public double WidthMm { get; }

    public RgbaColor Color { get; }
}

/// <summary>Gölge.</summary>
public sealed record ShadowSpec(double OffsetXMm, double OffsetYMm, double BlurMm, RgbaColor Color);

/// <summary>Tahribatsız basit renk ayarları; -1 ile 1 arası, 0 = değişiklik yok.</summary>
public sealed record ImageAdjustments
{
    public double Brightness { get; init => field = Guard.InRange(value, -1, 1, nameof(Brightness)); }

    public double Contrast { get; init => field = Guard.InRange(value, -1, 1, nameof(Contrast)); }

    public double Saturation { get; init => field = Guard.InRange(value, -1, 1, nameof(Saturation)); }

    public static ImageAdjustments None { get; } = new();

    public bool IsNone => Brightness == 0 && Contrast == 0 && Saturation == 0;
}

/// <summary>Yazı tipi.</summary>
public sealed record FontSpec
{
    public required string Family { get; init => field = Guard.NotBlank(value, nameof(Family)); }

    /// <summary>Punto (pt).</summary>
    public double Size { get; init => field = Guard.Positive(value, nameof(Size)); } = 12;

    public int Weight { get; init => field = Guard.InRange(value, 1, 1000, nameof(Weight)); } = 400;

    public bool Italic { get; init; }

    public RgbaColor Color { get; init; } = RgbaColor.Black;
}
