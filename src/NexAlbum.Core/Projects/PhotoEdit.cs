using System.Text;
using NexAlbum.Core.Common;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Bir fotoğrafın harici araçla üretilmiş düzenlenmiş sürümü (Karar 20 §4). Orijinal dosya değişmez;
/// boyut ve yön çıktı dosyasından okunur.
/// </summary>
public sealed record PhotoEdit
{
    public required string Path { get; init => field = Guard.NotBlank(value, nameof(Path)); }

    public required int PixelWidth { get; init => field = Guard.Positive(value, nameof(PixelWidth)); }

    public required int PixelHeight { get; init => field = Guard.Positive(value, nameof(PixelHeight)); }

    public int? ExifOrientation { get; init => field = value is null ? null : Guard.InRange(value.Value, 1, 8, nameof(ExifOrientation)); }

    /// <summary>Üreten araç (ör. "Photoshop", "darktable-cli", "Harici düzenleyici").</summary>
    public required string Tool { get; init => field = Guard.NotBlank(value, nameof(Tool)); }

    /// <summary>Uygulanan tarif (ör. "Stüdyo / Sıcak Ton"); elle düzenlemede null.</summary>
    public string? Recipe { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Dosya bulunamadı mı? Açılışta denetlenir; kayıpsa orijinal kullanılır.</summary>
    public bool Missing { get; init; }
}

/// <summary>Toplu düzenleme motoru.</summary>
public enum EditEngineKind
{
    /// <summary>Adobe Photoshop action'ı (COM + ExtendScript).</summary>
    Photoshop,

    /// <summary>Komut satırı aracı (darktable-cli, rawtherapee-cli, …) ve bağımsız değişken şablonu.</summary>
    CommandLine,
}

/// <summary>
/// Düzenlenmiş dosya biçimi. PDF'e doğrudan (yeniden sıkıştırmadan) gömülebilen biçimler seçilmiştir:
/// JPEG en yüksek kalitede baskı standardıdır; PNG kayıpsızdır ama büyüktür.
/// </summary>
public enum EditOutputFormat
{
    Jpeg,
    Png,
}

/// <summary>
/// Toplu düzenleme tarifi: hangi araç, hangi action/ön ayar, hangi çıktı biçimi. Projede saklanır; yeni eklenen
/// fotoğraflara aynı tarif uygulanabilir (Karar 20 §1).
/// </summary>
public sealed record EditRecipe
{
    /// <summary>Komut şablonunda girdi dosyasının yeri.</summary>
    public const string InputToken = "{input}";

    /// <summary>Komut şablonunda çıktı dosyasının yeri.</summary>
    public const string OutputToken = "{output}";

    public required EditEngineKind Engine { get; init; }

    /// <summary>Görünen ad; dosya adı kısaltması buradan türetilir.</summary>
    public required string Name { get; init => field = Guard.NotBlank(value, nameof(Name)); }

    /// <summary>Photoshop action seti.</summary>
    public string? ActionSet { get; init; }

    /// <summary>Photoshop action'ı.</summary>
    public string? Action { get; init; }

    /// <summary>Komut satırı aracının yolu.</summary>
    public string? Executable { get; init; }

    /// <summary>Bağımsız değişkenler; <see cref="InputToken"/> ve <see cref="OutputToken"/> içerir. Kabuk kullanılmaz.</summary>
    public IReadOnlyList<string> Arguments { get; init => field = value ?? throw new ArgumentNullException(nameof(Arguments)); } = [];

    public EditOutputFormat Output { get; init; } = EditOutputFormat.Jpeg;

    /// <summary>JPEG kalitesi (Photoshop ölçeği 1–12).</summary>
    public int JpegQuality { get; init => field = Guard.InRange(value, 1, 12, nameof(JpegQuality)); } = 12;

    public string Extension => Output == EditOutputFormat.Png ? ".png" : ".jpg";

    /// <summary>Tarif kendi içinde tutarlı mı? Değilse kullanıcıya gösterilecek sebep.</summary>
    public string? Problem => Engine switch
    {
        EditEngineKind.Photoshop when string.IsNullOrWhiteSpace(ActionSet) || string.IsNullOrWhiteSpace(Action) => "Action seti ve action seçilmeli.",
        EditEngineKind.CommandLine when string.IsNullOrWhiteSpace(Executable) => "Komut satırı aracının yolu girilmeli.",
        EditEngineKind.CommandLine when !Arguments.Any(a => a.Contains(InputToken, StringComparison.Ordinal))
            || !Arguments.Any(a => a.Contains(OutputToken, StringComparison.Ordinal)) => $"Bağımsız değişkenlerde {InputToken} ve {OutputToken} bulunmalı.",
        _ => null,
    };

    /// <summary>Dosya adına uygun kısaltma: küçük harf ASCII, rakam ve tire (Türkçe harfler sadeleştirilir).</summary>
    public string Slug
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var c in Name)
            {
                var mapped = c switch
                {
                    'ç' or 'Ç' => 'c',
                    'ğ' or 'Ğ' => 'g',
                    'ı' or 'I' or 'İ' or 'i' => 'i',
                    'ö' or 'Ö' => 'o',
                    'ş' or 'Ş' => 's',
                    'ü' or 'Ü' => 'u',
                    _ => char.ToLowerInvariant(c),
                };
                if (mapped is >= 'a' and <= 'z' or >= '0' and <= '9')
                {
                    sb.Append(mapped);
                }
                else if (sb.Length > 0 && sb[^1] != '-')
                {
                    sb.Append('-');
                }

                if (sb.Length >= 32)
                {
                    break;
                }
            }

            var slug = sb.ToString().Trim('-');
            return slug.Length == 0 ? "duzenleme" : slug;
        }
    }

    /// <summary>Bir fotoğraf için deterministik çıktı dosya adı.</summary>
    public string OutputFileName(string assetId) => Guard.NotBlank(assetId, nameof(assetId)) + "-" + Slug + Extension;
}

/// <summary>Düzenleme ile ilgili proje ayarları.</summary>
public sealed record EditingSettings
{
    /// <summary>Son kullanılan toplu tarif.</summary>
    public EditRecipe? LastRecipe { get; init; }

    public static EditingSettings Default { get; } = new();
}
