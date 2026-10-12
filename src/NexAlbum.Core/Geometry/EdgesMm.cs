using NexAlbum.Core.Common;

namespace NexAlbum.Core.Geometry;

/// <summary>
/// Dört kenar için ayrı ayrı negatif olmayan milimetre değerleri (taşma payı, güvenli alan).
/// </summary>
public readonly record struct EdgesMm
{
    public EdgesMm(double top, double right, double bottom, double left)
    {
        Top = Guard.NonNegative(top, nameof(top));
        Right = Guard.NonNegative(right, nameof(right));
        Bottom = Guard.NonNegative(bottom, nameof(bottom));
        Left = Guard.NonNegative(left, nameof(left));
    }

    public double Top { get; }

    public double Right { get; }

    public double Bottom { get; }

    public double Left { get; }

    public static EdgesMm Uniform(double value) => new(value, value, value, value);

    public double Min => Math.Min(Math.Min(Top, Right), Math.Min(Bottom, Left));

    public double Max => Math.Max(Math.Max(Top, Right), Math.Max(Bottom, Left));

    public bool IsUniform =>
        Length.NearlyEqual(Top, Right) && Length.NearlyEqual(Top, Bottom) && Length.NearlyEqual(Top, Left);
}
