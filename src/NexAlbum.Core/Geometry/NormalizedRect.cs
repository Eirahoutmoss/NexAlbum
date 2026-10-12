using NexAlbum.Core.Common;

namespace NexAlbum.Core.Geometry;

/// <summary>
/// 0–1 aralığında normalize dikdörtgen (fotoğraf kırpma, Karar 09 §6.3).
/// </summary>
public readonly record struct NormalizedRect
{
    public NormalizedRect(double x, double y, double width, double height)
    {
        X = Guard.InRange(x, 0, 1, nameof(x));
        Y = Guard.InRange(y, 0, 1, nameof(y));
        Width = Guard.InRange(width, 0, 1, nameof(width));
        Height = Guard.InRange(height, 0, 1, nameof(height));
        if (Width <= 0 || Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Kırpma alanı boş olamaz.");
        }

        if (X + Width > 1 + Length.Tolerance || Y + Height > 1 + Length.Tolerance)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Kırpma alanı görüntü dışına taşamaz.");
        }
    }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    /// <summary>Kırpmasız tüm görüntü.</summary>
    public static NormalizedRect Full { get; } = new(0, 0, 1, 1);
}
