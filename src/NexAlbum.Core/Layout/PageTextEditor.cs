using System.Globalization;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Layout;

/// <summary>
/// Sayfadaki metinler: ekle, değiştir, taşı, sil. Saf işlemler (yeni sayfa döner); kilit denetimi çağıranın işidir.
/// Metin kutusu tuvalin (taşma payı dahil) dışına çıkamaz; güvenli alan ihlali ön kontrolde uyarılır.
/// </summary>
public static class PageTextEditor
{
    /// <summary>Metinler yuvaların üstünde durur.</summary>
    public const int TextLayer = 1000;

    /// <summary>Sayfada kullanılmayan ilk "text-NNN" kimliği.</summary>
    public static string NextTextId(AlbumPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        for (var n = 1; ; n++)
        {
            var id = "text-" + n.ToString("000", CultureInfo.InvariantCulture);
            if (page.FindElement(id) is null)
            {
                return id;
            }
        }
    }

    /// <summary>Hazır stille metin ekler; eklenen öğe en üst katmandadır.</summary>
    /// <param name="page">Sayfa.</param>
    /// <param name="preset">Hazır stil.</param>
    /// <param name="pageArea">Bitmiş sayfa alanı (mm, taşma payı hariç).</param>
    /// <param name="content">Yazı; null ise stilin örnek metni.</param>
    /// <param name="now">Zaman damgası.</param>
    public static (AlbumPage Page, TextElement Text) Add(AlbumPage page, TextPreset preset, RectMm pageArea, string? content, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(preset);
        var z = Math.Max(TextLayer, page.Elements.Count == 0 ? 0 : page.Elements.Max(e => e.ZIndex) + 1);
        var text = preset.Create(NextTextId(page), content, pageArea, z);
        return (page.AddElement(text) with { UpdatedAt = now }, text);
    }

    /// <summary>Metni değiştirir (yazı, yazı tipi, hizalama…). Değişiklik yoksa aynı sayfa döner.</summary>
    public static AlbumPage Update(AlbumPage page, string textId, Func<TextElement, TextElement> change, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(change);
        var text = Find(page, textId);
        var changed = change(text);
        return changed == text ? page : page.ReplaceElement(changed with { Id = text.Id }) with { UpdatedAt = now };
    }

    /// <summary>Metin kutusunu yeni konuma taşır; kutu tuvalin içinde tutulur.</summary>
    /// <param name="page">Sayfa.</param>
    /// <param name="textId">Metin.</param>
    /// <param name="x">Yeni sol kenar (mm).</param>
    /// <param name="y">Yeni üst kenar (mm).</param>
    /// <param name="canvas">Tuval (taşma payı dahil, mm).</param>
    /// <param name="now">Zaman damgası.</param>
    public static AlbumPage Move(AlbumPage page, string textId, double x, double y, RectMm canvas, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        var text = Find(page, textId);
        var b = text.Bounds;
        var nx = Math.Clamp(x, canvas.X, Math.Max(canvas.X, canvas.Right - b.Width));
        var ny = Math.Clamp(y, canvas.Y, Math.Max(canvas.Y, canvas.Bottom - b.Height));
        return Math.Abs(nx - b.X) < 1e-6 && Math.Abs(ny - b.Y) < 1e-6
            ? page
            : page.ReplaceElement(text with { Bounds = new RectMm(nx, ny, b.Width, b.Height) }) with { UpdatedAt = now };
    }

    /// <summary>Metin kutusunun genişliğini değiştirir; kutu ortası korunur, yükseklik yazıya göre yeniden hesaplanır.</summary>
    public static AlbumPage Resize(AlbumPage page, string textId, double width, RectMm canvas, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        var text = Find(page, textId);
        var b = text.Bounds;
        var w = Math.Clamp(width, 10, canvas.Width);
        var x = Math.Clamp(b.X + ((b.Width - w) / 2), canvas.X, canvas.Right - w);
        return Math.Abs(w - b.Width) < 1e-6
            ? page
            : page.ReplaceElement(text with { Bounds = new RectMm(x, b.Y, w, b.Height) }) with { UpdatedAt = now };
    }

    /// <summary>Metni yazının satır sayısına göre yeniden yükseklendirir (üst kenar sabit).</summary>
    public static TextElement FitHeight(TextElement text, int lines)
    {
        ArgumentNullException.ThrowIfNull(text);
        var height = Math.Max(1, lines) * text.Font.Size * TextPreset.MmPerPoint * (text.LineHeight ?? 1.25);
        return Math.Abs(height - text.Bounds.Height) < 1e-6
            ? text
            : text with { Bounds = new RectMm(text.Bounds.X, text.Bounds.Y, text.Bounds.Width, height) };
    }

    public static AlbumPage Remove(AlbumPage page, string textId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        Find(page, textId);
        return page.RemoveElement(textId) with { UpdatedAt = now };
    }

    private static TextElement Find(AlbumPage page, string textId) =>
        page.FindElement(textId) as TextElement ?? throw new KeyNotFoundException("Metin bulunamadı: " + textId);
}
