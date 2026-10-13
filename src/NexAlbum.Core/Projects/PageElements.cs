using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Sayfadaki öğelerin ortak tabanı (Karar 09 §5: Slot, TextElement, BackgroundElement).
/// </summary>
public abstract record PageElement
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    /// <summary>Katman sırası; büyük olan üstte çizilir.</summary>
    public int ZIndex { get; init; }
}

/// <summary>Fotoğrafın bir yuvaya tahribatsız yerleşimi (Karar 09 §6.3). Orijinal dosyaya dokunulmaz.</summary>
public sealed record PhotoPlacement
{
    public required string AssetId { get; init => field = Guard.NotBlank(value, nameof(AssetId)); }

    /// <summary>Yatay kaydırma, yuva genişliğine göre normalize (-1..1).</summary>
    public double OffsetX { get; init => field = Guard.InRange(value, -1, 1, nameof(OffsetX)); }

    /// <summary>Dikey kaydırma, yuva yüksekliğine göre normalize (-1..1).</summary>
    public double OffsetY { get; init => field = Guard.InRange(value, -1, 1, nameof(OffsetY)); }

    /// <summary>Ölçek; 1 = kırpma moduna göre tam sığdır.</summary>
    public double Scale { get; init => field = Guard.Positive(value, nameof(Scale)); } = 1;

    /// <summary>Döndürme (derece).</summary>
    public double Rotation { get; init => field = Guard.Finite(value, nameof(Rotation)); }

    public NormalizedRect Crop { get; init; } = NormalizedRect.Full;

    public ImageAdjustments Adjustments { get; init => field = value ?? throw new ArgumentNullException(nameof(Adjustments)); } = ImageAdjustments.None;
}

/// <summary>Paspartu: yuva kenarı ile fotoğraf arasındaki renkli iç pay (mm).</summary>
public sealed record MatSpec(EdgesMm Edges, RgbaColor Color);

/// <summary>Fotoğraf yuvası (Karar 09 §6).</summary>
public sealed record Slot : PageElement
{
    public required RectMm Bounds { get; init => field = value.IsEmpty ? throw new ArgumentException("Yuva boyutu sıfır olamaz.", nameof(Bounds)) : value; }

    public CropMode CropMode { get; init; } = CropMode.Cover;

    public int? MinDpi { get; init => field = Guard.PositiveOrNull(value, nameof(MinDpi)); }

    public int? TargetDpi { get; init => field = Guard.PositiveOrNull(value, nameof(TargetDpi)); }

    public PhotoPlacement? Placement { get; init; }

    public SlotShape Shape { get; init; } = SlotShape.Rectangle;

    /// <summary>
    /// Şablondan gelen taban biçim: <see cref="Bounds"/> içinde normalize dışbükey çokgen; null = dikdörtgen.
    /// Çerçeve stilleri bunu silmez; biçimli bir çerçeve kaldırılınca yuva bu çokgene döner (Karar 10 Rev. 1).
    /// </summary>
    public IReadOnlyList<UnitPoint>? Polygon
    {
        get;
        init => field = value is null ? null : value.Count >= 3
            ? [.. value]
            : throw new ArgumentException("Çokgen en az üç köşe içermeli.", nameof(Polygon));
    }

    public double CornerRadiusMm { get; init => field = Guard.NonNegative(value, nameof(CornerRadiusMm)); }

    public BorderSpec? Border { get; init; }

    public ShadowSpec? Shadow { get; init; }

    /// <summary>Paspartu; varsa fotoğraf yuvanın içinde bu pay kadar daraltılmış alanda durur.</summary>
    public MatSpec? Mat { get; init; }

    /// <summary>Uygulanan çerçeve stilinin kimliği (yeniden uygulama ve arayüz için).</summary>
    public string? StyleId { get; init; }

    public bool IsEmpty => Placement is null;

    /// <summary>
    /// Fotoğrafın kapladığı alan: yuva eksi paspartu. Çokgen biçimlerde paspartu her kenardan eşit içe öteleme olduğundan
    /// alan, içteki çokgenin sınır kutusudur; fotoğraf bu kutuyu kaplar ve çokgene kırpılır.
    /// </summary>
    public RectMm PhotoArea => Mat is not { } m
        ? Bounds
        : SlotOutline.Photo(this) is { Count: > 0 } inner ? Geometry.Polygon.Bounds(inner) : SlotOutline.IsPolygonal(this) ? default : Bounds.Deflate(m.Edges);
}

/// <summary>Metin öğesi (Karar 09 §8).</summary>
public sealed record TextElement : PageElement
{
    public required RectMm Bounds { get; init => field = value.IsEmpty ? throw new ArgumentException("Metin alanı boyutu sıfır olamaz.", nameof(Bounds)) : value; }

    public required string Content { get; init => field = value ?? throw new ArgumentNullException(nameof(Content)); }

    public required FontSpec Font { get; init => field = value ?? throw new ArgumentNullException(nameof(Font)); }

    public TextAlignment Alignment { get; init; } = TextAlignment.Left;

    public VerticalAlignment VerticalAlignment { get; init; } = VerticalAlignment.Top;

    public double? LineHeight { get; init => field = Guard.PositiveOrNull(value, nameof(LineHeight)); }

    /// <summary>Harf aralığı: punto katı (0,1 = harfler arasına puntonun %10'u kadar boşluk; Karar 09 §8, Rev. 2).</summary>
    public double LetterSpacing { get; init => field = Guard.Finite(value, nameof(LetterSpacing)); }

    /// <summary>Okunurluk etkisi (gölge, hale, kontur, zemin bandı, kabartma); yoksa <see cref="TextEffect.None"/>.</summary>
    public TextEffect Effect { get; init => field = value ?? throw new ArgumentNullException(nameof(Effect)); } = TextEffect.None;
}

/// <summary>Tüm sayfayı kaplayan arka plan katmanı (Karar 09 §9).</summary>
public sealed record BackgroundElement : PageElement
{
    public BackgroundElement()
    {
        ZIndex = -100;
    }

    public BackgroundFill Fill { get; init => field = value ?? throw new ArgumentNullException(nameof(Fill)); } = BackgroundFill.White;
}
