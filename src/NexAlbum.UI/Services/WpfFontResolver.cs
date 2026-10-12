using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using PdfSharp.Fonts;

namespace NexAlbum.UI.Services;

/// <summary>
/// PDF çıktısı için yazı tipi çözümleyici: WPF'in font motoruyla sistemdeki font dosyasını bulur ve PDF'e gömer
/// (Karar 11 §3.7). Font bulunamazsa WPF'in yedek fontu kullanılır; ön kontrol eksik fontu ayrıca uyarır.
/// </summary>
public sealed class WpfFontResolver : IFontResolver
{
    private readonly ConcurrentDictionary<string, byte[]> files = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Uygulama açılışında bir kez çağrılır.</summary>
    public static void Register()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new WpfFontResolver();
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        var typeface = new Typeface(
            new FontFamily(familyName),
            italic ? FontStyles.Italic : FontStyles.Normal,
            bold ? FontWeights.Bold : FontWeights.Normal,
            FontStretches.Normal);

        if (!typeface.TryGetGlyphTypeface(out var glyph) || !glyph.FontUri.IsFile)
        {
            return null;
        }

        var path = glyph.FontUri.LocalPath;
        var simulateBold = glyph.StyleSimulations.HasFlag(StyleSimulations.BoldSimulation);
        var simulateItalic = glyph.StyleSimulations.HasFlag(StyleSimulations.ItalicSimulation);
        return new FontResolverInfo(path, simulateBold, simulateItalic);
    }

    public byte[]? GetFont(string faceName) =>
        files.GetOrAdd(faceName, path => File.Exists(path) ? File.ReadAllBytes(path) : []) is { Length: > 0 } bytes ? bytes : null;
}
