using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Tuval yakınlaştırması (Karar 12 §10.3). %100 = gerçek baskı boyutu (1 mm = 96/25,4 DIP). "Sığdır" modunda sayfa
/// görünür alana sığar ve pencere boyutu değişince yeniden hesaplanır. Fotoğraf ve sayfa çiziminde animasyon yoktur (§17.3).
/// </summary>
public sealed class CanvasZoomViewModel : ObservableObject
{
    /// <summary>Bir milimetrenin ekran birimi (DIP) karşılığı.</summary>
    public const double DipPerMm = 96 / 25.4;

    /// <summary>Sığdır modunda sayfanın çevresinde bırakılan boşluk (DIP).</summary>
    public const double FitPadding = 48;

    public const double MinPercent = 10;

    public const double MaxPercent = 800;

    private static readonly double[] Levels = [10, 25, 33, 50, 67, 75, 100, 125, 150, 200, 300, 400, 600, 800];

    private double pageWidth;
    private double pageHeight;
    private double viewportWidth;
    private double viewportHeight;
    private bool isFit = true;
    private double percent = 100;

    public CanvasZoomViewModel()
    {
        ZoomInCommand = new RelayCommand(ZoomIn, () => HasPage && Percent < MaxPercent);
        ZoomOutCommand = new RelayCommand(ZoomOut, () => HasPage && Percent > MinPercent);
        FitCommand = new RelayCommand(Fit, () => HasPage);
        ActualSizeCommand = new RelayCommand(() => SetPercent(100), () => HasPage);
    }

    public RelayCommand ZoomInCommand { get; }

    public RelayCommand ZoomOutCommand { get; }

    public RelayCommand FitCommand { get; }

    public RelayCommand ActualSizeCommand { get; }

    public bool HasPage => pageWidth > 0 && pageHeight > 0;

    public bool IsFit => isFit;

    /// <summary>Etkin yakınlaştırma yüzdesi (sığdır modunda hesaplanan değer).</summary>
    public double Percent => isFit ? FitScale / DipPerMm * 100 : percent;

    public string PercentText => string.Format(CultureInfo.CurrentCulture, "%{0:0}", Percent);

    /// <summary>Sayfanın ekrandaki genişliği (DIP).</summary>
    public double DisplayWidth => pageWidth * Scale;

    /// <summary>Sayfanın ekrandaki yüksekliği (DIP).</summary>
    public double DisplayHeight => pageHeight * Scale;

    private double Scale => isFit ? FitScale : percent / 100 * DipPerMm;

    private double FitScale
    {
        get
        {
            if (!HasPage || viewportWidth <= 0 || viewportHeight <= 0)
            {
                return DipPerMm * 0.5;
            }

            var scale = Math.Min((viewportWidth - (2 * FitPadding)) / pageWidth, (viewportHeight - (2 * FitPadding)) / pageHeight);
            return Math.Clamp(scale, DipPerMm * MinPercent / 100, DipPerMm * MaxPercent / 100);
        }
    }

    /// <summary>Gösterilen sayfanın tuval ölçüsü (mm, taşma payı dahil).</summary>
    public void SetPage(double widthMm, double heightMm)
    {
        pageWidth = Math.Max(0, widthMm);
        pageHeight = Math.Max(0, heightMm);
        Notify();
    }

    /// <summary>Tuvalin görünür alanı (DIP).</summary>
    public void SetViewport(double width, double height)
    {
        viewportWidth = Math.Max(0, width);
        viewportHeight = Math.Max(0, height);
        if (isFit)
        {
            Notify();
        }
    }

    public void Fit()
    {
        isFit = true;
        Notify();
    }

    public void SetPercent(double value)
    {
        percent = Math.Clamp(value, MinPercent, MaxPercent);
        isFit = false;
        Notify();
    }

    /// <summary>Ctrl + tekerlek: kesintisiz çarpanla yakınlaştırır.</summary>
    public void ZoomBy(double factor)
    {
        if (HasPage && factor > 0)
        {
            SetPercent(Percent * factor);
        }
    }

    /// <summary>Bir üst sabit düzeye çıkar (ör. %52 → %67).</summary>
    public void ZoomIn()
    {
        var current = Percent;
        SetPercent(Levels.FirstOrDefault(l => l > current + 0.5, MaxPercent));
    }

    /// <summary>Bir alt sabit düzeye iner (ör. %52 → %50).</summary>
    public void ZoomOut()
    {
        var current = Percent;
        SetPercent(Levels.LastOrDefault(l => l < current - 0.5, MinPercent));
    }

    private void Notify()
    {
        OnPropertyChanged(nameof(HasPage));
        OnPropertyChanged(nameof(IsFit));
        OnPropertyChanged(nameof(Percent));
        OnPropertyChanged(nameof(PercentText));
        OnPropertyChanged(nameof(DisplayWidth));
        OnPropertyChanged(nameof(DisplayHeight));
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
        FitCommand.NotifyCanExecuteChanged();
        ActualSizeCommand.NotifyCanExecuteChanged();
    }
}
