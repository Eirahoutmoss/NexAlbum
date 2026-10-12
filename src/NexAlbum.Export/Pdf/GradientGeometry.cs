using NexAlbum.Core.Geometry;

namespace NexAlbum.Export.Pdf;

/// <summary>Doğrusal gradyanın başlangıç ve bitiş noktaları.</summary>
public static class GradientGeometry
{
    /// <summary>
    /// Açı: 0° soldan sağa, 90° yukarıdan aşağıya (y aşağı doğru artar). Çizgi alanın merkezinden geçer ve
    /// alanı tam kaplayacak uzunluktadır; böylece köşeler de geçişin içinde kalır.
    /// </summary>
    public static MarkLine Line(RectMm area, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180;
        var dx = Math.Cos(radians);
        var dy = Math.Sin(radians);
        var half = (Math.Abs(area.Width / 2 * dx)) + Math.Abs(area.Height / 2 * dy);
        var cx = area.X + (area.Width / 2);
        var cy = area.Y + (area.Height / 2);
        return new MarkLine(cx - (dx * half), cy - (dy * half), cx + (dx * half), cy + (dy * half));
    }
}
