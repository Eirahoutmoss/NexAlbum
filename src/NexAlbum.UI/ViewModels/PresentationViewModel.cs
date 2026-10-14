using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NexAlbum.Core.Effects;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Video;

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
    private int effectIndex;
    private bool autoplay;
    private bool browsing;

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
        effectIndex = Math.Max(0, Effects.ToList().FindIndex(e => e.Kind == TransitionKind.Aperture));
    }

    /// <summary>Sunumda seçilebilen geçiş efektleri (kesme hariç; Karar 23).</summary>
    public static IReadOnlyList<EffectInfo> Effects { get; } = [.. TransitionEffects.All.Where(e => e.Kind != TransitionKind.Cut)];

    /// <summary>Otomatik oynatmada bir görünümün süresi.</summary>
    public static TimeSpan AutoplayInterval { get; } = TimeSpan.FromSeconds(6);

    /// <summary>Sayfa geçişinde kullanılan efekt.</summary>
    public EffectInfo Effect => Effects[effectIndex];

    /// <summary>Efektin süresi (efektin tadına göre).</summary>
    public TimeSpan EffectDuration => TimeSpan.FromMilliseconds(1100 * TransitionEffects.DurationFactor(Effect.Kind));

    /// <summary>"Efekt: Aperture · E ile değiştir"</summary>
    public string EffectText => "Efekt: " + Effect.Name;

    public IReadOnlyList<PresentationSpread> Views => views;

    /// <summary>Otomatik oynatma (P): her görünüm <see cref="AutoplayInterval"/> kadar kalır; sonda durur.</summary>
    public bool Autoplay
    {
        get => autoplay;
        set
        {
            if (SetProperty(ref autoplay, value))
            {
                OnPropertyChanged(nameof(AutoplayText));
            }
        }
    }

    public string AutoplayText => autoplay ? "❚❚ Otomatik" : "▶ Otomatik (P)";

    /// <summary>«Göz at» kipi (G): odak şeridinde tüm görünümler.</summary>
    public bool Browsing
    {
        get => browsing;
        set => SetProperty(ref browsing, value);
    }

    /// <summary>"03 — 12" (Aperture sayacı).</summary>
    public string ShortCounter => string.Format(CultureInfo.InvariantCulture, "{0:00} — {1:00}", index + 1, views.Count);

    /// <summary>Sonraki (ya da önceki) efekte geçer.</summary>
    public void CycleEffect(int direction = 1)
    {
        effectIndex = (((effectIndex + direction) % Effects.Count) + Effects.Count) % Effects.Count;
        OnPropertyChanged(nameof(Effect));
        OnPropertyChanged(nameof(EffectText));
        OnPropertyChanged(nameof(EffectDuration));
    }

    /// <summary>Efekti türüne göre seçer.</summary>
    public void UseEffect(TransitionKind kind)
    {
        var found = Effects.ToList().FindIndex(e => e.Kind == kind);
        if (found >= 0 && found != effectIndex)
        {
            effectIndex = found;
            OnPropertyChanged(nameof(Effect));
            OnPropertyChanged(nameof(EffectText));
            OnPropertyChanged(nameof(EffectDuration));
        }
    }

    /// <summary>Görünümün etiketi: "Sayfa 3–4" ya da "Sayfa 5".</summary>
    public static string Label(PresentationSpread view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return view.Right is null
            ? string.Format(CultureInfo.CurrentCulture, "Sayfa {0}", view.FirstNumber)
            : string.Format(CultureInfo.CurrentCulture, "Sayfa {0}–{1}", view.FirstNumber, view.LastNumber);
    }

    /// <summary>Görünümün yüksek çözünürlüklü önizlemesini yükler (göz at şeridinde ortaya gelince).</summary>
    public void Preload(int view)
    {
        if (view >= 0 && view < views.Count)
        {
            views[view].Left.SetDetail(true);
            views[view].Right?.SetDetail(true);
        }
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
        OnPropertyChanged(nameof(ShortCounter));
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
