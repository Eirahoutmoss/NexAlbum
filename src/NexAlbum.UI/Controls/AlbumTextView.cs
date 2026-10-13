using System.Globalization;
using System.Windows;
using System.Windows.Media;
using NexAlbum.Core.Common;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.Controls;

/// <summary>
/// Sayfadaki metni çizer (birim mm; sayfanın Viewbox'ı ölçekler). Satır kırma, hizalama, harf aralığı ve etkiler
/// PDF ile ortak hesaptan gelir (<see cref="TextLayout"/>); böylece ekranda görülen baskıda da aynıdır.
/// Etkiler kutunun dışına taşabilir; tıklama alanı kutunun tamamıdır.
/// </summary>
public sealed class AlbumTextView : FrameworkElement
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(TextElement), typeof(AlbumTextView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((AlbumTextView)d).cache = null));

    private Drawing? cache;

    public TextElement? Text
    {
        get => (TextElement?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Verilen metnin genişliği (mm): yazı tipi gömülüyse uygulama kaynağından ölçülür.</summary>
    public static Func<string, double> Measurer(TextElement text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var typeface = TypefaceOf(text);
        var size = text.Font.Size * TextPreset.MmPerPoint;
        return s => s.Length == 0
            ? 0
            : new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size, Brushes.Black, 1.0).WidthIncludingTrailingWhitespace;
    }

    /// <summary>Metnin çizimi (kutunun sol üst köşesi = 0,0 mm). Testler ve önizlemeler de kullanır.</summary>
    public static Drawing Render(TextElement text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var typeface = TypefaceOf(text);
        var size = text.Font.Size * TextPreset.MmPerPoint;
        var layout = TextLayout.Compute(text, Measurer(text));
        var ox = text.Bounds.X;
        var oy = text.Bounds.Y;

        var glyphs = new GeometryGroup { FillRule = FillRule.Nonzero };
        foreach (var line in layout.Lines)
        {
            foreach (var piece in line.Pieces)
            {
                var ft = new FormattedText(piece.Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size, Brushes.Black, 1.0);

                // PDF'teki gibi: yazı tipinin satır kutusu satır yüksekliğinin içinde dikeyde ortalanır.
                var origin = new Point(line.Left + piece.X - ox, line.Top - oy + ((layout.Step - ft.Height) / 2));
                glyphs.Children.Add(ft.BuildGeometry(origin));
            }
        }

        glyphs.Freeze();
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            if (TextLayout.Band(text.Effect, layout) is { } band)
            {
                var r = band.Box;
                dc.DrawRoundedRectangle(ToBrush(band.Color), null, new Rect(r.X - ox, r.Y - oy, r.Width, r.Height), band.Radius, band.Radius);
            }

            foreach (var pass in TextLayout.Passes(text.Effect, size))
            {
                dc.PushTransform(new TranslateTransform(pass.Dx, pass.Dy));
                dc.DrawGeometry(ToBrush(pass.Color), null, glyphs);
                dc.Pop();
            }

            dc.DrawGeometry(ToBrush(text.Font.Color), null, glyphs);
        }

        group.Freeze();
        return group;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);

        // Tıklama alanı: harflerin arası da metne aittir.
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
        if (Text is not { } text)
        {
            return;
        }

        cache ??= Render(text);
        drawingContext.DrawDrawing(cache);
    }

    private static Typeface TypefaceOf(TextElement text) => new(
        AlbumFonts.ToWpf(text.Font.Family),
        text.Font.Italic ? FontStyles.Italic : FontStyles.Normal,
        FontWeight.FromOpenTypeWeight(Math.Clamp(text.Font.Weight, 1, 999)),
        FontStretches.Normal);

    private static SolidColorBrush ToBrush(RgbaColor c)
    {
        var brush = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        brush.Freeze();
        return brush;
    }
}
