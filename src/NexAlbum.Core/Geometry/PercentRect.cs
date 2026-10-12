using NexAlbum.Core.Common;

namespace NexAlbum.Core.Geometry;

/// <summary>
/// Sayfa ölçüsüne göre yüzde (0–100) cinsinden dikdörtgen. Şablonların ölçüden bağımsız olmasını sağlar (Karar 09 §10.2).
/// </summary>
public readonly record struct PercentRect
{
    public PercentRect(double x, double y, double width, double height)
    {
        X = Guard.InRange(x, 0, 100, nameof(x));
        Y = Guard.InRange(y, 0, 100, nameof(y));
        Width = Guard.InRange(width, 0, 100, nameof(width));
        Height = Guard.InRange(height, 0, 100, nameof(height));
        if (Width <= 0 || Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Alan boş olamaz.");
        }

        if (X + Width > 100 + Length.Tolerance || Y + Height > 100 + Length.Tolerance)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Alan sayfa dışına taşamaz.");
        }
    }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    /// <summary>Verilen alan içinde milimetre dikdörtgene çevirir.</summary>
    public RectMm Resolve(RectMm area) => new(
        area.X + (area.Width * X / 100),
        area.Y + (area.Height * Y / 100),
        area.Width * Width / 100,
        area.Height * Height / 100);
}
