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

    public static string Build(string albumName, Guid projectId, IReadOnlyList<ProofPage> pages, DateTimeOffset createdAt)
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
            pages = pages.Select(p => new { id = p.Id, label = p.Label }),
        });

        var figures = new StringBuilder();
        for (var i = 0; i < pages.Count; i++)
        {
            var p = pages[i];
            figures.Append(CultureInfo.InvariantCulture, $"""
                <article class="page" data-id="{Attr(p.Id)}" id="p{i}">
                  <div class="sheet"><img alt="{Attr(p.Label)}" width="{p.Width}" height="{p.Height}" loading="lazy" src="data:image/jpeg;base64,{Convert.ToBase64String(p.Jpeg)}"></div>
                  <div class="bar">
                    <span class="label">{Html(p.Label)}</span>
                    <button class="like" type="button" aria-pressed="false" title="Beğen"><span class="heart">♥</span> <span class="like-text">Beğen</span></button>
                  </div>
                  <textarea class="note" rows="2" placeholder="Bu sayfa için notunuz (ör. bu fotoğraf yerine başka bir tane)"></textarea>
                </article>

                """);
        }

        return Template
            .Replace("{{TITLE}}", Html(albumName), StringComparison.Ordinal)
            .Replace("{{COUNT}}", pages.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{DATE}}", Html(createdAt.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR"))), StringComparison.Ordinal)
            .Replace("{{META}}", meta.Replace("</", "<\\/", StringComparison.Ordinal), StringComparison.Ordinal)
            .Replace("{{PAGES}}", figures.ToString(), StringComparison.Ordinal);
    }

    private static string Html(string text) => WebUtility.HtmlEncode(text);

    private static string Attr(string text) => WebUtility.HtmlEncode(text);

    private const string Template = """
        <!doctype html>
        <html lang="tr">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{{TITLE}} · Prova</title>
        <style>
          :root { --bg:#f6f3ee; --card:#ffffff; --ink:#2b2620; --muted:#7a7166; --line:#e4ddd2; --accent:#b8862f; --like:#c0392b; }
          @media (prefers-color-scheme: dark) { :root { --bg:#16140f; --card:#211e18; --ink:#efe9df; --muted:#a39a8c; --line:#3a352c; --accent:#e0b252; --like:#ff6b5e; } }
          * { box-sizing:border-box; }
          body { margin:0; background:var(--bg); color:var(--ink); font:16px/1.5 -apple-system, "Segoe UI", Roboto, Arial, sans-serif; }
          header { position:sticky; top:0; z-index:2; background:color-mix(in srgb, var(--bg) 88%, transparent); backdrop-filter:blur(8px); border-bottom:1px solid var(--line); }
          .wrap { max-width:1100px; margin:0 auto; padding:16px; }
          header .wrap { display:flex; flex-wrap:wrap; gap:12px; align-items:center; justify-content:space-between; }
          h1 { font-size:20px; margin:0; font-weight:600; letter-spacing:.2px; }
          .sub { color:var(--muted); font-size:13px; }
          .actions { display:flex; gap:8px; align-items:center; flex-wrap:wrap; }
          .count { color:var(--muted); font-size:13px; }
          button { font:inherit; cursor:pointer; border-radius:999px; border:1px solid var(--line); background:var(--card); color:var(--ink); padding:7px 14px; }
          button.primary { background:var(--accent); border-color:var(--accent); color:#1b160d; font-weight:600; }
          .intro { color:var(--muted); font-size:14px; margin:4px 0 8px; }
          .page { background:var(--card); border:1px solid var(--line); border-radius:14px; padding:14px; margin:18px 0; }
          .sheet { background:#fff; border-radius:6px; overflow:hidden; box-shadow:0 6px 24px rgba(0,0,0,.12); }
          .sheet img { display:block; width:100%; height:auto; }
          .bar { display:flex; justify-content:space-between; align-items:center; margin:12px 2px 8px; }
          .label { font-weight:600; }
          .like[aria-pressed="true"] { border-color:var(--like); color:var(--like); }
          .heart { font-size:15px; }
          textarea { width:100%; font:inherit; color:var(--ink); background:transparent; border:1px solid var(--line); border-radius:10px; padding:9px 11px; resize:vertical; }
          textarea:focus { outline:2px solid color-mix(in srgb, var(--accent) 60%, transparent); border-color:transparent; }
          .general { margin:24px 0 40px; }
          .general h2 { font-size:16px; margin:0 0 8px; }
          .saved { color:var(--muted); font-size:12px; min-height:1em; }
          @media (max-width:600px) { .wrap { padding:12px; } .page { padding:10px; } h1 { font-size:17px; } }
        </style>
        </head>
        <body>
        <header>
          <div class="wrap">
            <div>
              <h1>{{TITLE}}</h1>
              <div class="sub">Albüm provası · {{COUNT}} sayfa · {{DATE}}</div>
            </div>
            <div class="actions">
              <span class="count" id="summary"></span>
              <button class="primary" type="button" id="export">Geri bildirimi kaydet</button>
            </div>
          </div>
        </header>
        <main class="wrap">
          <p class="intro">Sayfaları inceleyin; beğendiklerinizi ♥ ile işaretleyin, değişiklik istediğiniz sayfaya not yazın. Bitince «Geri bildirimi kaydet» ile inen küçük dosyayı tasarımcınıza gönderin. Bu dosya internete bağlanmaz, hiçbir yere veri göndermez.</p>
        {{PAGES}}
          <section class="general">
            <h2>Genel notunuz</h2>
            <textarea id="general" rows="3" placeholder="Albümün geneli için düşünceleriniz"></textarea>
            <p class="saved" id="saved"></p>
          </section>
        </main>
        <script id="meta" type="application/json">{{META}}</script>
        <script>
        (function () {
          var meta = JSON.parse(document.getElementById('meta').textContent);
          var key = 'nexalbum-prova-' + meta.projectId + '-' + meta.createdAt;
          var state = { pages: {}, general: '' };
          try { var saved = localStorage.getItem(key); if (saved) { state = JSON.parse(saved); } } catch (e) { }
          state.pages = state.pages || {};
          function persist() {
            try { localStorage.setItem(key, JSON.stringify(state)); document.getElementById('saved').textContent = 'Tarayıcıda saklandı.'; } catch (e) { }
            summary();
          }
          function summary() {
            var liked = 0, notes = 0;
            Object.keys(state.pages).forEach(function (id) { var p = state.pages[id]; if (p.liked) liked++; if (p.note && p.note.trim()) notes++; });
            document.getElementById('summary').textContent = liked + ' beğeni · ' + notes + ' not';
          }
          document.querySelectorAll('.page').forEach(function (el) {
            var id = el.getAttribute('data-id');
            var p = state.pages[id] || (state.pages[id] = { liked: false, note: '' });
            var like = el.querySelector('.like'), note = el.querySelector('.note');
            function show() { like.setAttribute('aria-pressed', p.liked ? 'true' : 'false'); like.querySelector('.like-text').textContent = p.liked ? 'Beğenildi' : 'Beğen'; }
            note.value = p.note || '';
            show();
            like.addEventListener('click', function () { p.liked = !p.liked; show(); persist(); });
            note.addEventListener('input', function () { p.note = note.value; persist(); });
          });
          var general = document.getElementById('general');
          general.value = state.general || '';
          general.addEventListener('input', function () { state.general = general.value; persist(); });
          summary();
          document.getElementById('export').addEventListener('click', function () {
            var out = { format: meta.format, version: meta.version, projectId: meta.projectId, projectName: meta.projectName, proofCreatedAt: meta.createdAt, savedAt: new Date().toISOString(), general: state.general || '', pages: [] };
            meta.pages.forEach(function (m) { var p = state.pages[m.id] || {}; out.pages.push({ id: m.id, label: m.label, liked: !!p.liked, note: p.note || '' }); });
            var blob = new Blob([JSON.stringify(out, null, 2)], { type: 'application/json' });
            var a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = (meta.projectName || 'album').replace(/[\\/:*?"<>|]+/g, '-') + ' - prova geri bildirimi.json';
            document.body.appendChild(a); a.click(); a.remove();
            setTimeout(function () { URL.revokeObjectURL(a.href); }, 1000);
          });
        })();
        </script>
        </body>
        </html>
        """;
}
