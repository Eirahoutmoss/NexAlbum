using System.Text.Json;

namespace NexAlbum.Persistence.Json;

/// <summary>
/// Enum değerlerinin dosyadaki metin karşılıkları. Varsayılan camelCase; gerektiğinde açıkça eşlenir
/// (ör. renk modu "CMYK", birim "mm"). Okurken büyük/küçük harf duyarsızdır.
/// </summary>
internal sealed class TextEnum<T>
    where T : struct, Enum
{
    private readonly Dictionary<T, string> toText = [];
    private readonly Dictionary<string, T> fromText = new(StringComparer.OrdinalIgnoreCase);

    public TextEnum(params (T Value, string Text)[] overrides)
    {
        foreach (var value in Enum.GetValues<T>())
        {
            var text = JsonNamingPolicy.CamelCase.ConvertName(Enum.GetName(value)!);
            foreach (var (overrideValue, overrideText) in overrides)
            {
                if (EqualityComparer<T>.Default.Equals(overrideValue, value))
                {
                    text = overrideText;
                }
            }

            toText[value] = text;
            fromText[text] = value;
        }
    }

    /// <summary>Okumada ek olarak kabul edilen eş anlamlı yazımlar.</summary>
    public TextEnum<T> Alias(string text, T value)
    {
        fromText[text] = value;
        return this;
    }

    public string Format(T value) => toText.TryGetValue(value, out var text)
        ? text
        : throw new ArgumentOutOfRangeException(nameof(value), value, $"{typeof(T).Name} için metin karşılığı yok.");

    public string? FormatOrNull(T? value) => value is { } v ? Format(v) : null;

    public T Parse(string? text, string field)
    {
        if (text is not null && fromText.TryGetValue(text, out var value))
        {
            return value;
        }

        throw new FormatException($"'{field}' alanında geçersiz değer: '{text}'.");
    }

    public T Parse(string? text, string field, T defaultValue) => text is null ? defaultValue : Parse(text, field);

    public T? ParseOrNull(string? text, string field) => text is null ? null : Parse(text, field);
}
