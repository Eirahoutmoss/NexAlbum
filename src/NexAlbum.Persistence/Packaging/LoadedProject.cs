using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.Persistence.Format;

namespace NexAlbum.Persistence.Packaging;

/// <summary>Paket bilgileri (manifest.json).</summary>
public sealed record PackageManifest(
    string FormatVersion,
    int SchemaVersion,
    DateTimeOffset CreatedAt,
    string AppVersion,
    Guid ProjectId,
    string ProjectName,
    bool Portable);

/// <summary>Açılan projenin sonucu.</summary>
public sealed record LoadedProject
{
    public required AlbumProject Project { get; init; }

    public required PackageManifest Manifest { get; init; }

    public IReadOnlyList<TemplateDefinition> Templates { get; init; } = [];

    /// <summary>profiles.json içindeki profiller.</summary>
    public IReadOnlyList<AlbumProfile> Profiles { get; init; } = [];

    /// <summary>Paketteki tüm girdi yolları.</summary>
    public IReadOnlyList<string> Entries { get; init; } = [];

    /// <summary>Özeti uyuşmayan ya da kaybolan dosyalar. Boş değilse kullanıcı uyarılmalı (Karar 09 §16.2).</summary>
    public IReadOnlyList<string> ChecksumMismatches { get; init; } = [];

    /// <summary>Dosya eski bir şema sürümündeydi ve bellekte yükseltildi.</summary>
    public bool Migrated => OriginalSchemaVersion < ProjectFormat.CurrentSchemaVersion;

    public int OriginalSchemaVersion { get; init; }

    /// <summary>Göç öncesi alınan yedeğin yolu, varsa.</summary>
    public string? MigrationBackup { get; init; }
}
