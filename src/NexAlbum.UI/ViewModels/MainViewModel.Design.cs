using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Assets.Import;
using NexAlbum.Core.Common;
using NexAlbum.Core.Design;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Tasarım paneli: çerçeve, fon (renk, gradyan, görsel), boşluk ve şablon değiştirme.
/// Kapsam seçili sayfa ya da tüm sayfalardır; kilitli sayfalara dokunulmaz. "Tüm sayfalar" seçiliyse
/// seçim projenin tasarım ayarına da yazılır; sonraki otomatik yerleşim aynı görünümü kullanır.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Şablon kendi boşluğunu kullanırken kaydırıcıda gösterilen başlangıç değerleri (% kısa kenar).</summary>
    public const double DefaultGapPercent = 2;

    public const double DefaultMarginPercent = 4;

    private DesignScope designScope = DesignScope.Page;
    private double backgroundOpacity = 1;
    private string customColor = "#FFFFFF";
    private double gapPercent = DefaultGapPercent;
    private double marginPercent = DefaultMarginPercent;
    private TemplateCountOption selectedTemplateCount = null!;
    private IReadOnlyList<TemplateItemViewModel> templates = [];
    private string? templateKey;
    private TemplateStyleOption selectedTemplateStyle = null!;
    private string? filterPageId;

    public IReadOnlyList<FrameItemViewModel> Frames { get; } = [.. FramePresets.All.Select(f => new FrameItemViewModel(f))];

    /// <summary>Dikdörtgen yuvayı koruyan çerçeveler (çizgi, paspartu, gölge, polaroid…).</summary>
    public IReadOnlyList<FrameItemViewModel> ClassicFrames => [.. Frames.Where(f => !f.Frame.OverridesShape)];

    /// <summary>Yuvanın biçimini değiştiren çerçeveler: kemer, altıgen, elmas, kesik köşe, üçgen, eğik, yuvarlak, oval.</summary>
    public IReadOnlyList<FrameItemViewModel> ShapedFrames => [.. Frames.Where(f => f.Frame.OverridesShape)];

    public IReadOnlyList<BackgroundItemViewModel> BackgroundColors { get; } = [.. BackgroundPresets.Colors.Select(p => new BackgroundItemViewModel(p))];

    public IReadOnlyList<BackgroundItemViewModel> BackgroundGradients { get; } = [.. BackgroundPresets.Gradients.Select(p => new BackgroundItemViewModel(p))];

    public ObservableCollection<BackgroundItemViewModel> BackgroundImages { get; } = [];

    public IReadOnlyList<TemplateCountOption> TemplateCounts { get; } =
        [new(0, "Tümü"), .. Enumerable.Range(1, 12).Select(n => new TemplateCountOption(n, string.Format(CultureInfo.CurrentCulture, "{0} fotoğraf", n)))];

    public RelayCommand<FrameItemViewModel> ApplyFrameCommand { get; private set; } = null!;

    public RelayCommand<BackgroundItemViewModel> ApplyBackgroundCommand { get; private set; } = null!;

    public RelayCommand ApplyCustomColorCommand { get; private set; } = null!;

    public AsyncRelayCommand ImportBackgroundsCommand { get; private set; } = null!;

    public RelayCommand ApplySpacingCommand { get; private set; } = null!;

    public RelayCommand ResetSpacingCommand { get; private set; } = null!;

    public RelayCommand<TemplateItemViewModel> ApplyTemplateCommand { get; private set; } = null!;

    /// <summary>Kilitsiz tüm sayfalar (ve proje varsayılanı).</summary>
    public bool ApplyToAllPages
    {
        get => designScope == DesignScope.AllPages;
        set => SetScope(value, DesignScope.AllPages);
    }

    /// <summary>Yalnızca seçili sayfa (varsayılan).</summary>
    public bool ApplyToSelectedPage
    {
        get => designScope == DesignScope.Page;
        set => SetScope(value, DesignScope.Page);
    }

    /// <summary>Yalnızca seçili yuva (çerçeve için; fon her zaman sayfanındır).</summary>
    public bool ApplyToSelectedSlot
    {
        get => designScope == DesignScope.Slot;
        set => SetScope(value, DesignScope.Slot);
    }

    public string DesignScopeHint => designScope switch
    {
        DesignScope.AllPages => "Kilitsiz tüm sayfalara uygulanır ve yeni sayfalar için varsayılan olur.",
        DesignScope.Slot => "Çerçeve yalnızca seçili yuvaya uygulanır (tuvalde bir fotoğrafa tıklayın). Fon sayfaya uygulanır.",
        _ => "Yalnızca seçili sayfaya uygulanır.",
    };

    /// <summary>Fon opaklığı (0–1); görsel fonlarda kâğıt beyazıyla karışır.</summary>
    public double BackgroundOpacity
    {
        get => backgroundOpacity;
        set => SetProperty(ref backgroundOpacity, Math.Clamp(value, 0, 1));
    }

    /// <summary>Özel renk (#RRGGBB).</summary>
    public string CustomColor
    {
        get => customColor;
        set => SetProperty(ref customColor, value ?? string.Empty);
    }

    /// <summary>Fotoğraflar arası boşluk (% kısa kenar).</summary>
    public double GapPercent
    {
        get => gapPercent;
        set => SetProperty(ref gapPercent, Math.Round(Math.Clamp(value, 0, 20), 1));
    }

    /// <summary>Sayfa kenar payı (% kısa kenar).</summary>
    public double MarginPercent
    {
        get => marginPercent;
        set => SetProperty(ref marginPercent, Math.Round(Math.Clamp(value, 0, 30), 1));
    }

    public string SpacingSummary
    {
        get
        {
            var design = project?.Settings.Design ?? DesignSettings.Default;
            return design.GapPercent is null && design.MarginPercent is null
                ? "Şu an: her şablonun kendi boşlukları."
                : string.Format(CultureInfo.CurrentCulture, "Şu an: boşluk %{0:0.#}, kenar %{1:0.#}.", design.GapPercent ?? DefaultGapPercent, design.MarginPercent ?? DefaultMarginPercent);
        }
    }

    public bool HasBackgroundImages => BackgroundImages.Count > 0;

    public TemplateCountOption SelectedTemplateCount
    {
        get => selectedTemplateCount;
        set
        {
            if (SetProperty(ref selectedTemplateCount, value ?? TemplateCounts[0]))
            {
                RefreshTemplates();
            }
        }
    }

    public IReadOnlyList<TemplateStyleOption> TemplateStyles { get; } =
    [
        new(TemplateStyle.All, "Tümü"),
        new(TemplateStyle.Geometric, "Geometrik"),
        new(TemplateStyle.Collage, "Kolaj"),
        new(TemplateStyle.Grid, "Izgara"),
        new(TemplateStyle.Simple, "Sade"),
    ];

    /// <summary>Şablon galerisinin biçim süzgeci.</summary>
    public TemplateStyleOption SelectedTemplateStyle
    {
        get => selectedTemplateStyle;
        set
        {
            if (SetProperty(ref selectedTemplateStyle, value ?? TemplateStyles[0]))
            {
                RefreshTemplates();
            }
        }
    }

    public bool HasTemplates => Templates.Count > 0;

    /// <summary>Sayfa yönüne uyan, süzgece göre şablonlar (önizlemeler projenin sayfa oranında).</summary>
    public IReadOnlyList<TemplateItemViewModel> Templates
    {
        get => templates;
        private set
        {
            if (SetProperty(ref templates, value))
            {
                OnPropertyChanged(nameof(HasTemplates));
            }
        }
    }

    public void ApplyFrame(FrameItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var now = clock.GetUtcNow();
        if (designScope == DesignScope.Slot)
        {
            if (SelectedSlot is not { } slot)
            {
                Status = "Önce tuvalde bir yuvaya tıklayın ya da kapsamı «Seçili sayfa» yapın.";
                return;
            }

            EditSelectedPage(p => PageEditor.ApplyFrame(p, slot.Id, item.Frame, now), "Çerçeve «" + item.Name + "» yuvaya uygulandı.");
            return;
        }

        ApplyDesign(
            "Çerçeve «" + item.Name + "»",
            p => PageDesigner.ApplyFrame(p, item.Frame, now),
            d => d with { FrameStyleId = item.Id });
    }

    public void ApplyBackground(BackgroundItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var fill = item.Fill with { Opacity = BackgroundOpacity };
        var now = clock.GetUtcNow();
        ApplyDesign("Fon «" + item.Name + "»", p => PageDesigner.ApplyBackground(p, fill, now), d => d with { Background = fill });
    }

    public void ApplyCustomColor()
    {
        var text = CustomColor.Trim();
        if (!text.StartsWith('#'))
        {
            text = "#" + text;
        }

        if (!RgbaColor.TryParse(text, out var color))
        {
            dialogs.Warn("Renk kodu anlaşılamadı. Örnek: #F5EFE6");
            return;
        }

        var fill = BackgroundFill.Solid(color) with { Opacity = BackgroundOpacity };
        var now = clock.GetUtcNow();
        ApplyDesign("Fon " + color.ToHex(), p => PageDesigner.ApplyBackground(p, fill, now), d => d with { Background = fill });
    }

    public async Task ImportBackgroundsAsync()
    {
        var current = project;
        var files = dialogs.AskBackgroundImages();
        if (current is null || files is not { Count: > 0 })
        {
            return;
        }

        await RunImportAsync(c => new PhotoImporter().ImportFiles(files, c, new ImportOptions { Role = AssetRole.Background }), "fon görseli").ConfigureAwait(true);
    }

    /// <summary>Boşluk ve kenar payı proje genelidir: kilitsiz tüm sayfalar kendi şablonlarıyla yeniden ölçülendirilir.</summary>
    public void ApplySpacing() => SetSpacing(GapPercent, MarginPercent);

    public void ResetSpacing()
    {
        GapPercent = DefaultGapPercent;
        MarginPercent = DefaultMarginPercent;
        SetSpacing(null, null);
    }

    public void ApplyTemplate(TemplateItemViewModel? item)
    {
        if (project is null || item is null || SelectedPage is null)
        {
            return;
        }

        if (SelectedPage.IsLocked)
        {
            Status = "Sayfa kilitli; şablon değiştirmek için önce kilidi açın.";
            return;
        }

        var number = SelectedPage.Number;
        var result = PageDesigner.ApplyTemplate(SelectedPage.Page, item.Template, project, clock.GetUtcNow());
        UpdateProject(project.ReplacePage(result.Page));
        Status = result.RemovedAssetIds.Count == 0
            ? string.Format(CultureInfo.CurrentCulture, "Sayfa {0}: «{1}» şablonu uygulandı.", number, item.Name)
            : string.Format(
                CultureInfo.CurrentCulture,
                "Sayfa {0}: «{1}» şablonu uygulandı; sığmayan {2} fotoğraf sayfadan çıkarıldı (kullanılmayanlarda).",
                number,
                item.Name,
                result.RemovedAssetIds.Count);
    }

    private void InitializeDesign()
    {
        selectedTemplateCount = TemplateCounts[0];
        selectedTemplateStyle = TemplateStyles[0];
        ApplyFrameCommand = new RelayCommand<FrameItemViewModel>(ApplyFrame, f => f is not null && CanDesign);
        ApplyBackgroundCommand = new RelayCommand<BackgroundItemViewModel>(ApplyBackground, b => b is not null && CanDesign);
        ApplyCustomColorCommand = new RelayCommand(ApplyCustomColor, () => CanDesign);
        ImportBackgroundsCommand = new AsyncRelayCommand(ImportBackgroundsAsync, () => HasProject && !IsBusy);
        ApplySpacingCommand = new RelayCommand(ApplySpacing, () => HasProject && !IsBusy);
        ResetSpacingCommand = new RelayCommand(ResetSpacing, () => HasProject && !IsBusy);
        ApplyTemplateCommand = new RelayCommand<TemplateItemViewModel>(ApplyTemplate, t => t is not null && SelectedPage is { IsLocked: false } && !IsBusy);
    }

    private bool CanDesign => HasProject && !IsBusy;

    private void RefreshDesignCommands()
    {
        ApplyFrameCommand.NotifyCanExecuteChanged();
        ApplyBackgroundCommand.NotifyCanExecuteChanged();
        ApplyCustomColorCommand.NotifyCanExecuteChanged();
        ImportBackgroundsCommand.NotifyCanExecuteChanged();
        ApplySpacingCommand.NotifyCanExecuteChanged();
        ResetSpacingCommand.NotifyCanExecuteChanged();
        ApplyTemplateCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Proje değişince: fon görselleri, boşluk değerleri ve şablon önizlemeleri.</summary>
    private void RefreshDesign(AlbumProject? value)
    {
        var backgrounds = value?.Backgrounds ?? [];
        if (!backgrounds.Select(b => b.Id).SequenceEqual(BackgroundImages.Select(b => b.Id), StringComparer.Ordinal)
            || backgrounds.Zip(BackgroundImages).Any(x => x.First.Missing != x.Second.Asset?.Missing))
        {
            BackgroundImages.Clear();
            foreach (var image in backgrounds)
            {
                BackgroundImages.Add(new BackgroundItemViewModel(image));
            }

            OnPropertyChanged(nameof(HasBackgroundImages));
        }

        var design = value?.Settings.Design ?? DesignSettings.Default;
        GapPercent = design.GapPercent ?? DefaultGapPercent;
        MarginPercent = design.MarginPercent ?? DefaultMarginPercent;
        OnPropertyChanged(nameof(SpacingSummary));
        RefreshTemplates();
    }

    /// <summary>Seçili sayfa değişince şablon süzgeci o sayfanın fotoğraf sayısına geçer.</summary>
    private void OnDesignPageChanged(PageItemViewModel? page)
    {
        if (page is null || string.Equals(page.Page.Id, filterPageId, StringComparison.Ordinal))
        {
            return;
        }

        filterPageId = page.Page.Id;
        if (page.PhotoCount is > 0 and var count && TemplateCounts.FirstOrDefault(o => o.Count == count) is { } option)
        {
            SelectedTemplateCount = option;
        }
    }

    private void RefreshTemplates()
    {
        if (project is null)
        {
            templateKey = null;
            Templates = [];
            return;
        }

        var size = project.PageSize;
        var design = project.Settings.Design;
        // Canlı önizleme: hücreler seçili sayfanın fotoğraflarıyla (okuma sırasıyla) dolar.
        var photos = SelectedPage?.Slots
            .Where(s => s.Asset is not null)
            .OrderBy(s => s.Slot.Bounds.Y).ThenBy(s => s.Slot.Bounds.X)
            .Select(s => s.Asset!)
            .ToList() ?? [];
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"{size.Width}x{size.Height}|{design.GapPercent}|{design.MarginPercent}|{SelectedTemplateCount.Count}|{SelectedTemplateStyle.Style}|{string.Join(",", photos.Select(p => p.Id))}");
        if (string.Equals(key, templateKey, StringComparison.Ordinal))
        {
            return;
        }

        templateKey = key;
        var fills = photos.Select(PreviewBrush).ToList();
        var count = SelectedTemplateCount.Count;
        var style = SelectedTemplateStyle;
        Templates = [.. BuiltInTemplates.All
            .Where(t => t.Fits(size.Orientation) && (count == 0 || t.PhotoCount.Accepts(count)) && style.Matches(t.Category))
            .OrderBy(t => t.PhotoCount.Min)
            .ThenBy(t => t.Category == TemplateCategory.Geometric ? 0 : 1)
            .ThenBy(t => t.Category)
            .Select(t => new TemplateItemViewModel(t, size, design, fills))];
    }

    /// <summary>Önizleme hücresi için fotoğrafın (önbellekteki) küçük resmi; henüz yüklenmemişse yumuşak dolgu.</summary>
    private static Brush PreviewBrush(PhotoAsset asset)
    {
        if (ImageLoader.Shared.TryGetCached(asset, ImageLoader.ThumbnailWidth) is not { } image)
        {
            return TemplateItemViewModel.EmptyCellFill;
        }

        var brush = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
        brush.Freeze();
        return brush;
    }

    private void ApplyDesign(string what, Func<AlbumPage, AlbumPage> change, Func<DesignSettings, DesignSettings> design)
    {
        if (project is null)
        {
            return;
        }

        if (ApplyToAllPages || SelectedPage is null)
        {
            var withDesign = PageDesigner.WithDesign(project, design(project.Settings.Design));
            var (updated, changed) = PageDesigner.ApplyToUnlocked(withDesign, change);
            UpdateProject(updated);
            var locked = updated.Pages.Count - changed;
            Status = string.Format(
                CultureInfo.CurrentCulture,
                "{0}: {1} sayfaya uygulandı{2}; yeni sayfalar için varsayılan yapıldı.",
                what,
                changed,
                locked > 0 ? string.Format(CultureInfo.CurrentCulture, " ({0} kilitli sayfa atlandı)", locked) : string.Empty);
            return;
        }

        if (SelectedPage.IsLocked)
        {
            Status = "Sayfa kilitli; değiştirmek için önce kilidi açın.";
            return;
        }

        var number = SelectedPage.Number;
        UpdateProject(project.ReplacePage(change(SelectedPage.Page)));
        Status = string.Format(CultureInfo.CurrentCulture, "{0}: sayfa {1} güncellendi.", what, number);
    }

    private void SetScope(bool selected, DesignScope scope)
    {
        if (!selected || designScope == scope)
        {
            return;
        }

        designScope = scope;
        OnPropertyChanged(nameof(ApplyToAllPages));
        OnPropertyChanged(nameof(ApplyToSelectedPage));
        OnPropertyChanged(nameof(ApplyToSelectedSlot));
        OnPropertyChanged(nameof(DesignScopeHint));
    }

    private void SetSpacing(double? gap, double? margin)
    {
        if (project is null)
        {
            return;
        }

        var now = clock.GetUtcNow();
        var withDesign = PageDesigner.WithDesign(project, project.Settings.Design with { GapPercent = gap, MarginPercent = margin });
        var (updated, changed) = PageDesigner.ApplyToUnlocked(withDesign, p => PageDesigner.Relayout(p, withDesign, BuiltInTemplates.All, now));
        UpdateProject(updated);
        Status = gap is null
            ? string.Format(CultureInfo.CurrentCulture, "Şablon boşluklarına dönüldü; {0} sayfa yeniden ölçülendirildi.", changed)
            : string.Format(CultureInfo.CurrentCulture, "Boşluk %{0:0.#}, kenar %{1:0.#}; {2} sayfa yeniden ölçülendirildi.", gap, margin, changed);
    }
}

/// <summary>Tasarım işlemlerinin kapsamı.</summary>
public enum DesignScope
{
    Slot,
    Page,
    AllPages,
}
