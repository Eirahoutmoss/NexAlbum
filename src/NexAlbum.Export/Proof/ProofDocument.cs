using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NexAlbum.Export.Proof;

/// <summary>Provadaki bir sayfa: kimlik, numara ve önizleme görüntüsü (JPEG baytları).</summary>
public sealed record ProofPage(string Id, string Label, byte[] Jpeg, int Width, int Height);

/// <summary>
/// Çevrimdışı HTML prova (Karar 11 Rev. 2): tek dosya, internet gerektirmez, hiçbir sunucuya veri göndermez. Müşteri sayfaları
/// gezer, beğenir, not yazar ve "Geri bildirimi kaydet" ile küçük bir JSON dosyası indirir; tasarımcı bu dosyayı NexAlbum'e geri
/// alır. Görüntüler dosyanın içine gömülüdür (JPEG, data URI). Yazılanlar tarayıcıda da saklanır (aynı dosya yeniden açılınca).
/// </summary>
public static class ProofDocument
{
    /// <summary>Geri bildirim dosyasının biçim adı.</summary>
    public const string FeedbackFormat = "nexalbum-proof-feedback";

    /// <summary>Gösteride varsayılan geçiş efekti (müşteri tarayıcıda değiştirebilir).</summary>
    public const string DefaultEffect = "aperture";

    /// <param name="albumName">Albüm adı.</param>
    /// <param name="projectId">Proje kimliği (geri bildirim bu projeye döner).</param>
    /// <param name="pages">Sayfalar (kapak dahil, gösterim sırasıyla).</param>
    /// <param name="createdAt">Oluşturma zamanı.</param>
    /// <param name="effect">Gösterinin açılış efekti (effects.js adı, ör. "aperture", "honeycomb"); karışık için "__mix__".</param>
    public static string Build(string albumName, Guid projectId, IReadOnlyList<ProofPage> pages, DateTimeOffset createdAt, string effect = DefaultEffect)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumName);
        ArgumentNullException.ThrowIfNull(pages);
        var meta = JsonSerializer.Serialize(new
        {
            format = FeedbackFormat,
            version = 1,
            projectId = projectId.ToString("D", CultureInfo.InvariantCulture),
            projectName = albumName,
            createdAt = createdAt.ToString("O", CultureInfo.InvariantCulture),
            effect = string.IsNullOrWhiteSpace(effect) ? DefaultEffect : effect,
            pages = pages.Select(p => new { id = p.Id, label = p.Label }),
        });

        var cards = new StringBuilder();
        foreach (var p in pages)
        {
            var aspect = string.Create(CultureInfo.InvariantCulture, $"{Math.Max(1, p.Width)} / {Math.Max(1, p.Height)}");
            cards.Append(CultureInfo.InvariantCulture, $"""
                      <button class="card" type="button" data-id="{Attr(p.Id)}" style="--a:{aspect}" aria-label="{Attr(p.Label)}"><img alt="{Attr(p.Label)}" width="{p.Width}" height="{p.Height}" src="data:image/jpeg;base64,{Convert.ToBase64String(p.Jpeg)}"><span class="heart" aria-hidden="true">♥</span><span class="dot" aria-hidden="true">Not</span></button>

                """);
        }

        return Template
            .Replace("{{TITLE}}", Html(albumName), StringComparison.Ordinal)
            .Replace("{{COUNT}}", pages.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{DATE}}", Html(createdAt.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR"))), StringComparison.Ordinal)
            .Replace("{{META}}", meta.Replace("</", "<\\/", StringComparison.Ordinal), StringComparison.Ordinal)
            .Replace("{{EFFECTS}}", ProofAssets.EffectsScript.Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase), StringComparison.Ordinal)
            .Replace("{{PAGES}}", cards.ToString(), StringComparison.Ordinal);
    }

    private static string Html(string text) => WebUtility.HtmlEncode(text);

    private static string Attr(string text) => WebUtility.HtmlEncode(text);

    /// <summary>Prova şablonu (Assets/proof.html; odak şeridi, gösteri ve geri bildirim).</summary>
    private static string Template => ProofAssets.ProofTemplate;
}
