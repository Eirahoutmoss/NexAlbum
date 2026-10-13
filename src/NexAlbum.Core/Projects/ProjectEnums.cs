namespace NexAlbum.Core.Projects;

/// <summary>Sayfa tipi (Karar 09 §5.3). Kapak ve sırt aynı yapıyı kullanır.</summary>
public enum PageType
{
    Content,
    FrontCover,
    BackCover,
    Spine,
    Title,
}

public enum SpreadPosition
{
    Single,
    Left,
    Right,
}

/// <summary>Fotoğrafın yuvaya sığdırılma biçimi.</summary>
public enum CropMode
{
    Cover,
    Contain,
    Fill,
}

/// <summary>
/// Yuva biçimi (Karar 10 Rev. 1). İlk üçü doğrudan çizilir; diğerleri <see cref="SlotOutline"/> ile çokgene çevrilir.
/// Sıra dosya biçimini etkilemez (metin olarak saklanır) ama yeni değerler sona eklenir.
/// </summary>
public enum SlotShape
{
    Rectangle,

    /// <summary>Yuvaya içten teğet elips (kare yuvada daire).</summary>
    Circle,

    /// <summary>Yuvarlak köşeli dikdörtgen (<see cref="Slot.CornerRadiusMm"/>).</summary>
    Rounded,

    /// <summary>Şablonun tanımladığı serbest dışbükey çokgen (<see cref="Slot.Polygon"/>): üçgen, çapraz kesim, petek…</summary>
    Polygon,

    /// <summary>Tepesi yukarıda üçgen.</summary>
    Triangle,

    /// <summary>Eşkenar dörtgen (elmas).</summary>
    Diamond,

    /// <summary>Düz tepeli altıgen; yuva oranına göre düzgün altıgene yakın tutulur.</summary>
    Hexagon,

    /// <summary>Köşeleri kesik dikdörtgen (sekizgen).</summary>
    Octagon,

    /// <summary>Üstü yarım daire kemer.</summary>
    Arch,

    /// <summary>Eğik paralelkenar.</summary>
    Slant,
}

public enum TextAlignment
{
    Left,
    Center,
    Right,
    Justify,
}

public enum VerticalAlignment
{
    Top,
    Middle,
    Bottom,
}

public enum BackgroundMode
{
    Solid,
    Gradient,
    Image,
    Transparent,
}

public enum AssetSourceType
{
    /// <summary>Fotoğraf orijinal konumunda (varsayılan).</summary>
    Reference,

    /// <summary>Fotoğraf proje paketine gömülü (taşınabilir).</summary>
    Embedded,
}

/// <summary>Varlığın albümdeki rolü.</summary>
public enum AssetRole
{
    /// <summary>Yuvalara yerleşen fotoğraf (varsayılan).</summary>
    Photo,

    /// <summary>Sayfa fonu olarak kullanılan görsel; otomatik yerleşim onu fotoğraf olarak kullanmaz.</summary>
    Background,
}
