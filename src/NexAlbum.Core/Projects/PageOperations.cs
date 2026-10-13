using System.Globalization;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Elle sayfa yönetimi: boş sayfa ekleme, çoğaltma, silme ve sıralama. Kilitli sayfa silinmez ve taşınmaz
/// (kilit, kullanıcının "bu sayfaya dokunma" demesidir). İşlemler değişmez projeyi döndürür; geri al/yinele bu sayede
/// anlık görüntüyle çalışır.
/// </summary>
public static class PageOperations
{
    /// <summary>Projede kullanılmayan ilk "page-NNN" kimliği (otomatik yerleşimle aynı biçim).</summary>
    public static string NextPageId(AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var used = project.Pages.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        for (var n = 1; ; n++)
        {
            var id = "page-" + n.ToString("000", CultureInfo.InvariantCulture);
            if (!used.Contains(id))
            {
                return id;
            }
        }
    }

    /// <summary>
    /// <paramref name="afterIndex"/> numaralı sayfanın arkasına boş sayfa ekler (−1 ya da sayfa yoksa başa).
    /// Yeni sayfa komşusunun fonunu alır; albüm görsel olarak tutarlı kalır.
    /// </summary>
    /// <returns>Yeni proje ve eklenen sayfanın sırası.</returns>
    public static (AlbumProject Project, int Index) AddBlank(AlbumProject project, int afterIndex, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(project);
        var index = Math.Clamp(afterIndex + 1, 0, project.Pages.Count);
        var neighbour = project.Pages.ElementAtOrDefault(Math.Min(afterIndex, project.Pages.Count - 1));
        var page = new AlbumPage
        {
            Id = NextPageId(project),
            Background = neighbour?.Background ?? BackgroundFill.White,
            CreatedAt = now,
            UpdatedAt = now,
        };
        return (project.AddPage(page, index), index);
    }

    /// <summary>Sayfanın kopyasını hemen arkasına ekler (aynı fotoğraflar, yeni kimlik, kilitsiz).</summary>
    public static (AlbumProject Project, int Index) Duplicate(AlbumProject project, string pageId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(project);
        var index = IndexOf(project, pageId);
        var copy = project.Pages[index] with
        {
            Id = NextPageId(project),
            Locked = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        return (project.AddPage(copy, index + 1), index + 1);
    }

    /// <summary>Sayfayı siler; kilitliyse silmez.</summary>
    /// <returns>Yeni proje; sayfa kilitliyse <c>null</c>.</returns>
    public static AlbumProject? Delete(AlbumProject project, string pageId)
    {
        ArgumentNullException.ThrowIfNull(project);
        var index = IndexOf(project, pageId);
        return project.Pages[index].Locked ? null : project.RemovePage(pageId);
    }

    /// <summary>
    /// Sayfayı yeni sırasına taşır. Kilitli sayfa yerinden oynamaz; başka bir sayfa da kilitli bir sayfanın yerini alamaz
    /// (kilitli sayfaların sırası korunur).
    /// </summary>
    /// <returns>Yeni proje; taşınamıyorsa <c>null</c>, sıra değişmiyorsa aynı proje.</returns>
    public static AlbumProject? Move(AlbumProject project, string pageId, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(project);
        var index = IndexOf(project, pageId);
        newIndex = Math.Clamp(newIndex, 0, project.Pages.Count - 1);
        if (newIndex == index)
        {
            return project;
        }

        var from = Math.Min(index, newIndex);
        var to = Math.Max(index, newIndex);
        for (var i = from; i <= to; i++)
        {
            if (project.Pages[i].Locked)
            {
                return null;
            }
        }

        return project.MovePage(pageId, newIndex);
    }

    private static int IndexOf(AlbumProject project, string pageId)
    {
        for (var i = 0; i < project.Pages.Count; i++)
        {
            if (string.Equals(project.Pages[i].Id, pageId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new ArgumentException("Sayfa bulunamadı: " + pageId, nameof(pageId));
    }
}
