using NexAlbum.Core.Effects;
using SkiaSharp;

namespace NexAlbum.Rendering.Effects;

/// <summary>
/// <see cref="EffectFrame"/>'i SkiaSharp ile uygular (Karar 23): katman dönüşümleri, opaklık, bulanıklık, renk (doygunluk,
/// parlaklık), gölge, giren katmanın maskesi ve üst katmanlar. Katmanların içeriği çağıranın verdiği çizim işlevleridir;
/// işlevler kareyi (0,0)–(w,h) alanına dönüşümsüz çizer.
/// </summary>
public static class EffectPainter
{
    public static void Paint(SKCanvas canvas, float width, float height, EffectFrame frame, Action<SKCanvas> drawFrom, Action<SKCanvas> drawTo)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(drawFrom);
        ArgumentNullException.ThrowIfNull(drawTo);
        if (frame.ToOnTop)
        {
            DrawLayer(canvas, width, height, frame.From, null, drawFrom);
            DrawLayer(canvas, width, height, frame.To, frame.Mask, drawTo);
        }
        else
        {
            DrawLayer(canvas, width, height, frame.To, frame.Mask, drawTo);
            DrawLayer(canvas, width, height, frame.From, null, drawFrom);
        }

        foreach (var overlay in frame.Overlays)
        {
            DrawOverlay(canvas, width, height, overlay);
        }
    }

    /// <summary>Doygunluk ve parlaklık renk matrisi; ikisi de 1 ise null.</summary>
    public static SKColorFilter? ColorFilter(double saturation, double brightness)
    {
        if (Math.Abs(saturation - 1) < 1e-4 && Math.Abs(brightness - 1) < 1e-4)
        {
            return null;
        }

        var s = (float)saturation;
        var b = (float)brightness;
        const float R = 0.2126f, G = 0.7152f, B = 0.0722f;
        return SKColorFilter.CreateColorMatrix(
        [
            b * (R + ((1 - R) * s)), b * (G * (1 - s)), b * (B * (1 - s)), 0, 0,
            b * (R * (1 - s)), b * (G + ((1 - G) * s)), b * (B * (1 - s)), 0, 0,
            b * (R * (1 - s)), b * (G * (1 - s)), b * (B + ((1 - B) * s)), 0, 0,
            0, 0, 0, 1, 0,
        ]);
    }

    /// <summary>Çokgenlerin birleşimi (piksel; efekt alanında 1 birim = yükseklik).</summary>
    public static SKPath MaskPath(IReadOnlyList<IReadOnlyList<EffectPoint>> mask, float height)
    {
        ArgumentNullException.ThrowIfNull(mask);
        var path = new SKPath { FillType = SKPathFillType.Winding };
        foreach (var poly in mask)
        {
            if (poly.Count < 3)
            {
                continue;
            }

            path.MoveTo((float)(poly[0].X * height), (float)(poly[0].Y * height));
            for (var i = 1; i < poly.Count; i++)
            {
                path.LineTo((float)(poly[i].X * height), (float)(poly[i].Y * height));
            }

            path.Close();
        }

        return path;
    }

    private static void DrawLayer(SKCanvas canvas, float width, float height, EffectLayer layer, IReadOnlyList<IReadOnlyList<EffectPoint>>? mask, Action<SKCanvas> draw)
    {
        if (!layer.Visible)
        {
            return;
        }

        canvas.Save();
        if (mask is not null)
        {
            using var path = MaskPath(mask, height);
            if (path.IsEmpty)
            {
                canvas.Restore();
                return;
            }

            canvas.ClipPath(path, antialias: true);
        }

        var px = (float)(layer.PivotX * height);
        var py = (float)(layer.PivotY * height);
        canvas.Translate((float)(layer.OffsetX * height), (float)(layer.OffsetY * height));
        if (Math.Abs(layer.Rotation) > 1e-6)
        {
            canvas.RotateDegrees((float)layer.Rotation, px, py);
        }

        var sx = (float)(layer.Scale * layer.ScaleX);
        var sy = (float)(layer.Scale * layer.ScaleY);
        if (Math.Abs(sx - 1) > 1e-6 || Math.Abs(sy - 1) > 1e-6)
        {
            canvas.Scale(sx, sy, px, py);
        }

        if (layer.Shadow > 0.001)
        {
            var unit = Math.Min(width, height);
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, unit * 0.03f);
            using var shadow = new SKPaint { Color = SKColors.Black.WithAlpha((byte)Math.Round(Math.Clamp(layer.Shadow, 0, 1) * 150)), MaskFilter = blur };
            canvas.DrawRect(unit * 0.01f, unit * 0.025f, width, height, shadow);
        }

        using var colorFilter = ColorFilter(layer.Saturation, layer.Brightness);
        var sigma = (float)(layer.Blur * Math.Min(width, height));
        using var blurFilter = sigma > 0.3f ? SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp) : null;
        var layered = layer.Opacity < 0.999 || colorFilter is not null || blurFilter is not null;
        if (layered)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.White.WithAlpha((byte)Math.Round(Math.Clamp(layer.Opacity, 0, 1) * 255)),
                ColorFilter = colorFilter,
                ImageFilter = blurFilter,
            };
            canvas.SaveLayer(paint);
            draw(canvas);
            canvas.Restore();
        }
        else
        {
            draw(canvas);
        }

        canvas.Restore();
    }

    private static void DrawOverlay(SKCanvas canvas, float width, float height, EffectOverlay overlay)
    {
        switch (overlay)
        {
            case RingOverlay ring when ring.Alpha > 0.001 && ring.R > 0.0005:
                using (var paint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = Math.Max(1f, (float)(ring.Width * height)),
                    Color = Color(ring.Color, ring.Alpha),
                })
                {
                    canvas.DrawCircle((float)(ring.Cx * height), (float)(ring.Cy * height), (float)(ring.R * height), paint);
                }

                break;

            case SolidOverlay solid when solid.Alpha > 0.001:
                using (var paint = new SKPaint { Color = Color(solid.Color, solid.Alpha) })
                {
                    canvas.DrawRect(0, 0, width, height, paint);
                }

                break;

            case GlowOverlay glow when glow.Alpha > 0.001 && glow.R > 0:
                var center = new SKPoint((float)(glow.Cx * height), (float)(glow.Cy * height));
                using (var shader = SKShader.CreateRadialGradient(center, (float)(glow.R * height), [Color(glow.Color, glow.Alpha), Color(glow.Color, 0)], SKShaderTileMode.Clamp))
                using (var paint = new SKPaint { Shader = shader, BlendMode = SKBlendMode.Screen })
                {
                    canvas.DrawRect(0, 0, width, height, paint);
                }

                break;

            case BandOverlay band when Math.Max(band.Alpha0, band.Alpha1) > 0.001 && Math.Abs(band.X1 - band.X0) > 0.0001:
                var x0 = (float)(band.X0 * height);
                var x1 = (float)(band.X1 * height);
                using (var shader = SKShader.CreateLinearGradient(new SKPoint(x0, 0), new SKPoint(x1, 0), [Color(band.Color, band.Alpha0), Color(band.Color, band.Alpha1)], SKShaderTileMode.Clamp))
                using (var paint = new SKPaint { Shader = shader })
                {
                    canvas.DrawRect(Math.Min(x0, x1), 0, Math.Abs(x1 - x0), height, paint);
                }

                break;
        }
    }

    private static SKColor Color(Core.Common.RgbaColor c, double alpha) =>
        new(c.R, c.G, c.B, (byte)Math.Round(Math.Clamp(alpha, 0, 1) * c.A));
}
