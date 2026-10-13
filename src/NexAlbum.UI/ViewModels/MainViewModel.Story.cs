using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>Hikâye panelindeki bölüm satırı.</summary>
public sealed class ChapterItemViewModel(MainViewModel owner, StoryChapter chapter, IReadOnlyList<PhotoAsset> photos, IReadOnlyList<AssetItemViewModel> preview, int index, Brush color) : ObservableObject
{
    private bool isSelected;

    public StoryChapter Chapter { get; } = chapter;

    public int Number { get; } = index + 1;

    /// <summary>Bölüm adı; değiştirince projeye yazılır (geri alınabilir).</summary>
    public string Name
    {
        get => Chapter.Name;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && !string.Equals(value.Trim(), Chapter.Name, StringComparison.Ordinal))
            {
                owner.RenameChapter(Chapter.Id, value);
            }
        }
    }

    public Brush Color { get; } = color;

    public int PhotoCount { get; } = photos.Count;

    /// <summary>"Cmt 13 Haz · 09:30–09:57"</summary>
    public string TimeRange { get; } = photos.Count == 0
        ? "Fotoğraf yok"
        : string.Format(
            CultureInfo.GetCultureInfo("tr-TR"),
            "{0:ddd d MMM} · {0:HH:mm}–{1:HH:mm}",
            photos[0].CapturedAt!.Value.DateTime,
            photos[^1].CapturedAt!.Value.DateTime);

    public string Summary => string.Format(CultureInfo.CurrentCulture, "{0} · {1} fotoğraf", TimeRange, PhotoCount);

    /// <summary>İlk fotoğraflar (küçük resim şeridi; galeri kareleriyle aynı önbellekli küçük resimler).</summary>
    public IReadOnlyList<AssetItemViewModel> Preview { get; } = preview;

    public bool CanMerge => owner.Chapters.Count > 1;

    /// <summary>Hazır bölüm adları (menüden tek tıkla).</summary>
    public IReadOnlyList<ChapterNameSuggestion> Suggestions { get; } =
        [.. StoryDetector.PhaseNames.Select(n => new ChapterNameSuggestion(n, new RelayCommand(() => owner.RenameChapter(chapter.Id, n))))];

    /// <summary>Seçili bölüm galeride süzülür.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}

/// <summary>Hazır bölüm adı ve uygulama komutu.</summary>
public sealed record ChapterNameSuggestion(string Name, IRelayCommand ApplyCommand);

/// <summary>Zaman çizelgesi çubuğu: 15 dakikalık dilimdeki fotoğraf sayısı, bölüm renginde.</summary>
public sealed record TimelineBin(double Height, Brush Color, string ToolTip);

/// <summary>
/// Hikâye akışı (Karar 10 Rev. 2): düğünün bölümlerini bulma, adlandırma, birleştirme/bölme ve albümü bölümlere göre
/// oluşturma. Bölümler projeye yazılır; tüm değişiklikler geri alınabilir.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Sağ paneldeki «Hikâye» sekmesinin sırası.</summary>
    public const int StoryTabIndex = 4;

    private static readonly Brush[] ChapterPalette =
    [
        Frozen("#C9A85E"), Frozen("#8FA88A"), Frozen("#C98E7A"), Frozen("#7E95B5"),
        Frozen("#B48EAE"), Frozen("#D1B07A"), Frozen("#6FA3A0"), Frozen("#A8896C"),
    ];

    private double storyGapMinutes = 40;
    private ChapterItemViewModel? selectedChapter;

    public ObservableCollection<ChapterItemViewModel> Chapters { get; } = [];

    public ObservableCollection<TimelineBin> Timeline { get; } = [];

    public IReadOnlyList<string> ChapterNameSuggestions => StoryDetector.PhaseNames;

    public RelayCommand DetectStoryCommand { get; private set; } = null!;

    public RelayCommand ClearStoryCommand { get; private set; } = null!;

    public RelayCommand<ChapterItemViewModel> MergeChapterCommand { get; private set; } = null!;

    public RelayCommand SplitStoryHereCommand { get; private set; } = null!;

    public AsyncRelayCommand BuildFromStoryCommand { get; private set; } = null!;

    public bool HasStory => project?.Story.HasChapters == true;

    public bool HasNoStory => !HasStory;

    /// <summary>Bölüm bulmada yeni bölüm için en az ara (dakika).</summary>
    public double StoryGapMinutes
    {
        get => storyGapMinutes;
        set => SetProperty(ref storyGapMinutes, Math.Round(Math.Clamp(value, 10, 240) / 5) * 5);
    }

    public bool StoryEnabled
    {
        get => project?.Story.Enabled ?? true;
        set => UpdateStory(s => s with { Enabled = value }, value ? "Hikâye akışı açık" : "Hikâye akışı kapalı");
    }

    public bool StoryOpeners
    {
        get => project?.Story.Openers ?? false;
        set => UpdateStory(s => s with { Openers = value }, value ? "Açılış sayfaları açık" : "Açılış sayfaları kapalı");
    }

    /// <summary>"6 bölüm · 2 fotoğrafın çekim zamanı yok"</summary>
    public string StorySummary
    {
        get
        {
            if (project is null || !HasStory)
            {
                return string.Empty;
            }

            var untimed = project.Photos.Count(p => p.CapturedAt is null);
            return string.Format(CultureInfo.CurrentCulture, "{0} bölüm", Chapters.Count)
                + (untimed > 0 ? string.Format(CultureInfo.CurrentCulture, " · {0} fotoğrafın çekim zamanı yok (sonda)", untimed) : string.Empty);
        }
    }

    /// <summary>Galeride süzülen bölüm (yeniden seçince süzgeç kalkar).</summary>
    public ChapterItemViewModel? SelectedChapter
    {
        get => selectedChapter;
        set
        {
            var previous = selectedChapter;
            if (SetProperty(ref selectedChapter, value))
            {
                previous?.IsSelected = false;
                value?.IsSelected = true;
                GalleryFilter = value is null ? (galleryFilter == GalleryFilter.Chapter ? GalleryFilter.All : galleryFilter) : GalleryFilter.Chapter;
                RebuildGallery();
            }
        }
    }

    /// <summary>Fotoğrafları çekim saatine göre bölümlere ayırır (var olan bölümlerin yerine geçer; geri alınabilir).</summary>
    public void DetectStory()
    {
        if (project is null)
        {
            return;
        }

        var plan = StoryDetector.Detect(project.Photos, new StoryDetectionOptions { Gap = TimeSpan.FromMinutes(storyGapMinutes) });
        if (!plan.HasChapters)
        {
            Status = "Fotoğraflarda çekim zamanı (EXIF) yok; bölümler bulunamadı. Galeriden «Buradan yeni bölüm» ile elle başlatabilirsiniz.";
            return;
        }

        UpdateStory(_ => plan with { Enabled = project.Story.Enabled, Openers = project.Story.Openers }, "Bölümleri bul");
        PanelTabIndex = StoryTabIndex;
        Status = string.Format(CultureInfo.CurrentCulture, "{0} bölüm bulundu. Adları değiştirebilir, birleştirebilir; «Hikâyeye göre oluştur» ile albümü kurabilirsiniz.", plan.Chapters.Count);
    }

    public void RenameChapter(string chapterId, string name) =>
        UpdateStory(s => s.Rename(chapterId, name.Trim()), "Bölüm adı");

    public void MergeChapter(ChapterItemViewModel? chapter)
    {
        if (chapter is not null)
        {
            UpdateStory(s => s.MergeWithPrevious(chapter.Chapter.Id), "Bölümleri birleştir");
        }
    }

    /// <summary>Galeride seçili fotoğrafın çekim anında yeni bölüm başlatır (adı saate göre önerilir).</summary>
    public void SplitStoryHere()
    {
        if (project is null || SelectedAsset?.Asset.CapturedAt is not { } at)
        {
            Status = "Yeni bölüm için çekim zamanı olan bir fotoğraf seçin.";
            return;
        }

        var mainDay = project.Photos.Where(p => p.CapturedAt is not null).GroupBy(p => p.CapturedAt!.Value.DateTime.Date)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
        UpdateStory(s => s.SplitAt(at, StoryDetector.Suggest(at, mainDay)), "Yeni bölüm");
        PanelTabIndex = StoryTabIndex;
        Status = "Bu fotoğraftan itibaren yeni bölüm başladı. Adını Hikâye sekmesinden değiştirebilirsiniz.";
    }

    public void ClearStory() => UpdateStory(_ => StoryPlan.Empty with { Openers = project?.Story.Openers ?? false }, "Bölümleri kaldır");

    /// <summary>Galeride bölüm süzgeci: fotoğraf seçili bölüme mi ait?</summary>
    private bool InSelectedChapter(AssetItemViewModel item) =>
        selectedChapter is not null && project is not null && project.Story.ChapterOf(item.Asset)?.Id == selectedChapter.Chapter.Id;

    private async Task BuildFromStoryAsync()
    {
        if (project is null)
        {
            return;
        }

        if (!project.Story.Enabled)
        {
            UpdateStory(s => s with { Enabled = true }, null);
        }

        await AutoLayoutAsync(newSeed: false).ConfigureAwait(true);
    }

    private void UpdateStory(Func<StoryPlan, StoryPlan> change, string? label)
    {
        if (project is null)
        {
            return;
        }

        var changed = change(project.Story);
        if (changed == project.Story)
        {
            return;
        }

        UpdateProject((project with { Story = changed }).Touch(clock.GetUtcNow()), label ?? "Hikâye", record: label is not null);
    }

    /// <summary>Bölüm listesi, zaman çizelgesi ve şeritteki bölüm etiketleri projeden yeniden kurulur.</summary>
    private void RefreshStory(AlbumProject value)
    {
        var keep = selectedChapter?.Chapter.Id;
        selectedChapter = null;
        Chapters.Clear();
        var groups = value.Story.Distribute(value.Photos);
        var colors = new Dictionary<string, Brush>(StringComparer.Ordinal);
        var items = Assets.ToDictionary(a => a.Asset.Id, StringComparer.Ordinal);
        var index = 0;
        foreach (var group in groups.Where(g => g.Chapter is not null))
        {
            var color = ChapterPalette[index % ChapterPalette.Length];
            colors[group.Chapter!.Id] = color;
            var preview = group.Photos.Take(5).Select(p => items.GetValueOrDefault(p.Id)).OfType<AssetItemViewModel>().ToList();
            Chapters.Add(new ChapterItemViewModel(this, group.Chapter, group.Photos, preview, index++, color));
        }

        // Fotoğrafı olmayan bölümler de listede kalsın (ör. elle açılmış, henüz boş).
        foreach (var empty in value.Story.Chapters.Where(c => !colors.ContainsKey(c.Id)))
        {
            var color = ChapterPalette[index % ChapterPalette.Length];
            colors[empty.Id] = color;
            Chapters.Add(new ChapterItemViewModel(this, empty, [], [], index++, color));
        }

        if (keep is not null && Chapters.FirstOrDefault(c => c.Chapter.Id == keep) is { } again)
        {
            selectedChapter = again;
            again.IsSelected = true;
        }
        else if (galleryFilter == GalleryFilter.Chapter)
        {
            galleryFilter = GalleryFilter.All;
            RebuildGallery();
        }

        foreach (var asset in Assets)
        {
            asset.ChapterColor = value.Story.ChapterOf(asset.Asset) is { } c && colors.TryGetValue(c.Id, out var brush) ? brush : null;
        }

        RefreshTimeline(value, colors);
        RefreshChapterLabels(value, colors);
        OnPropertyChanged(nameof(SelectedChapter));
        OnPropertyChanged(nameof(HasStory));
        OnPropertyChanged(nameof(HasNoStory));
        OnPropertyChanged(nameof(StoryEnabled));
        OnPropertyChanged(nameof(StoryOpeners));
        OnPropertyChanged(nameof(StorySummary));
    }

    private void RefreshTimeline(AlbumProject value, Dictionary<string, Brush> colors)
    {
        Timeline.Clear();
        var timed = value.Photos.Where(p => p.CapturedAt is not null).OrderBy(p => p.CapturedAt).ToList();
        if (timed.Count == 0)
        {
            return;
        }

        // Çekim yoğunluğu: 15 dakikalık dilimler; uzun boşluklar tek ince aralık olarak kısaltılır (gece gibi).
        var slice = TimeSpan.FromMinutes(15);
        var bins = new List<(DateTimeOffset Start, int Count, Brush Color)>();
        foreach (var photo in timed)
        {
            var at = photo.CapturedAt!.Value;
            var color = value.Story.ChapterOf(photo) is { } c && colors.TryGetValue(c.Id, out var b) ? b : ChapterPalette[0];
            if (bins.Count > 0 && at - bins[^1].Start < slice)
            {
                bins[^1] = (bins[^1].Start, bins[^1].Count + 1, bins[^1].Color);
                continue;
            }

            if (bins.Count > 0 && at - bins[^1].Start >= TimeSpan.FromMinutes(60))
            {
                bins.Add((at, 0, Brushes.Transparent));
            }

            bins.Add((at, 1, color));
        }

        var max = bins.Max(b => b.Count);
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        foreach (var (start, count, color) in bins)
        {
            Timeline.Add(new TimelineBin(
                count == 0 ? 0 : 6 + (34.0 * count / max),
                color,
                count == 0 ? "ara" : string.Format(tr, "{0:HH:mm} · {1} fotoğraf", start.DateTime, count)));
        }
    }

    /// <summary>Şeritte bölümün ilk sayfasının üstüne bölüm adı (sayfanın çoğunluk fotoğrafına göre; açılış sayfası sonrakine göre).</summary>
    private void RefreshChapterLabels(AlbumProject value, Dictionary<string, Brush> colors)
    {
        var assets = value.Assets.ToDictionary(a => a.Id, StringComparer.Ordinal);
        string? previous = null;
        foreach (var page in Pages)
        {
            var chapter = !value.Story.HasChapters
                ? null
                : page.Page.Slots.Select(s => s.Placement?.AssetId).OfType<string>()
                    .Select(id => assets.TryGetValue(id, out var a) ? value.Story.ChapterOf(a) : null)
                    .OfType<StoryChapter>()
                    .GroupBy(c => c.Id).OrderByDescending(g => g.Count()).Select(g => g.First()).FirstOrDefault();
            page.ChapterLabel = chapter is not null && chapter.Id != previous ? chapter.Name : null;
            page.ChapterColor = chapter is not null && colors.TryGetValue(chapter.Id, out var color) ? color : null;
            previous = chapter?.Id ?? previous;
        }
    }

    private void InitializeStory()
    {
        DetectStoryCommand = new RelayCommand(DetectStory, () => HasProject && !IsBusy && HasPhotos);
        ClearStoryCommand = new RelayCommand(ClearStory, () => HasStory && !IsBusy);
        MergeChapterCommand = new RelayCommand<ChapterItemViewModel>(MergeChapter, c => c is not null && Chapters.Count > 1 && !IsBusy);
        SplitStoryHereCommand = new RelayCommand(SplitStoryHere, () => SelectedAsset?.Asset.CapturedAt is not null && !IsBusy);
        BuildFromStoryCommand = new AsyncRelayCommand(BuildFromStoryAsync, () => HasStory && HasPhotos && !IsBusy);
    }

    private void RefreshStoryCommands()
    {
        DetectStoryCommand?.NotifyCanExecuteChanged();
        ClearStoryCommand?.NotifyCanExecuteChanged();
        MergeChapterCommand?.NotifyCanExecuteChanged();
        SplitStoryHereCommand?.NotifyCanExecuteChanged();
        BuildFromStoryCommand?.NotifyCanExecuteChanged();
    }

    private static SolidColorBrush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
