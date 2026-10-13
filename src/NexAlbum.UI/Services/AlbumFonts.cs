using System.IO;
using System.Windows;
using System.Windows.Media;

namespace NexAlbum.UI.Services;

/// <summary>Gömülü albüm yazı tipi: aile adı, türü ve stil başına dosya.</summary>
/// <param name="Family">Aile adı (proje dosyasına yazılan ad).</param>
/// <param name="Kind">"El yazısı", "Serif" ya da "Sans".</param>
/// <param name="Regular">Düz dosya.</param>
/// <param name="Bold">Kalın dosya; yoksa kalınlık taklit edilir.</param>
/// <param name="Italic">Eğik dosya; yoksa eğiklik taklit edilir.</param>
/// <param name="Folder">Dosyaların klasörü (pack URI yolu).</param>
public sealed record AlbumFont(string Family, string Kind, string Regular, string? Bold = null, string? Italic = null, string Folder = AlbumFonts.Folder)
{
    /// <summary>Ekranda kullanılacak WPF yazı tipi (uygulama kaynağından; kurulum gerekmez).</summary>
    public FontFamily WpfFamily { get; } = new(new Uri("pack://application:,,,/"), Folder + "#" + Family);
}

/// <summary>
/// Albüm sayfalarındaki yazılar için uygulamaya gömülü yazı tipleri (SIL OFL 1.1; Karar 17 §11, Karar 08 Rev. 4).
/// Ekran WPF'in kaynak yazı tipini, PDF aynı dosyayı gömer; kurulu olmayan bilgisayarda da sonuç aynıdır.
/// </summary>
public static class AlbumFonts
{
    static AlbumFonts()
    {
        // "pack:" şeması uygulama başlamadan (ör. testlerde) de tanınsın.
        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
    }

    /// <summary>Gömülü dosyaların klasörü (pack URI).</summary>
    public const string Folder = "/NexAlbum;component/Assets/AlbumFonts/";

    /// <summary>Arayüz yazı tipinin (Inter) klasörü; albüm yazısı olarak da kullanılabilir.</summary>
    public const string InterFolder = "/NexAlbum;component/Assets/Fonts/";

    /// <summary>PDF çözümleyicisinde gömülü dosya yüzlerinin öneki.</summary>
    public const string FacePrefix = "nexalbum:";

    public static IReadOnlyList<AlbumFont> All { get; } =
    [
        new("Great Vibes", "El yazısı", "GreatVibes_400Regular.ttf"),
        new("Pinyon Script", "El yazısı", "PinyonScript_400Regular.ttf"),
        new("Parisienne", "El yazısı", "Parisienne_400Regular.ttf"),
        new("Allura", "El yazısı", "Allura_400Regular.ttf"),
        new("Cormorant Garamond", "Serif", "CormorantGaramond_400Regular.ttf", "CormorantGaramond_700Bold.ttf", "CormorantGaramond_400Regular_Italic.ttf"),
        new("Playfair Display", "Serif", "PlayfairDisplay_400Regular.ttf", "PlayfairDisplay_700Bold.ttf", "PlayfairDisplay_400Regular_Italic.ttf"),
        new("Montserrat", "Sans", "Montserrat_400Regular.ttf", "Montserrat_700Bold.ttf"),
        new("Inter", "Sans", "Inter-Regular.ttf", "Inter-Bold.ttf", Folder: InterFolder),
    ];

    public static AlbumFont? Find(string family) =>
        All.FirstOrDefault(f => string.Equals(f.Family, family, StringComparison.OrdinalIgnoreCase));

    /// <summary>Ekranda kullanılacak yazı tipi: gömülüyse uygulama kaynağından, değilse sistemden.</summary>
    public static FontFamily ToWpf(string family) => Find(family)?.WpfFamily ?? new FontFamily(family);

    /// <summary>Gömülü ya da sistemde kurulu mu? (Ön kontrol: eksik yazı tipi uyarısı.)</summary>
    public static bool IsAvailable(string family) => Find(family) is not null || FontCatalog.IsSystemFont(family);

    /// <summary>
    /// PDF için yüz seçimi: istenen stil dosyası varsa o, yoksa düz dosya + taklit (kalın/eğik). Gömülü değilse null.
    /// </summary>
    public static (string Face, bool SimulateBold, bool SimulateItalic)? Resolve(string family, bool bold, bool italic)
    {
        if (Find(family) is not { } font)
        {
            return null;
        }

        var face = FacePrefix + font.Folder;
        if (italic && font.Italic is { } i)
        {
            return (face + i, bold, false);
        }

        if (bold && font.Bold is { } b)
        {
            return (face + b, false, italic);
        }

        return (face + font.Regular, bold, italic);
    }

    /// <summary>Gömülü dosyanın baytları (PDF'e gömmek için).</summary>
    public static byte[]? Load(string face)
    {
        if (!face.StartsWith(FacePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var info = Application.GetResourceStream(new Uri("pack://application:,,," + face[FacePrefix.Length..]));
        if (info is null)
        {
            return null;
        }

        using var stream = info.Stream;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
