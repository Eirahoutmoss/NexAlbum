using System.Globalization;
using System.Text;

namespace NexAlbum.Core.Effects;

/// <summary>
/// Efekt karesinin kısa, kültürden bağımsız metni (4 ondalık). C# hesabı ile HTML provadaki JavaScript karşılığının aynı
/// sonucu verdiğini denetlemek için iki tarafta da aynı biçim üretilir.
/// </summary>
public static class EffectDescriber
{
    public static string Describe(EffectFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var sb = new StringBuilder();
        Layer(sb, "F", frame.From);
        Layer(sb, "T", frame.To);
        sb.Append(frame.ToOnTop ? "top;" : "under;");
        if (frame.Mask is null)
        {
            sb.Append("mask:none;");
        }
        else
        {
            sb.Append("mask:");
            foreach (var poly in frame.Mask)
            {
                sb.Append('[');
                foreach (var p in poly)
                {
                    sb.Append(N(p.X)).Append(',').Append(N(p.Y)).Append(' ');
                }

                sb.Append(']');
            }

            sb.Append(';');
        }

        foreach (var o in frame.Overlays)
        {
            sb.Append(o switch
            {
                RingOverlay r => "ring " + N(r.Cx) + " " + N(r.Cy) + " " + N(r.R) + " " + N(r.Width) + " " + N(r.Alpha),
                SolidOverlay s => "solid " + s.Color.R.ToString(CultureInfo.InvariantCulture) + " " + N(s.Alpha),
                GlowOverlay g => "glow " + N(g.Cx) + " " + N(g.Cy) + " " + N(g.R) + " " + N(g.Alpha),
                BandOverlay b => "band " + N(b.X0) + " " + N(b.X1) + " " + N(b.Alpha0) + " " + N(b.Alpha1),
                _ => "?",
            }).Append(';');
        }

        return sb.ToString();
    }

    private static void Layer(StringBuilder sb, string name, EffectLayer l) =>
        sb.Append(name).Append(':')
            .Append(N(l.Scale)).Append(' ').Append(N(l.ScaleX)).Append(' ').Append(N(l.ScaleY)).Append(' ')
            .Append(N(l.OffsetX)).Append(' ').Append(N(l.OffsetY)).Append(' ').Append(N(l.Rotation)).Append(' ')
            .Append(N(l.PivotX)).Append(' ').Append(N(l.PivotY)).Append(' ').Append(N(l.Opacity)).Append(' ')
            .Append(N(l.Blur)).Append(' ').Append(N(l.Saturation)).Append(' ').Append(N(l.Brightness)).Append(' ')
            .Append(N(l.Shadow)).Append(';');

    /// <summary>4 ondalık; −0 yazılmaz.</summary>
    private static string N(double v)
    {
        var r = Math.Round(v, 4, MidpointRounding.AwayFromZero);
        return (r == 0 ? 0 : r).ToString("0.####", CultureInfo.InvariantCulture);
    }
}
