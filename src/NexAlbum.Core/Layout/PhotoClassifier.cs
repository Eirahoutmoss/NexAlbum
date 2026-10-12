using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Layout;

/// <summary>Çözünürlük sınıfı (Karar 10 §6.4).</summary>
public enum ResolutionClass
{
    Low,
    Medium,
    High,
    VeryHigh,
}

/// <summary>Sınıflandırılmış fotoğraf.</summary>
public sealed record ClassifiedPhoto(PhotoAsset Asset, PageOrientation Orientation, double AspectRatio, int Index)
{
    public bool Favorite => Asset.Favorite;
}

/// <summary>Aşama 1 — fotoğraf sınıflandırma (Karar 10 §6).</summary>
public static class PhotoClassifier
{
    public const double LandscapeThreshold = 1.15;
    public const double PortraitThreshold = 0.87;

    /// <summary>En-boy oranına göre yön: &gt; 1,15 yatay, &lt; 0,87 dikey, arası kare.</summary>
    public static PageOrientation Classify(double width, double height)
    {
        var ratio = width / height;
        return ratio > LandscapeThreshold ? PageOrientation.Landscape
            : ratio < PortraitThreshold ? PageOrientation.Portrait
            : PageOrientation.Square;
    }

    public static ResolutionClass ClassifyDpi(double dpi) => dpi switch
    {
        < 150 => ResolutionClass.Low,
        < 250 => ResolutionClass.Medium,
        <= 350 => ResolutionClass.High,
        _ => ResolutionClass.VeryHigh,
    };

    public static ClassifiedPhoto Classify(PhotoAsset asset, int index)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return new ClassifiedPhoto(asset, Classify(asset.DisplayWidth, asset.DisplayHeight), asset.DisplayAspectRatio, index);
    }
}
