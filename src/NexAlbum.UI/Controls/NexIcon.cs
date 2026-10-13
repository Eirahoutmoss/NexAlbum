using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace NexAlbum.UI.Controls;

/// <summary>
/// İnce çizgi ikon (Karar 17 §6). <see cref="Data"/> 24 birimlik kutuda bir geometridir (<c>Themes/Icons.xaml</c>);
/// rengi <see cref="Foreground"/>'dan (kalıtımla düğmeden) gelir, çizgi kalınlığı kutuyla birlikte ölçeklenir.
/// </summary>
public sealed class NexIcon : FrameworkElement
{
    /// <summary>Lucide ikonlarının tasarım kutusu.</summary>
    public const double DesignSize = 24;

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(Geometry), typeof(NexIcon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(NexIcon), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(NexIcon), new FrameworkPropertyMetadata(1.6, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(NexIcon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    static NexIcon()
    {
        WidthProperty.OverrideMetadata(typeof(NexIcon), new FrameworkPropertyMetadata(18.0));
        HeightProperty.OverrideMetadata(typeof(NexIcon), new FrameworkPropertyMetadata(18.0));
        SnapsToDevicePixelsProperty.OverrideMetadata(typeof(NexIcon), new FrameworkPropertyMetadata(true));
        FocusableProperty.OverrideMetadata(typeof(NexIcon), new FrameworkPropertyMetadata(false));
    }

    public Geometry? Data
    {
        get => (Geometry?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>24 birimlik kutuda çizgi kalınlığı (Lucide varsayılanı 2; NexAlbum daha ince: 1,6).</summary>
    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>İç dolgu (ör. favori yıldızı); varsayılan yok — ikonlar çizgidir.</summary>
    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var data = Data;
        var size = Math.Min(ActualWidth, ActualHeight);
        if (data is null || size <= 0)
        {
            return;
        }

        var scale = size / DesignSize;
        var pen = new Pen(Foreground ?? Brushes.Gray, StrokeThickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
        pen.Freeze();
        drawingContext.PushTransform(new MatrixTransform(scale, 0, 0, scale, (ActualWidth - size) / 2, (ActualHeight - size) / 2));
        drawingContext.DrawGeometry(Fill, pen, data);
        drawingContext.Pop();
    }
}
