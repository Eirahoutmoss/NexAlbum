using System.Globalization;
using NexAlbum.Core.Effects;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Video;

/// <summary>
/// Projeden video zaman çizelgesi kurar (Karar 21): hikâye bölümleri, açılış ve bölüm başlıkları, fotoğraf seçimi (sosyal
/// biçimde hedef süreye göre, favoriler önce), süreler (favoriler daha uzun), geçişler (stil havuzundan tohumlu ya da elle),
/// Ken Burns kadrajları ve müzik vuruşuna oturan kesmeler. Saf ve deterministiktir.
/// </summary>
public static class VideoPlanner
{
    /// <summary>Bir fotoğraf, video oranından bu kat daha farklıysa bulanık zemin üzerinde bütün olarak gösterilir.</summary>
    public const double BackdropRatio = 1.45;

    /// <summary>Başlık kartı süresi (sn).</summary>
    public const double TitleSeconds = 2.4;

    public static VideoTimeline Plan(AlbumProject project, VideoSettings settings, BeatGrid? music = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(settings);
        var format = VideoFormat.Find(settings.FormatId);
        var style = VideoStyle.Find(settings.StyleId);
        var random = new DeterministicRandom(LayoutSeed.FromProjectId(project.Id) ^ (ulong)StableHash(style.Id + format.Id));
        var notes = new List<string>();
        var tr = CultureInfo.GetCultureInfo("tr-TR");

        // 1. Fotoğraflar: albüm sayfalarında kullanılanlar (yoksa hepsi); dosyası olmayanlar çıkar.
        var used = project.Pages.SelectMany(p => p.Slots).Select(s => s.Placement?.AssetId).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var pool = project.Photos.Where(p => !p.Missing || p.IsEditActive).Where(p => used.Count == 0 || used.Contains(p.Id)).ToList();
        if (pool.Count == 0)
        {
            return new VideoTimeline(format, settings.Fps, [], 0, style.Vignette, style.Grain, 0, ["Videoya girecek fotoğraf yok."]);
        }

        var groups = project.Story.HasChapters ? project.Story.Distribute(pool) : [new ChapterPhotos(null, OrderForFilm(pool))];

        // 2. Sosyal biçim / hedef süre: fotoğraf sayısı bütçesi, bölümlere orantılı.
        var titles = settings.ChapterTitles ? groups.Count(g => g.Chapter is not null) + 1 : 0;
        var target = settings.TargetSeconds ?? (format.Social ? 30 : null);
        if (target is { } seconds)
        {
            var titleTime = Math.Min(titles * TitleSeconds * 0.8, seconds * 0.3);
            if (titleTime > seconds * 0.2)
            {
                titles = 1; // kısa kliplerde yalnızca açılış başlığı
                titleTime = TitleSeconds;
            }

            var budget = Math.Max(1, (int)Math.Round((seconds - titleTime) / Math.Max(0.8, style.PhotoSeconds - (style.TransitionSeconds / 2))));
            groups = Budget(groups, budget);
            notes.Add(string.Format(tr, "Hedef {0:0} sn: {1} fotoğraf seçildi (önce favoriler).", seconds, groups.Sum(g => g.Photos.Count)));
        }

        var showChapterTitles = settings.ChapterTitles && titles > 1;

        // 3. Öğe dizisi (süreler henüz göreli).
        var items = new List<(VideoClip Template, double Length, bool ChapterStart)>();
        if (settings.ChapterTitles)
        {
            var first = pool.Where(p => p.CapturedAt is not null).Select(p => p.CapturedAt!.Value).DefaultIfEmpty().Min();
            var subtitle = first == default ? null : first.DateTime.ToString("d MMMM yyyy", tr).ToUpper(tr);
            var opening = OrderForFilm(pool);
            if (style.Intro == VideoIntro.Book && opening.Count >= 2)
            {
                // Albüm açılışı: kapakta albüm adı, açılınca ilk iki fotoğraf çift sayfa olarak.
                items.Add((new BookClip(0, 0, TransitionKind.Cut, 0, style.Look, project.Name, subtitle, opening[0].Id, opening[1].Id), TitleSeconds * 1.7, true));
            }
            else
            {
                items.Add((new TitleClip(0, 0, TransitionKind.Cut, 0, style.Look, project.Name, subtitle) { Animation = style.Title }, TitleSeconds, true));
            }
        }

        foreach (var group in groups)
        {
            var chapterOverride = group.Chapter is { } ch ? settings.Overrides.FirstOrDefault(o => o.ChapterId == ch.Id) : null;
            var look = chapterOverride?.Look ?? style.Look;
            var startOfChapter = true;
            if (showChapterTitles && group.Chapter is { } chapter)
            {
                var range = group.Photos.Where(p => p.CapturedAt is not null).Select(p => p.CapturedAt!.Value.DateTime).ToList();
                var sub = range.Count > 0 ? range.Min().ToString("HH:mm", tr) : null;
                items.Add((new TitleClip(0, 0, chapterOverride?.Transition ?? style.ChapterTransition, 0, look, chapter.Name, sub) { Animation = style.Title }, TitleSeconds * 0.85, true));
                startOfChapter = false;
            }

            // Sahne stillerinde fotoğraflar gruplanır (galeri 5'li, kolaj 4'lü); 3'ten az kalanlar tek tek gösterilir.
            var singles = group.Photos.ToList();
            if (style.Scene != VideoScene.None && !settings.ReduceMotion && group.Photos.Count >= 3)
            {
                var size = style.Scene == VideoScene.FocusGallery ? 5 : 4;
                singles = [];
                for (var start = 0; start < group.Photos.Count; start += size)
                {
                    var chunk = group.Photos.Skip(start).Take(size).ToList();
                    if (chunk.Count < 3)
                    {
                        singles.AddRange(chunk);
                        continue;
                    }

                    var transition = startOfChapter && items.Count > 0
                        ? chapterOverride?.Transition ?? style.ChapterTransition
                        : chapterOverride?.Transition ?? style.Transitions[random.Next(style.Transitions.Count)];
                    var ids = chunk.Select(p => p.Id).ToList();
                    items.Add(style.Scene == VideoScene.FocusGallery
                        ? ((VideoClip)new GalleryClip(0, 0, transition, 0, look, ids, group.Chapter?.Name), style.PhotoSeconds * chunk.Count * 0.75, startOfChapter)
                        : ((VideoClip)new CollageClip(0, 0, transition, 0, look, ids), style.PhotoSeconds * 2.2, startOfChapter));
                    startOfChapter = false;
                }
            }

            foreach (var photo in singles)
            {
                var transition = startOfChapter && items.Count > 0
                    ? chapterOverride?.Transition ?? style.ChapterTransition
                    : chapterOverride?.Transition ?? style.Transitions[random.Next(style.Transitions.Count)];
                var length = style.PhotoSeconds * (photo.Favorite ? 1.3 : 1);
                var (from, to, backdrop) = Motion(photo.DisplayAspectRatio, format.Aspect, settings.ReduceMotion ? 0 : style.MotionAmount, random);
                items.Add((new PhotoClip(0, 0, transition, 0, look, photo.Id, from, to) { BlurredBackdrop = backdrop }, length, startOfChapter));
                startOfChapter = false;
            }
        }

        // 4. Zamanlama: kesme anları (geçişin ortası) vuruşa oturtulur.
        var beats = music is { IsUsable: true } grid ? grid with { Beats = [.. grid.Beats.Select(b => b - grid.LeadIn).Where(b => b > 0)] } : null;
        if (beats is not null)
        {
            notes.Add(string.Format(tr, "Müzik: {0:0} BPM; kesmeler vuruşa oturtuldu.", music!.Bpm));
        }
        else if (music is not null)
        {
            notes.Add("Müzikte belirgin vuruş bulunamadı; sabit ritim kullanıldı.");
        }

        var clips = new List<VideoClip>(items.Count);
        var cut = 0.0;
        for (var i = 0; i < items.Count; i++)
        {
            var (template, length, _) = items[i];
            var kind = settings.ReduceMotion ? Calm(template.TransitionIn) : template.TransitionIn;
            var transition = i == 0 || kind == TransitionKind.Cut ? 0 : Math.Min(style.TransitionSeconds * TransitionEffects.DurationFactor(kind), length * 0.45);
            var start = Math.Max(0, cut - (transition / 2));
            var nextCut = cut + length;
            if (beats is not null && i < items.Count - 1)
            {
                nextCut = beats.Snap(nextCut, length * 0.35);
            }

            var nextTransition = i + 1 < items.Count && items[i + 1].Template.TransitionIn != TransitionKind.Cut
                ? Math.Min(style.TransitionSeconds * TransitionEffects.DurationFactor(settings.ReduceMotion ? Calm(items[i + 1].Template.TransitionIn) : items[i + 1].Template.TransitionIn), items[i + 1].Length * 0.45)
                : 0;
            var end = i == items.Count - 1 ? nextCut : nextCut + (nextTransition / 2);
            clips.Add(template with { Start = start, Duration = Math.Max(0.2, end - start), TransitionIn = i == 0 ? TransitionKind.Cut : kind, TransitionSeconds = transition });
            cut = nextCut;
        }

        var duration = clips.Count == 0 ? 0 : clips[^1].End;
        return new VideoTimeline(format, settings.Fps, clips, duration, style.Vignette, style.Grain, music?.LeadIn ?? 0, notes);
    }

    /// <summary>
    /// Ken Burns kadrajları: video oranındaki en büyük pencere, ölçek 1 ile 1/(1+hareket) arasında yaklaşır ya da uzaklaşır,
    /// yönü tohumla seçilir. Oranı çok farklı fotoğraf (dikey fotoğraf yatay videoda) bütün gösterilir; zemin bulanık kopyadır.
    /// </summary>
    public static (Frame01 From, Frame01 To, bool Backdrop) Motion(double photoAspect, double videoAspect, double amount, DeterministicRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var ratio = Math.Max(photoAspect, videoAspect) / Math.Min(photoAspect, videoAspect);
        var backdrop = ratio >= BackdropRatio;

        // Pencere (fotoğraf oranında 0–1): kapla modunda video oranındaki en büyük pencere; bulanık zeminde fotoğrafın tamamı.
        double w = 1, h = 1;
        if (!backdrop)
        {
            if (photoAspect > videoAspect)
            {
                w = videoAspect / photoAspect;
            }
            else
            {
                h = photoAspect / videoAspect;
            }
        }

        var full = new Frame01((1 - w) / 2, (1 - h) / 2, w, h);
        if (amount <= 0)
        {
            return (full, full, backdrop);
        }

        var k = 1 / (1 + amount);
        var zw = w * k;
        var zh = h * k;

        // Yakın pencerenin konumu: merkez ya da hafif üst üçte bir (yüzler çoğunlukla üstte), sol/sağ kayma tohumla.
        var dx = (random.NextDouble() - 0.5) * (w - zw);
        var dy = -(h - zh) * 0.25 * random.NextDouble();
        var close = new Frame01(full.X + ((w - zw) / 2) + dx, full.Y + ((h - zh) / 2) + dy, zw, zh);
        return random.Next(2) == 0 ? (full, close, backdrop) : (close, full, backdrop);
    }

    private static TransitionKind Calm(TransitionKind kind) => kind switch
    {
        TransitionKind.Cut or TransitionKind.Crossfade or TransitionKind.DipToBlack or TransitionKind.DipToWhite => kind,
        _ => TransitionKind.Crossfade,
    };

    /// <summary>Bölümsüz filmde sıra: çekim zamanı (yoksa içe aktarma sırası).</summary>
    private static List<PhotoAsset> OrderForFilm(List<PhotoAsset> pool) =>
        [.. pool.Where(p => p.CapturedAt is not null).OrderBy(p => p.CapturedAt), .. pool.Where(p => p.CapturedAt is null)];

    /// <summary>Bütçeyi bölümlere fotoğraf sayısıyla orantılı dağıtır (her bölüme en az 1); bölümde önce favoriler, sonra zamana eşit aralıklı.</summary>
    private static IReadOnlyList<ChapterPhotos> Budget(IReadOnlyList<ChapterPhotos> groups, int budget)
    {
        var total = groups.Sum(g => g.Photos.Count);
        if (budget >= total)
        {
            return groups;
        }

        var keep = groups.Count <= budget ? groups : [.. groups.OrderByDescending(g => g.Photos.Count).Take(budget).OrderBy(g => groups.ToList().IndexOf(g))];
        var shares = keep.Select(g => Math.Max(1, (int)Math.Floor((double)budget * g.Photos.Count / keep.Sum(x => x.Photos.Count)))).ToArray();
        for (var i = 0; shares.Sum() < budget; i = (i + 1) % shares.Length)
        {
            if (shares[i] < keep[i].Photos.Count)
            {
                shares[i]++;
            }
            else if (keep.Select((g, j) => shares[j] < g.Photos.Count).All(x => !x))
            {
                break;
            }
        }

        while (shares.Sum() > budget)
        {
            var largest = Array.IndexOf(shares, shares.Max());
            shares[largest]--;
        }

        return [.. keep.Select((g, i) => g with { Photos = Pick(g.Photos, shares[i]) })];
    }

    private static List<PhotoAsset> Pick(IReadOnlyList<PhotoAsset> photos, int count)
    {
        if (count >= photos.Count)
        {
            return [.. photos];
        }

        var chosen = photos.Where(p => p.Favorite).Take(count).ToHashSet();
        var rest = photos.Where(p => !chosen.Contains(p)).ToList();
        var need = count - chosen.Count;
        for (var i = 0; i < need; i++)
        {
            chosen.Add(rest[(int)Math.Floor((i + 0.5) * rest.Count / need)]);
        }

        return [.. photos.Where(chosen.Contains)];
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            var h = 17;
            foreach (var c in s)
            {
                h = (h * 31) + c;
            }

            return h & 0x7fffffff;
        }
    }
}
