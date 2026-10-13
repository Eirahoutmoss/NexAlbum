using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>Metin etkisi seçeneği; önizleme seçili metnin yazı tipi ve rengiyle "Aa" çizer.</summary>
public sealed class TextEffectOption(TextEffectKind kind, string name, string hint) : ObservableObject
{
    private TextElement sample = Sample(kind, null);

    public TextEffectKind Kind { get; } = kind;

    public string Name { get; } = name;

    public string Hint { get; } = hint;

    public TextElement Preview
    {
        get => sample;
        private set => SetProperty(ref sample, value);
    }

    internal void Refresh(TextElement? text) => Preview = Sample(Kind, text);

    private static TextElement Sample(TextEffectKind kind, TextElement? text)
    {
        var color = text?.Font.Color ?? TextPresets.Ink;
        return new TextElement
        {
            Id = "onizleme",
            Bounds = new RectMm(0, 0, 30, 14),
            Content = "Aa",
            Font = new FontSpec { Family = text?.Font.Family ?? "Playfair Display", Size = 26, Weight = text?.Font.Weight ?? 400, Italic = text?.Font.Italic ?? false, Color = color },
            Alignment = Core.Projects.TextAlignment.Center,
            VerticalAlignment = Core.Projects.VerticalAlignment.Middle,
            LineHeight = 1.1,
            Effect = kind == TextEffectKind.None
                ? TextEffect.None
                : new TextEffect { Kind = kind, Color = TextEffect.DefaultColor(kind, color), Strength = 0.8 },
        };
    }
}

/// <summary>Metin etkileri ve harf aralığı (Karar 17 §11): fotoğraf üstündeki yazı her zeminde okunsun.</summary>
public sealed partial class MainViewModel
{
    public IReadOnlyList<TextEffectOption> TextEffectOptions { get; } =
    [
        new(TextEffectKind.None, "Yok", "Etkisiz, düz yazı."),
        new(TextEffectKind.Shadow, "Gölge", "Sağ alta yumuşak gölge: açık yazı açık zeminde ayrışır."),
        new(TextEffectKind.Glow, "Hale", "Harflerin çevresinde yumuşak ışık: koyu yazı karışık zeminde okunur."),
        new(TextEffectKind.Outline, "Kontur", "İnce, keskin çerçeve çizgisi: her zeminde net."),
        new(TextEffectKind.Band, "Bant", "Yazının arkasında yarı saydam zemin: en yoğun fotoğrafta bile okunur."),
        new(TextEffectKind.Emboss, "Kabartma", "Işık ve gölgeyle kâğıttan kabarık görünüm (baskıda gerçek kabartma değildir)."),
    ];

    public IReadOnlyList<TextColorOption> TextEffectColors { get; } =
    [
        Swatch("Siyah", "#000000"),
        Swatch("Mürekkep", "#2B2620"),
        Swatch("Beyaz", "#FFFFFF"),
        Swatch("Krem", "#F3EAD8"),
        Swatch("Altın", "#A9843F"),
    ];

    public RelayCommand MakeTextReadableCommand { get; private set; } = null!;

    public RelayCommand<TextColorOption> ApplyTextEffectColorCommand { get; private set; } = null!;

    public TextEffectOption? SelectedTextEffect
    {
        get => SelectedText is { } t ? TextEffectOptions.First(o => o.Kind == (t.Text.Effect.IsNone ? TextEffectKind.None : t.Text.Effect.Kind)) : null;
        set
        {
            if (value is null || SelectedText is not { } t || value == SelectedTextEffect)
            {
                return;
            }

            var current = t.Text.Effect;
            UpdateText(x => x with
            {
                Effect = value.Kind == TextEffectKind.None
                    ? TextEffect.None
                    : new TextEffect
                    {
                        Kind = value.Kind,
                        Color = TextEffect.DefaultColor(value.Kind, x.Font.Color),
                        Strength = current.IsNone ? 0.6 : current.Strength,
                    },
            });
        }
    }

    public bool HasTextEffect => SelectedText?.Text.Effect.IsNone == false;

    public string TextEffectHint => SelectedTextEffect?.Hint ?? string.Empty;

    /// <summary>Etkinin gücü (%0–100).</summary>
    public double TextEffectStrength
    {
        get => Math.Round((SelectedText?.Text.Effect.Strength ?? 0.6) * 100);
        set
        {
            var strength = Math.Round(Math.Clamp(value, 5, 100)) / 100;
            if (SelectedText is { } t && HasTextEffect && Math.Abs(t.Text.Effect.Strength - strength) > 0.001)
            {
                UpdateText(x => x with { Effect = x.Effect with { Strength = strength } });
            }
        }
    }

    public string TextEffectColorHex
    {
        get => HasTextEffect ? SelectedText!.Text.Effect.Color.ToHex() : string.Empty;
        set
        {
            if (HasTextEffect && RgbaColor.TryParse(value?.Trim(), out var color))
            {
                UpdateText(x => x with { Effect = x.Effect with { Color = color } });
            }
        }
    }

    /// <summary>Harf aralığı (puntonun yüzdesi, −5 … +50).</summary>
    public double TextLetterSpacing
    {
        get => Math.Round((SelectedText?.Text.LetterSpacing ?? 0) * 100);
        set
        {
            var spacing = Math.Round(Math.Clamp(value, -5, 50)) / 100;
            if (SelectedText is { } t && Math.Abs(t.Text.LetterSpacing - spacing) > 0.0001)
            {
                UpdateText(x => FitToContent(x with { LetterSpacing = spacing }));
            }
        }
    }

    /// <summary>Yazı rengine göre en az müdahaleli okunurluk etkisini uygular (koyu yazıya hale, açık yazıya gölge).</summary>
    public void MakeTextReadable()
    {
        if (SelectedText is { } t)
        {
            UpdateText(x => x with { Effect = TextEffect.Readable(x.Font.Color) });
            Status = (TextEffect.IsDark(t.Text.Font.Color) ? "Koyu yazıya beyaz hale" : "Açık yazıya koyu gölge") + " eklendi; güç ve rengi Metin sekmesinden ayarlayabilirsiniz.";
        }
    }

    private void InitializeTextEffects()
    {
        MakeTextReadableCommand = new RelayCommand(MakeTextReadable, () => SelectedText is not null && SelectedPage is { IsLocked: false });
        ApplyTextEffectColorCommand = new RelayCommand<TextColorOption>(c => TextEffectColorHex = c?.Hex ?? TextEffectColorHex, c => c is not null && HasTextEffect);
    }

    private void RaiseTextEffects()
    {
        foreach (var option in TextEffectOptions)
        {
            option.Refresh(SelectedText?.Text);
        }

        OnPropertyChanged(nameof(SelectedTextEffect));
        OnPropertyChanged(nameof(HasTextEffect));
        OnPropertyChanged(nameof(TextEffectHint));
        OnPropertyChanged(nameof(TextEffectStrength));
        OnPropertyChanged(nameof(TextEffectColorHex));
        OnPropertyChanged(nameof(TextLetterSpacing));
    }
}
