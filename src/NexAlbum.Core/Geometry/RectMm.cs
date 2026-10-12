using NexAlbum.Core.Common;

namespace NexAlbum.Core.Geometry;

/// <summary>
/// Sayfanın sol üst köşesine göre milimetre cinsinden dikdörtgen.
/// </summary>
public readonly record struct RectMm
{
    public RectMm(double x, double y, double width, double height)
    {
        X = Guard.Finite(x, nameof(x));
        Y = Guard.Finite(y, nameof(y));
        Width = Guard.NonNegative(width, nameof(width));
        Height = Guard.NonNegative(height, nameof(height));
    }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public bool IsEmpty => Width <= Length.Tolerance || Height <= Length.Tolerance;

    public static RectMm FromSize(SizeMm size) => new(0, 0, size.Width, size.Height);

    /// <summary>Diğer dikdörtgen tamamen bunun içinde mi (tolerans dahil)?</summary>
    public bool Contains(RectMm other) =>
        other.X >= X - Length.Tolerance
        && other.Y >= Y - Length.Tolerance
        && other.Right <= Right + Length.Tolerance
        && other.Bottom <= Bottom + Length.Tolerance;

    /// <summary>İki dikdörtgen pozitif alanla kesişiyor mu? Yalnızca kenar teması kesişim sayılmaz.</summary>
    public bool Intersects(RectMm other) =>
        X < other.Right - Length.Tolerance
        && other.X < Right - Length.Tolerance
        && Y < other.Bottom - Length.Tolerance
        && other.Y < Bottom - Length.Tolerance;

    /// <summary>Kenarlardan içe doğru daraltır (ör. güvenli alan). Sonuç negatif olamaz.</summary>
    public RectMm Deflate(EdgesMm edges) => new(
        X + edges.Left,
        Y + edges.Top,
        Math.Max(0, Width - edges.Left - edges.Right),
        Math.Max(0, Height - edges.Top - edges.Bottom));

    /// <summary>Kenarlardan dışa doğru genişletir (ör. taşma payı).</summary>
    public RectMm Inflate(EdgesMm edges) => new(
        X - edges.Left,
        Y - edges.Top,
        Width + edges.Left + edges.Right,
        Height + edges.Top + edges.Bottom);
}
