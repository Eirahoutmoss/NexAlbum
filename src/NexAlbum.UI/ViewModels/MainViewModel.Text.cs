using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Common;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>Metin paneli için yazı tipi seçeneği (adı kendi yazı tipiyle gösterilir).</summary>
public sealed record FontOption(string Family, string Kind, FontFamily Preview);

/// <summary>Hazır metin stili düğmesi: örnek yazı kendi yazı tipi, eğikliği ve rengiyle.</summary>
public sealed record TextPresetOption(TextPreset Preset, string Name, string Sample, FontFamily Family, FontStyle Style, Brush Foreground)
{
    public static TextPresetOption From(TextPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        var c = preset.Font.Color;
        var brush = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        brush.Freeze();
        return new(preset, preset.Name, preset.SampleText, AlbumFonts.ToWpf(preset.Font.Family),
            preset.Font.Italic ? FontStyles.Italic : FontStyles.Normal, brush);
    }
}

/// <summary>Metin rengi örneği.</summary>
public sealed record TextColorOption(string Name, string Hex, Brush Preview);

/// <summary>
/// Sayfadaki yazılar: hazır stille ekle (başlık, isimler, tarih, alıntı…), tuvalde seç ve sürükle, sağ paneldeki «Metin»
/// sekmesinden yazı, yazı tipi, boyut, renk ve hizalamayı değiştir. Kutu yazıya göre kendiliğinden uzar. Her değişiklik
/// geri alınabilir; hızlı yazım tek adımda birleşir.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Sağ paneldeki «Metin» sekmesinin sırası.</summary>
    public const int TextTabIndex = 3;

    private TextItemViewModel? selectedText;
    private string? selectedTextId;
    private string? selectedTextPageId;
    private int panelTabIndex;

    /// <summary>Ctrl+T ile eklenen stil (serbest metin).</summary>
    public static TextPreset DefaultTextPreset { get; } = TextPresets.Find("body")!;

    /// <summary>Hazır stiller, kendi yazı tipiyle önizlemeli.</summary>
    public IReadOnlyList<TextPresetOption> TextPresetList { get; } = [.. TextPresets.All.Select(TextPresetOption.From)];

    public IReadOnlyList<FontOption> FontOptions { get; } =
        [.. AlbumFonts.All.Select(f => new FontOption(f.Family, f.Kind, f.WpfFamily))];

    public IReadOnlyList<TextColorOption> TextColors { get; } =
    [
        Swatch("Mürekkep", "#2B2620"),
        Swatch("Siyah", "#000000"),
        Swatch("Antrasit", "#3E3A35"),
        Swatch("Altın", "#A9843F"),
        Swatch("Bakır", "#9A5B37"),
        Swatch("Bordo", "#6B2433"),
        Swatch("Lacivert", "#1F2A44"),
        Swatch("Adaçayı", "#5E6E5A"),
        Swatch("Krem", "#F3EAD8"),
        Swatch("Beyaz", "#FFFFFF"),
    ];

    public RelayCommand<TextPreset> AddTextCommand { get; private set; } = null!;

    public RelayCommand DeleteTextCommand { get; private set; } = null!;

    public RelayCommand<TextColorOption> ApplyTextColorCommand { get; private set; } = null!;

    /// <summary>Sağ panelin etkin sekmesi (metin seçilince «Metin» sekmesi açılır).</summary>
    public int PanelTabIndex
    {
        get => panelTabIndex;
        set => SetProperty(ref panelTabIndex, value);
    }

    public TextItemViewModel? SelectedText
    {
        get => selectedText;
        private set
        {
            var previous = selectedText;
            if (SetProperty(ref selectedText, value))
            {
                previous?.IsSelected = false;
                value?.IsSelected = true;
                RaiseTextEditor();
            }
        }
    }

    public bool HasSelectedText => SelectedText is not null;

    /// <summary>Metnin kendisi (çok satırlı; Enter yeni satır).</summary>
    public string TextContent
    {
        get => SelectedText?.Text.Content ?? string.Empty;
        set
        {
            if (SelectedText is { } text && !string.Equals(text.Text.Content, value, StringComparison.Ordinal))
            {
                UpdateText(t => FitToContent(t with { Content = value ?? string.Empty }));
            }
        }
    }

    public FontOption? TextFont
    {
        get => SelectedText is { } t ? FontOptions.FirstOrDefault(f => string.Equals(f.Family, t.Text.Font.Family, StringComparison.OrdinalIgnoreCase)) : null;
        set
        {
            if (value is not null && SelectedText is { } t && !string.Equals(t.Text.Font.Family, value.Family, StringComparison.Ordinal))
            {
                UpdateText(x => FitToContent(x with { Font = x.Font with { Family = value.Family } }));
            }
        }
    }

    /// <summary>Punto (6–144).</summary>
    public double TextSize
    {
        get => SelectedText?.Text.Font.Size ?? 12;
        set
        {
            var size = Math.Round(Math.Clamp(value, 6, 144));
            if (SelectedText is { } t && Math.Abs(t.Text.Font.Size - size) > 0.01)
            {
                UpdateText(x => FitToContent(x with { Font = x.Font with { Size = size } }));
            }
        }
    }

    public bool TextBold
    {
        get => SelectedText?.Text.Font.Weight >= 600;
        set => UpdateText(x => FitToContent(x with { Font = x.Font with { Weight = value ? 700 : 400 } }));
    }

    public bool TextItalic
    {
        get => SelectedText?.Text.Font.Italic == true;
        set => UpdateText(x => FitToContent(x with { Font = x.Font with { Italic = value } }));
    }

    /// <summary>Satır aralığı (punto katı, 0,8–2,5).</summary>
    public double TextLineHeight
    {
        get => SelectedText?.Text.LineHeight ?? TextLines.DefaultLineHeight;
        set
        {
            var lh = Math.Round(Math.Clamp(value, 0.8, 2.5), 2);
            if (SelectedText is { } t && Math.Abs((t.Text.LineHeight ?? TextLines.DefaultLineHeight) - lh) > 0.001)
            {
                UpdateText(x => FitToContent(x with { LineHeight = lh }));
            }
        }
    }

    /// <summary>Metin kutusunun genişliği (sayfa genişliğine oranı, %10–100).</summary>
    public double TextWidthPercent
    {
        get => SelectedText is { } t && SelectedPage is { } p ? Math.Round(t.Width / p.PageBounds.Width * 100) : 50;
        set
        {
            if (SelectedText is not { } t || SelectedPage is not { } page || project is null)
            {
                return;
            }

            var width = page.PageBounds.Width * Math.Clamp(value, 10, 100) / 100;
            if (Math.Abs(width - t.Width) < 0.1)
            {
                return;
            }

            var now = clock.GetUtcNow();
            KeepTextSelected(t.Id);
            EditSelectedPage(
                p =>
                {
                    var resized = PageTextEditor.Resize(p, t.Id, width, page.CanvasBounds, now);
                    return ReferenceEquals(resized, p) ? p : PageTextEditor.Update(resized, t.Id, FitToContent, now);
                },
                "Metin kutusu genişliği");
        }
    }

    public string TextColorHex
    {
        get => SelectedText?.Text.Font.Color.ToHex() ?? string.Empty;
        set
        {
            if (RgbaColor.TryParse(value?.Trim(), out var color))
            {
                UpdateText(x => x with { Font = x.Font with { Color = color } });
            }
        }
    }

    public bool IsTextLeft
    {
        get => SelectedText?.Text.Alignment == Core.Projects.TextAlignment.Left;
        set => SetAlignment(value, Core.Projects.TextAlignment.Left);
    }

    public bool IsTextCenter
    {
        get => SelectedText?.Text.Alignment == Core.Projects.TextAlignment.Center;
        set => SetAlignment(value, Core.Projects.TextAlignment.Center);
    }

    public bool IsTextRight
    {
        get => SelectedText?.Text.Alignment == Core.Projects.TextAlignment.Right;
        set => SetAlignment(value, Core.Projects.TextAlignment.Right);
    }

    /// <summary>Hazır stille seçili sayfaya metin ekler ve seçer; «Metin» sekmesi açılır.</summary>
    public void AddText(TextPreset? preset)
    {
        if (preset is null || project is null || SelectedPage is not { } page)
        {
            return;
        }

        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var content = preset.Id == "date"
            ? clock.GetLocalNow().ToString("d MMMM yyyy", tr).ToUpper(tr)
            : null;
        var now = clock.GetUtcNow();
        string? id = null;
        if (EditSelectedPage(
            p =>
            {
                var (updated, text) = PageTextEditor.Add(p, preset, page.PageBounds, content, now);
                id = text.Id;
                return updated;
            },
            preset.Name + " eklendi. Yazıyı «Metin» sekmesinden değiştirin; tuvalde sürükleyerek taşıyın."))
        {
            SelectTextById(id);
            PanelTabIndex = TextTabIndex;
        }
    }

    public void DeleteText()
    {
        if (SelectedText is not { } text)
        {
            return;
        }

        var now = clock.GetUtcNow();
        selectedTextId = null;
        if (EditSelectedPage(p => PageTextEditor.Remove(p, text.Id, now), "Metin silindi. Geri almak için Ctrl+Z."))
        {
            SelectedText = null;
        }
    }

    /// <summary>Tuvalde tıklanan metni seçer (yuva seçimi kalkar); null seçimi kaldırır.</summary>
    public void SelectText(TextItemViewModel? text)
    {
        if (text is not null)
        {
            SelectSlot(null);
        }

        selectedTextId = text?.Id;
        selectedTextPageId = text is null ? null : SelectedPage?.Page.Id;
        SelectedText = text;
        if (text is not null)
        {
            PanelTabIndex = TextTabIndex;
        }
    }

    /// <summary>Sürüklerken önizleme: yalnızca ekrandaki konum değişir (mm).</summary>
    public static void PreviewMoveText(TextItemViewModel text, double startX, double startY, double deltaXMm, double deltaYMm)
    {
        ArgumentNullException.ThrowIfNull(text);
        text.X = startX + deltaXMm;
        text.Y = startY + deltaYMm;
    }

    /// <summary>Sürükleme bitince yeni konum projeye yazılır (taşma payı alanı içinde kalır).</summary>
    public void CommitTextMove(TextItemViewModel text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (SelectedPage is not { } page)
        {
            return;
        }

        // Ekran koordinatı tuvalin köşesine göredir; proje koordinatına çevrilir.
        var x = text.X + page.CanvasBounds.X;
        var y = text.Y + page.CanvasBounds.Y;
        var now = clock.GetUtcNow();
        KeepTextSelected(text.Id);
        if (!EditSelectedPage(p => PageTextEditor.Move(p, text.Id, x, y, page.CanvasBounds, now), "Metin taşındı."))
        {
            // Kilitli sayfa ya da değişiklik yok: önizleme geri alınır.
            RestoreTextSelection(SelectedPage);
        }
    }

    private static TextColorOption Swatch(string name, string hex)
    {
        var c = RgbaColor.Parse(hex);
        var brush = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        brush.Freeze();
        return new TextColorOption(name, hex, brush);
    }

    /// <summary>
    /// Kutu yüksekliğini yazının satır sayısına göre ayarlar (WPF ile ölçülür; satır kırma kuralı PDF ile aynı:
    /// <see cref="TextLines"/>).
    /// </summary>
    private static TextElement FitToContent(TextElement text)
    {
        var typeface = new Typeface(
            AlbumFonts.ToWpf(text.Font.Family),
            text.Font.Italic ? FontStyles.Italic : FontStyles.Normal,
            FontWeight.FromOpenTypeWeight(Math.Clamp(text.Font.Weight, 1, 999)),
            FontStretches.Normal);
        var size = text.Font.Size * TextPreset.MmPerPoint;
        var lines = TextLines.Wrap(text.Content, text.Bounds.Width, s =>
            new FormattedText(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, size, Brushes.Black, 1.0).WidthIncludingTrailingWhitespace);
        return PageTextEditor.FitHeight(text, lines.Count);
    }

    private void InitializeText()
    {
        AddTextCommand = new RelayCommand<TextPreset>(AddText, p => p is not null && SelectedPage is { IsLocked: false } && !IsBusy);
        DeleteTextCommand = new RelayCommand(DeleteText, () => SelectedText is not null && SelectedPage is { IsLocked: false } && !IsBusy);
        ApplyTextColorCommand = new RelayCommand<TextColorOption>(c => TextColorHex = c?.Hex ?? TextColorHex, c => c is not null && SelectedText is not null);
    }

    private void RefreshTextCommands()
    {
        AddTextCommand?.NotifyCanExecuteChanged();
        DeleteTextCommand?.NotifyCanExecuteChanged();
        ApplyTextColorCommand?.NotifyCanExecuteChanged();
    }

    private void UpdateText(Func<TextElement, TextElement> change)
    {
        if (SelectedText is not { } text)
        {
            return;
        }

        var now = clock.GetUtcNow();
        KeepTextSelected(text.Id);
        EditSelectedPage(p => PageTextEditor.Update(p, text.Id, change, now), "Metin güncellendi.");
    }

    private void SetAlignment(bool on, Core.Projects.TextAlignment alignment)
    {
        if (on)
        {
            UpdateText(x => x with { Alignment = alignment });
        }
    }

    private void KeepTextSelected(string id)
    {
        selectedTextId = id;
        selectedTextPageId = SelectedPage?.Page.Id;
    }

    private void SelectTextById(string? id)
    {
        KeepTextSelected(id ?? string.Empty);
        RestoreTextSelection(SelectedPage);
    }

    /// <summary>Sayfa görünüm modeli yenilenince (her düzenlemede) seçili metin kimliğiyle yeniden seçilir.</summary>
    private void RestoreTextSelection(PageItemViewModel? page)
    {
        var text = page is not null && selectedTextId is not null && string.Equals(page.Page.Id, selectedTextPageId, StringComparison.Ordinal)
            ? page.Texts.FirstOrDefault(t => string.Equals(t.Id, selectedTextId, StringComparison.Ordinal))
            : null;

        // Sayfa yenilenirken seçili sayfa bir an boş kalır; kimlik o arada korunur, başka sayfaya geçilince bırakılır.
        if (text is null && page is not null)
        {
            selectedTextId = null;
        }

        selectedText?.IsSelected = false;
        selectedText = null;
        SelectedText = text;
    }

    private void RaiseTextEditor()
    {
        OnPropertyChanged(nameof(HasSelectedText));
        OnPropertyChanged(nameof(TextContent));
        OnPropertyChanged(nameof(TextFont));
        OnPropertyChanged(nameof(TextSize));
        OnPropertyChanged(nameof(TextBold));
        OnPropertyChanged(nameof(TextItalic));
        OnPropertyChanged(nameof(TextLineHeight));
        OnPropertyChanged(nameof(TextWidthPercent));
        OnPropertyChanged(nameof(TextColorHex));
        OnPropertyChanged(nameof(IsTextLeft));
        OnPropertyChanged(nameof(IsTextCenter));
        OnPropertyChanged(nameof(IsTextRight));
        RefreshTextCommands();
    }
}
