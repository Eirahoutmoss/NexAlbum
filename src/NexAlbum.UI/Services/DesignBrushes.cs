using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.Services;

/// <summary>Domain renk/dolgu/biçimlerinden WPF fırça, geometri ve efektleri (hepsi dondurulmuş).</summary>
public static class DesignBrushes
{
    public static Color ToColor(RgbaColor c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    public static Brush Solid(RgbaColor c, double opacity = 1) => Freeze(new SolidColorBrush(ToColor(c)) { Opacity = opacity });

    /// <summary>Düz renk ve gradyan dolgular; görsel fon için yer tutucu olarak beyaz döner.</summary>
    public static Brush Fill(BackgroundFill fill)
    {
        ArgumentNullException.ThrowIfNull(fill);
        return fill.Mode switch
        {
            BackgroundMode.Solid when fill.Color is { } c => Solid(c, fill.Opacity),
            BackgroundMode.Gradient when fill.Gradient is { } g => Gradient(g, fill.Opacity),
            BackgroundMode.Transparent => Brushes.Transparent,
            _ => Brushes.White,
        };
    }

    /// <summary>Açı: 0° soldan sağa, 90° yukarıdan aşağıya (PDF ile aynı kural).</summary>
    public static Brush Gradient(GradientSpec g, double opacity = 1)
    {
        ArgumentNullException.ThrowIfNull(g);
        var radians = g.AngleDegrees * Math.PI / 180;
        var dx = Math.Cos(radians);
        var dy = Math.Sin(radians);
        var half = (Math.Abs(0.5 * dx)) + Math.Abs(0.5 * dy);
        return Freeze(new LinearGradientBrush(ToColor(g.From), ToColor(g.To), new Point(0.5 - (dx * half), 0.5 - (dy * half)), new Point(0.5 + (dx * half), 0.5 + (dy * half)))
        {
            Opacity = opacity,
        });
    }

    /// <summary>Yuva biçimi: dikdörtgen, yuvarlak köşeli dikdörtgen ya da elips.</summary>
    public static Geometry Shape(SlotShape shape, double x, double y, double width, double height, double radius)
    {
        var rect = new Rect(x, y, Math.Max(0, width), Math.Max(0, height));
        Geometry geometry = shape == SlotShape.Circle
            ? new EllipseGeometry(rect)
            : new RectangleGeometry(rect, Math.Max(0, radius), Math.Max(0, radius));
        geometry.Freeze();
        return geometry;
    }

    /// <summary>
    /// Çokgen yuva biçimi (Karar 10 Rev. 1): köşeler sayfa mm'sinde; (<paramref name="originX"/>, <paramref name="originY"/>)
    /// yerel sıfır noktası olur. Boş/çökmüş çokgende boş geometri döner (hiçbir şey çizilmez).
    /// </summary>
    public static Geometry Polygon(IReadOnlyList<PointMm>? points, double originX, double originY)
    {
        if (points is not { Count: >= 3 })
        {
            return Geometry.Empty;
        }

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(points[0].X - originX, points[0].Y - originY), isFilled: true, isClosed: true);
            context.PolyLineTo([.. points.Skip(1).Select(p => new Point(p.X - originX, p.Y - originY))], isStroked: true, isSmoothJoin: false);
        }

        geometry.Freeze();
        return geometry;
    }

    /// <summary>Gölge efekti (mm cinsinden; tuval birimi mm olduğundan doğrudan kullanılır).</summary>
    public static Effect Shadow(ShadowSpec shadow)
    {
        ArgumentNullException.ThrowIfNull(shadow);
        var effect = new DropShadowEffect
        {
            Color = Color.FromRgb(shadow.Color.R, shadow.Color.G, shadow.Color.B),
            Opacity = shadow.Color.A / 255.0,
            BlurRadius = shadow.BlurMm * 2,
            ShadowDepth = Math.Sqrt((shadow.OffsetXMm * shadow.OffsetXMm) + (shadow.OffsetYMm * shadow.OffsetYMm)),
            Direction = -Math.Atan2(shadow.OffsetYMm, shadow.OffsetXMm) * 180 / Math.PI,
            RenderingBias = RenderingBias.Performance,
        };
        effect.Freeze();
        return effect;
    }

    /// <summary>Çerçeve önizlemelerinde fotoğraf yerine kullanılan yumuşak "gökyüzü" dolgusu.</summary>
    public static Brush SamplePhoto { get; } = Freeze(new LinearGradientBrush(Color.FromRgb(0x9C, 0xB8, 0xD4), Color.FromRgb(0xE9, 0xD7, 0xC0), 90));

    public static Brush EmptySlot { get; } = Freeze(new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)));

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
