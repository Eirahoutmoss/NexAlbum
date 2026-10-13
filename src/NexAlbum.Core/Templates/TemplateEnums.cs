namespace NexAlbum.Core.Templates;

/// <summary>Şablon kategorileri (Karar 09 §10.3).</summary>
public enum TemplateCategory
{
    Single,
    Grid,
    Collage,
    Featured,
    FullBleed,
    Minimal,
    Cover,
    Spine,
    Custom,

    /// <summary>Üçgen, çapraz kesim, elmas, petek ve kemer yuvalı düzenler (Karar 10 Rev. 1).</summary>
    Geometric,
}

/// <summary>Şablonun uygun olduğu sayfa yönü.</summary>
public enum TemplateOrientation
{
    Any,
    Portrait,
    Landscape,
    Square,
}
