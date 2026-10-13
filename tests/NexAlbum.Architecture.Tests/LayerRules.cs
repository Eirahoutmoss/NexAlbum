using FluentAssertions;

namespace NexAlbum.Architecture.Tests;

/// <summary>
/// Karar 08 §13.2 (Revizyon 2, 5) — katmanlar arası izin verilen bağımlılıklar.
/// </summary>
internal static class LayerRules
{
    public static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["NexAlbum.Core"] = [],
        ["NexAlbum.Rendering"] = ["NexAlbum.Core"],
        ["NexAlbum.Export"] = ["NexAlbum.Core", "NexAlbum.Rendering"],
        ["NexAlbum.Assets"] = ["NexAlbum.Core"],
        ["NexAlbum.Persistence"] = ["NexAlbum.Core"],
        ["NexAlbum.Integrations"] = ["NexAlbum.Core", "NexAlbum.Assets"],
        ["NexAlbum.Video"] = ["NexAlbum.Core", "NexAlbum.Rendering"],
        ["NexAlbum.UI"] = ["NexAlbum.Core", "NexAlbum.Rendering", "NexAlbum.Export", "NexAlbum.Assets", "NexAlbum.Persistence", "NexAlbum.Integrations", "NexAlbum.Video"],
    };

    /// <summary>Core'un hiçbir koşulda bağlanamayacağı teknoloji derlemeleri.</summary>
    public static readonly string[] ForbiddenInCore =
    [
        "PresentationFramework", "PresentationCore", "WindowsBase", "System.Xaml",
        "SkiaSharp", "PdfSharp", "Microsoft.Data.Sqlite", "Serilog",
    ];

    /// <summary>Bir katmanın bağımlılıklarının izin listesinde olduğunu doğrular.</summary>
    public static void AssertOnlyAllowed(string layer, IReadOnlyCollection<string> references)
    {
        var allowed = Allowed[layer];
        references.Except(allowed, StringComparer.Ordinal).Should().BeEmpty(
            $"{layer} yalnızca Karar 08 §13.2'de izin verilen katmanlara bağlanabilir: [{string.Join(", ", allowed)}]");
    }
}
