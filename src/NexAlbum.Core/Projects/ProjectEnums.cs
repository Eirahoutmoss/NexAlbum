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

public enum SlotShape
{
    Rectangle,
    Circle,
    Rounded,
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
