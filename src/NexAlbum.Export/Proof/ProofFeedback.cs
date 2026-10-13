using System.Globalization;
using System.Text.Json;
using NexAlbum.Core.Projects;

namespace NexAlbum.Export.Proof;

/// <summary>Provada bir sayfanın geri bildirimi.</summary>
public sealed record ProofPageFeedback(string Id, bool Liked, string Note);

/// <summary>Sonuç: kaç sayfada beğeni/not işlendi, tanınmayan sayfa sayısı.</summary>
public sealed record ProofApplyResult(AlbumProject Project, int Liked, int Notes, int Unknown);

/// <summary>
/// HTML provadan dönen geri bildirim (Karar 11 Rev. 2). Dosya yalnızca okunur; içindeki metin projeye not olarak yazılır, hiçbir
/// komut çalıştırılmaz. Başka bir projenin dosyası reddedilir.
/// </summary>
public sealed record ProofFeedback(Guid ProjectId, string ProjectName, string General, IReadOnlyList<ProofPageFeedback> Pages)
{
    /// <summary>Not uzunluğu sınırı (karakter); daha uzunu kırpılır.</summary>
    public const int MaxNote = 2000;

    /// <summary>Sayfa notunda prova satırının başı.</summary>
    public const string NotePrefix = "Prova: ";

    public static ProofFeedback Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("format", out var format)
            || !string.Equals(format.GetString(), ProofDocument.FeedbackFormat, StringComparison.Ordinal))
        {
            throw new FormatException("Bu dosya bir NexAlbum prova geri bildirimi değil.");
        }

        if (!root.TryGetProperty("projectId", out var id) || !Guid.TryParse(id.GetString(), CultureInfo.InvariantCulture, out var projectId))
        {
            throw new FormatException("Geri bildirimde proje kimliği yok.");
        }

        var pages = new List<ProofPageFeedback>();
        if (root.TryGetProperty("pages", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in list.EnumerateArray())
            {
                if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty("id", out var pid) || pid.GetString() is not { Length: > 0 } pageId)
                {
                    continue;
                }

                var liked = p.TryGetProperty("liked", out var l) && l.ValueKind == JsonValueKind.True;
                var note = p.TryGetProperty("note", out var n) && n.ValueKind == JsonValueKind.String ? Clean(n.GetString()) : string.Empty;
                pages.Add(new ProofPageFeedback(pageId, liked, note));
            }
        }

        var name = root.TryGetProperty("projectName", out var pn) && pn.ValueKind == JsonValueKind.String ? pn.GetString() ?? string.Empty : string.Empty;
        var general = root.TryGetProperty("general", out var g) && g.ValueKind == JsonValueKind.String ? Clean(g.GetString()) : string.Empty;
        return new ProofFeedback(projectId, name, general, pages);
    }

    /// <summary>
    /// Sayfa notlarına yazar: "Prova: ♥ beğenildi · not". Önceki prova satırı yenisiyle değişir, tasarımcının kendi notları
    /// korunur. Geri bildirimi olmayan sayfaya dokunulmaz.
    /// </summary>
    public ProofApplyResult ApplyTo(AlbumProject project, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.Id != ProjectId)
        {
            throw new InvalidOperationException("Bu geri bildirim başka bir albümün provasına ait (" + ProjectName + ").");
        }

        var byId = Pages.GroupBy(p => p.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);
        var known = project.Pages.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        var liked = 0;
        var notes = 0;
        var updated = project;
        foreach (var page in project.Pages)
        {
            if (!byId.TryGetValue(page.Id, out var feedback) || (!feedback.Liked && feedback.Note.Length == 0))
            {
                continue;
            }

            liked += feedback.Liked ? 1 : 0;
            notes += feedback.Note.Length > 0 ? 1 : 0;
            var line = NotePrefix + string.Join(" · ", new[] { feedback.Liked ? "♥ beğenildi" : null, feedback.Note.Length > 0 ? feedback.Note.Replace("\n", " / ", StringComparison.Ordinal) : null }.OfType<string>());
            updated = updated.ReplacePage(page with { Notes = Merge(page.Notes, line), UpdatedAt = now });
        }

        if (General.Length > 0)
        {
            var description = Merge(updated.Description, NotePrefix + General.Replace("\n", " / ", StringComparison.Ordinal));
            updated = updated with { Description = description };
        }

        return new ProofApplyResult(updated, liked, notes, byId.Keys.Count(k => !known.Contains(k)));
    }

    /// <summary>Önceki "Prova:" satırlarını çıkarıp yenisini sona ekler.</summary>
    private static string Merge(string? existing, string line)
    {
        var kept = (existing ?? string.Empty)
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.Length > 0 && !l.StartsWith(NotePrefix, StringComparison.Ordinal));
        return string.Join("\n", kept.Append(line));
    }

    private static string Clean(string? text)
    {
        var t = (text ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Trim();
        t = new string([.. t.Where(c => !char.IsControl(c) || c == '\n')]);
        return t.Length > MaxNote ? t[..MaxNote] : t;
    }
}
