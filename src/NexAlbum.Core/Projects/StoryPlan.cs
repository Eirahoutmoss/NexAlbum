using NexAlbum.Core.Common;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Albümün bir bölümü (hazırlık, gelin alma, nikâh…). Üyelik saklanmaz, başlangıç zamanından hesaplanır: bir fotoğraf,
/// çekim zamanı kendisinden önce ya da aynı anda başlayan son bölüme aittir. Böylece sonradan eklenen fotoğraflar da
/// kendiliğinden doğru bölüme düşer (Karar 10 Rev. 2).
/// </summary>
/// <param name="Id">Kalıcı kimlik.</param>
/// <param name="Name">Görünen ad (açılış sayfasında başlık).</param>
/// <param name="Start">Bölümün başladığı an (çekim saati, fotoğrafın kendi saat dilimiyle).</param>
public sealed record StoryChapter(string Id, string Name, DateTimeOffset Start)
{
    public string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); } = Id;

    public string Name { get; init => field = Guard.NotBlank(value, nameof(Name)).Trim(); } = Name;
}

/// <summary>Bölümleriyle birlikte fotoğraflar (zaman sırasıyla).</summary>
public sealed record ChapterPhotos(StoryChapter? Chapter, IReadOnlyList<PhotoAsset> Photos);

/// <summary>Hikâye akışı: bölümler, otomatik yerleşimde kullanılıp kullanılmayacağı ve açılış sayfaları.</summary>
public sealed record StoryPlan
{
    public static StoryPlan Empty { get; } = new();

    /// <summary>Başlangıca göre sıralı bölümler.</summary>
    public IReadOnlyList<StoryChapter> Chapters
    {
        get;
        init => field = [.. (value ?? throw new ArgumentNullException(nameof(Chapters))).OrderBy(c => c.Start)];
    } = [];

    /// <summary>Otomatik oluşturma bölümlere göre çalışsın mı (her bölüm yeni sayfada başlar).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Her bölüm, en iyi fotoğrafıyla tam sayfa ve bölüm adıyla açılsın mı.</summary>
    public bool Openers { get; init; }

    public bool HasChapters => Chapters.Count > 0;

    /// <summary>Fotoğrafın bölümü; zamanı yoksa ya da ilk bölümden önceyse null.</summary>
    public StoryChapter? ChapterOf(PhotoAsset photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        if (photo.CapturedAt is not { } at)
        {
            return null;
        }

        StoryChapter? found = null;
        foreach (var chapter in Chapters)
        {
            if (chapter.Start > at)
            {
                break;
            }

            found = chapter;
        }

        return found ?? (Chapters.Count > 0 ? Chapters[0] : null);
    }

    /// <summary>
    /// Fotoğrafları bölümlerine dağıtır: bölümler zaman sırasıyla, her bölümde fotoğraflar çekim zamanına göre (eşitlikte
    /// verilen sıra). Zamanı olmayanlar sonda, bölümsüz bir grupta. Boş bölüm döndürülmez.
    /// </summary>
    public IReadOnlyList<ChapterPhotos> Distribute(IEnumerable<PhotoAsset> photos)
    {
        ArgumentNullException.ThrowIfNull(photos);
        var list = photos.ToList();
        var timed = list.Where(p => p.CapturedAt is not null).OrderBy(p => p.CapturedAt).ToList();
        var untimed = list.Where(p => p.CapturedAt is null).ToList();
        var result = new List<ChapterPhotos>();
        if (Chapters.Count == 0)
        {
            if (timed.Count > 0)
            {
                result.Add(new ChapterPhotos(null, timed));
            }
        }
        else
        {
            foreach (var group in timed.GroupBy(ChapterOf))
            {
                result.Add(new ChapterPhotos(group.Key, [.. group]));
            }
        }

        if (untimed.Count > 0)
        {
            result.Add(new ChapterPhotos(null, untimed));
        }

        return result;
    }

    public StoryPlan Rename(string chapterId, string name) => this with
    {
        Chapters = [.. Chapters.Select(c => c.Id == chapterId ? c with { Name = name } : c)],
    };

    /// <summary>Bölümü öncekiyle birleştirir (sınır kalkar); ilk bölüm sonrakiyle birleşir.</summary>
    public StoryPlan MergeWithPrevious(string chapterId)
    {
        var index = Chapters.ToList().FindIndex(c => c.Id == chapterId);
        if (index < 0 || Chapters.Count < 2)
        {
            return this;
        }

        // İlk bölümün öncesi yoktur: sonraki bölüm ona katılır (ilk bölümün adı ve başlangıcı kalır).
        var removed = index == 0 ? Chapters[1].Id : chapterId;
        return this with { Chapters = [.. Chapters.Where(c => c.Id != removed)] };
    }

    /// <summary>Verilen anda yeni bölüm başlatır (aynı anda başlayan bölüm varsa yalnızca adını değiştirir).</summary>
    public StoryPlan SplitAt(DateTimeOffset start, string name)
    {
        if (Chapters.FirstOrDefault(c => c.Start == start) is { } same)
        {
            return Rename(same.Id, name);
        }

        return this with { Chapters = [.. Chapters, new StoryChapter(NextId(), name, start)] };
    }

    public string NextId()
    {
        for (var i = 1; ; i++)
        {
            var id = "chapter-" + i.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
            if (Chapters.All(c => c.Id != id))
            {
                return id;
            }
        }
    }
}
