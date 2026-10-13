using NexAlbum.Core.Common;

namespace NexAlbum.Core.Geometry;

/// <summary>Sayfanın sol üst köşesine göre milimetre cinsinden nokta (y aşağı doğru artar).</summary>
public readonly record struct PointMm(double X, double Y);

/// <summary>
/// Bir dikdörtgenin içinde 0–1 aralığında normalize nokta: (0,0) sol üst, (1,1) sağ alt köşe.
/// Çokgen yuvaların köşeleri bu biçimde saklanır; yuva büyüyüp küçülse de biçim korunur (Karar 10 Rev. 1).
/// </summary>
public readonly record struct UnitPoint
{
    public UnitPoint(double x, double y)
    {
        X = Guard.InRange(x, 0, 1, nameof(x));
        Y = Guard.InRange(y, 0, 1, nameof(y));
    }

    public double X { get; }

    public double Y { get; }

    /// <summary>Kayan nokta hatasıyla aralık dışına çıkan değerleri sınıra çekerek oluşturur.</summary>
    public static UnitPoint Clamped(double x, double y) => new(Math.Clamp(x, 0, 1), Math.Clamp(y, 0, 1));
}
