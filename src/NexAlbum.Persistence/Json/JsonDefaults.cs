using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexAlbum.Persistence.Json;

/// <summary>
/// Tüm proje JSON dosyaları için ortak serileştirme ayarları (Karar 09 §2.1, §21.2).
/// </summary>
internal static class JsonDefaults
{
    /// <summary>Aşırı iç içe JSON reddedilir.</summary>
    public const int MaxDepth = 64;

    public static JsonSerializerOptions Options { get; } = Create();

    public static JsonDocumentOptions DocumentOptions { get; } = new() { MaxDepth = MaxDepth };

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowOutOfOrderMetadataProperties = true,
            MaxDepth = MaxDepth,

            // Dosya HTML'e gömülmez; Türkçe karakterler ve "&" okunabilir kalsın (insan okunabilir ilkesi).
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling = JsonNumberHandling.Strict,
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
