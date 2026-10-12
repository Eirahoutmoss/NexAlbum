using NexAlbum.Core.Common;

namespace NexAlbum.Core.Geometry;

/// <summary>
/// Milimetre cinsinden pozitif boyut.
/// </summary>
public readonly record struct SizeMm
{
    public SizeMm(double width, double height)
    {
        Width = Guard.Positive(width, nameof(width));
        Height = Guard.Positive(height, nameof(height));
    }

    public double Width { get; }

    public double Height { get; }

    /// <summary>Boyuttan türetilen yön; ayrıca saklanmaz, böylece tutarsızlık oluşamaz.</summary>
    public PageOrientation Orientation =>
        Length.NearlyEqual(Width, Height) ? PageOrientation.Square
        : Width > Height ? PageOrientation.Landscape
        : PageOrientation.Portrait;

    public double AspectRatio => Width / Height;
}
