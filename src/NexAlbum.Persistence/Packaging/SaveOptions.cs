using NexAlbum.Core.Templates;

namespace NexAlbum.Persistence.Packaging;

/// <summary>Kaydetme seçenekleri.</summary>
public sealed record SaveOptions
{
    /// <summary>Kaydeden uygulama sürümü (manifest.appVersion).</summary>
    public string AppVersion { get; init; } = "0.0.0";

    /// <summary>Taşınabilir mod: orijinaller pakete gömülüdür (Karar 09 §13.2).</summary>
    public bool Portable { get; init; }

    /// <summary>Projede kullanılan şablonlar (templates.json).</summary>
    public IReadOnlyList<TemplateDefinition> Templates { get; init; } = [];

    /// <summary>Küçük resim, önizleme, gömülü orijinal ve fontlar.</summary>
    public IReadOnlyList<PackageFile> Files { get; init; } = [];

    /// <summary>Saat kaynağı (testlerde sabitlenir).</summary>
    public TimeProvider Clock { get; init; } = TimeProvider.System;
}
