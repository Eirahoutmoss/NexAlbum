using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Sayfadaki metnin çizim modeli (birim mm; sayfanın Viewbox'ı ekrana ölçekler). Satır yüksekliği sabittir
/// (punto × satır aralığı) ve satır bloğu kutuda hizalanır: PDF ile aynı kural (<see cref="TextLines"/>).
/// </summary>
public sealed class TextItemViewModel : ObservableObject
{
    private double x;
    private double y;
    private bool isSelected;

    /// <param name="text">Metin öğesi.</param>
    /// <param name="bounds">Kutunun tuvaldeki yeri (taşma payı kökenine göre, mm).</param>
    public TextItemViewModel(TextElement text, RectMm bounds)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        x = bounds.X;
        y = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;
        FontFamily = AlbumFonts.ToWpf(text.Font.Family);
        FontSize = text.Font.Size * TextPreset.MmPerPoint;
        LineHeight = FontSize * (text.LineHeight ?? TextLines.DefaultLineHeight);
        FontWeight = FontWeight.FromOpenTypeWeight(Math.Clamp(text.Font.Weight, 1, 999));
        FontStyle = text.Font.Italic ? FontStyles.Italic : FontStyles.Normal;
        var color = text.Font.Color;
        var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        brush.Freeze();
        Foreground = brush;
        Alignment = text.Alignment switch
        {
            Core.Projects.TextAlignment.Center => System.Windows.TextAlignment.Center,
            Core.Projects.TextAlignment.Right => System.Windows.TextAlignment.Right,
            Core.Projects.TextAlignment.Justify => System.Windows.TextAlignment.Justify,
            _ => System.Windows.TextAlignment.Left,
        };
        VerticalAlignment = text.VerticalAlignment switch
        {
            Core.Projects.VerticalAlignment.Middle => System.Windows.VerticalAlignment.Center,
            Core.Projects.VerticalAlignment.Bottom => System.Windows.VerticalAlignment.Bottom,
            _ => System.Windows.VerticalAlignment.Top,
        };
    }

    public TextElement Text { get; }

    public string Id => Text.Id;

    public string Content => Text.Content;

    /// <summary>Sol kenar (mm); sürüklerken önizleme için değişir, bırakınca proje güncellenir.</summary>
    public double X
    {
        get => x;
        set => SetProperty(ref x, value);
    }

    public double Y
    {
        get => y;
        set => SetProperty(ref y, value);
    }

    public double Width { get; }

    public double Height { get; }

    public FontFamily FontFamily { get; }

    /// <summary>Yazı boyu (mm).</summary>
    public double FontSize { get; }

    /// <summary>Sabit satır yüksekliği (mm).</summary>
    public double LineHeight { get; }

    public FontWeight FontWeight { get; }

    public FontStyle FontStyle { get; }

    public Brush Foreground { get; }

    public System.Windows.TextAlignment Alignment { get; }

    public System.Windows.VerticalAlignment VerticalAlignment { get; }

    /// <summary>Seçili metin (yalnızca ekranda kesik altın çerçeve).</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    /// <summary>Seçim çerçevesinin çizgi kalınlığı (mm): sayfa küçük görünse de ince kalır.</summary>
    public static double OutlineThickness => 0.5;
}
