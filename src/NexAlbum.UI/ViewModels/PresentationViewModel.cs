using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>Sunumdaki bir görünüm: çift sayfa (sol + sağ) ya da tek sayfa.</summary>
public sealed record PresentationSpread(PageItemViewModel Left, PageItemViewModel? Right)
{
    public int FirstNumber => Left.Number;

    public int LastNumber => Right?.Number ?? Left.Number;
}

/// <summary>
/// Sunum modu (F5, Karar 17 §10): albüm seçimi çoğu zaman stüdyoda, çiftle aynı ekrana bakılarak yapılır. Arayüz tamamen
/// çekilir; yalnızca sayfalar kalır. Sayfalar düzenleyiciden bağımsız görünüm modelleriyle çizilir; yalnızca görünen
/// sayfaların yüksek çözünürlüklü önizlemesi yüklenir.
/// </summary>
public sealed class PresentationViewModel : ObservableObject
{
    private readonly List<PageItemViewModel> pages;
    private readonly bool spreads;
    private readonly List<PresentationSpread> views;
    private int index;

    /// <param name="project">Gösterilecek proje (en az bir sayfa).</param>
    /// <param name="startPageIndex">Başlangıç sayfası; içinde bulunduğu çift sayfadan başlanır.</param>
    /// <param name="spreads">Karşılıklı iki sayfa (true) ya da tek tek sayfa.</param>
    public PresentationViewModel(AlbumProject project, int startPageIndex, bool spreads)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.Pages.Count == 0)
        {
            throw new ArgumentException("Sunum için en az bir sayfa gerekir.", nameof(project));
        }

        Title = project.Name;
        this.spreads = spreads;
        pages = [.. project.Pages.Select((p, i) => new PageItemViewModel(p, i, project) { ShowGuides = false })];
        views = spreads
            ? [.. Enumerable.Range(0, (pages.Count + 1) / 2).Select(i => new PresentationSpread(pages[2 * i], pages.ElementAtOrDefault((2 * i) + 1)))]
            : [.. pages.Select(p => new PresentationSpread(p, null))];
        // Sunumda sayfa baskıdaki gibi görünür: taşma payı kırpılır, yalnızca bitmiş ölçü gösterilir.
        PageWidth = pages[0].PageArea.Width;
        PageHeight = pages[0].PageArea.Height;
        TrimOffset = new System.Windows.Thickness(-pages[0].PageArea.X, -pages[0].PageArea.Y, 0, 0);
        CanvasWidth = pages[0].CanvasWidth;
        CanvasHeight = pages[0].CanvasHeight;
        var start = Math.Clamp(startPageIndex, 0, pages.Count - 1);
        index = spreads ? start / 2 : start;
        UpdateDetail(-1, index);
    }

    public string Title { get; }

    /// <summary>Bir sayfanın bitmiş ölçüsü (mm, taşma payı hariç).</summary>
    public double PageWidth { get; }

    public double PageHeight { get; }

    /// <summary>Taşma payı dahil tuval ölçüsü (mm); kırpma kutusunun içinde <see cref="TrimOffset"/> kadar kaydırılır.</summary>
    public double CanvasWidth { get; }

    public double CanvasHeight { get; }

    /// <summary>Tuvalin bitmiş sayfaya hizalanması için negatif kenar boşluğu (mm).</summary>
    public System.Windows.Thickness TrimOffset { get; }

    /// <summary>Görünümün toplam genişliği (mm): çift sayfada iki sayfa.</summary>
    public double ViewWidth => spreads ? PageWidth * 2 : PageWidth;

    public bool IsSpreads => spreads;

    public int Count => views.Count;

    public int Index => index;

    public PresentationSpread Current => views[index];

    public bool CanGoNext => index < views.Count - 1;

    public bool CanGoPrevious => index > 0;

    /// <summary>"3–4 / 20" ya da "5 / 20".</summary>
    public string CounterText => Current.Right is null
        ? string.Format(CultureInfo.CurrentCulture, "{0} / {1}", Current.FirstNumber, pages.Count)
        : string.Format(CultureInfo.CurrentCulture, "{0}–{1} / {2}", Current.FirstNumber, Current.LastNumber, pages.Count);

    public bool Next() => GoTo(index + 1);

    public bool Previous() => GoTo(index - 1);

    public bool First() => GoTo(0);

    public bool Last() => GoTo(views.Count - 1);

    /// <summary>Verilen görünüme gider; değiştiyse true.</summary>
    public bool GoTo(int target)
    {
        target = Math.Clamp(target, 0, views.Count - 1);
        if (target == index)
        {
            return false;
        }

        var previous = index;
        index = target;
        UpdateDetail(previous, target);
        OnPropertyChanged(nameof(Index));
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(CounterText));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        return true;
    }

    /// <summary>Sunum kapanırken yüklenen önizlemeler bırakılır.</summary>
    public void Close()
    {
        foreach (var page in pages)
        {
            page.SetDetail(false);
        }
    }

    private void UpdateDetail(int previous, int current)
    {
        if (previous >= 0)
        {
            views[previous].Left.SetDetail(false);
            views[previous].Right?.SetDetail(false);
        }

        views[current].Left.SetDetail(true);
        views[current].Right?.SetDetail(true);
    }
}
