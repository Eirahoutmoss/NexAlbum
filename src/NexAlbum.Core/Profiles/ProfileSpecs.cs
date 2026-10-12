using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Profiles;

// Karar 03 §3.1 şemasının bölümleri. Değer null ise "bilinmiyor" demektir; asla varsayılan uydurulmaz.

/// <summary>Bitmiş sayfa ölçüsü. Yön ölçüden türetilir.</summary>
public sealed record PageSpec(SizeMm? Size = null);

/// <summary>Açık çift sayfa (spread) bilgisi.</summary>
public sealed record SpreadSpec(bool Enabled = false, string? Note = null);

/// <summary>Kenar bazlı ölçü (taşma payı veya güvenli alan).</summary>
public sealed record EdgeSpec(EdgesMm? Value = null, bool Verified = false, string? Note = null)
{
    public static EdgeSpec Unknown { get; } = new();
}

/// <summary>Orta birleşim payı (cilt boşluğu).</summary>
public sealed record GutterSpec
{
    public GutterSpec(double? widthMm = null, bool verified = false, string? note = null)
    {
        WidthMm = Guard.NonNegativeOrNull(widthMm, nameof(widthMm));
        Verified = verified;
        Note = note;
    }

    public double? WidthMm { get; }

    public bool Verified { get; }

    public string? Note { get; }

    public static GutterSpec Unknown { get; } = new();
}

/// <summary>Çözünürlük beklentisi.</summary>
public sealed record ResolutionSpec
{
    public ResolutionSpec(int? targetDpi = null, int? minDpi = null, string? note = null)
    {
        TargetDpi = Guard.PositiveOrNull(targetDpi, nameof(targetDpi));
        MinDpi = Guard.PositiveOrNull(minDpi, nameof(minDpi));
        Note = note;
    }

    public int? TargetDpi { get; }

    public int? MinDpi { get; }

    public string? Note { get; }

    public static ResolutionSpec Unknown { get; } = new();
}

/// <summary>Renk modu ve ICC profili.</summary>
public sealed record ColorSpec(ColorMode? Mode = null, string? IccProfile = null, bool ProfileEmbedded = false, string? Note = null)
{
    public static ColorSpec Unknown { get; } = new();
}

/// <summary>Kesim işaretleri ve baskı kontrol öğeleri.</summary>
public sealed record OutputSpec
{
    public OutputSpec(
        bool cropMarks = false,
        double? cropMarkOffsetMm = null,
        double? cropMarkLengthMm = null,
        bool registrationMarks = false,
        bool colorBars = false,
        bool pageInfo = false)
    {
        CropMarks = cropMarks;
        CropMarkOffsetMm = Guard.NonNegativeOrNull(cropMarkOffsetMm, nameof(cropMarkOffsetMm));
        CropMarkLengthMm = Guard.PositiveOrNull(cropMarkLengthMm, nameof(cropMarkLengthMm));
        RegistrationMarks = registrationMarks;
        ColorBars = colorBars;
        PageInfo = pageInfo;
    }

    public bool CropMarks { get; }

    public double? CropMarkOffsetMm { get; }

    public double? CropMarkLengthMm { get; }

    public bool RegistrationMarks { get; }

    public bool ColorBars { get; }

    public bool PageInfo { get; }

    public static OutputSpec None { get; } = new();
}

/// <summary>Sırt genişliği. Formül uydurulmaz; kullanıcı baskı evinden alıp girer (Karar 03 §8).</summary>
public sealed record SpineSpec
{
    public SpineSpec(
        double? widthMm = null,
        SpineCalculationMethod method = SpineCalculationMethod.Manual,
        bool verified = false,
        string? note = null)
    {
        WidthMm = Guard.PositiveOrNull(widthMm, nameof(widthMm));
        Method = method;
        Verified = verified;
        Note = note;
    }

    public double? WidthMm { get; }

    public SpineCalculationMethod Method { get; }

    public bool Verified { get; }

    public string? Note { get; }
}

/// <summary>Kapak ölçüleri.</summary>
public sealed record CoverSpec(CoverType Type, SizeMm? Front = null, SizeMm? Back = null, SpineSpec? Spine = null);

/// <summary>Kâğıt bilgisi (yalnızca bilgi amaçlı).</summary>
public sealed record PaperSpec
{
    public PaperSpec(string? type = null, double? weightGsm = null, string? note = null)
    {
        Type = type;
        WeightGsm = Guard.PositiveOrNull(weightGsm, nameof(weightGsm));
        Note = note;
    }

    public string? Type { get; }

    public double? WeightGsm { get; }

    public string? Note { get; }
}

/// <summary>Cilt tipi (serbest metin: "layflat", "klasik" vb.).</summary>
public sealed record BindingSpec(string Type, string? Note = null);

/// <summary>Profile iliştirilmiş not/uyarı.</summary>
public sealed record ProfileNote(ProfileNoteLevel Level, string Message);
