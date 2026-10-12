using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Templates;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Json;
using NexAlbum.Persistence.Json.Dtos;

namespace NexAlbum.Persistence.Packaging;

/// <summary>
/// .albumproj okuyucu (Karar 09 §16). Hatalar <see cref="ProjectFormatException"/> olarak, kullanıcıya gösterilebilir biçimde döner.
/// </summary>
public static class ProjectPackageReader
{
    /// <summary>Dosyadan proje açar. Göç gerekiyorsa (seçeneğe göre) önce yedek alır; orijinal dosya değiştirilmez.</summary>
    public static LoadedProject Load(string path, LoadOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        options ??= LoadOptions.Default;

        LoadedProject loaded;
        using (var stream = OpenFile(path))
        {
            loaded = Load(stream, options);
        }

        if (loaded.Migrated && options.BackupBeforeMigration)
        {
            var backup = ProjectFiles.MigrationBackupPath(path, loaded.OriginalSchemaVersion);
            if (!File.Exists(backup))
            {
                File.Copy(path, backup);
            }

            loaded = loaded with { MigrationBackup = backup };
        }

        return loaded;
    }

    /// <summary>Akıştan proje açar.</summary>
    public static LoadedProject Load(Stream stream, LoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= LoadOptions.Default;

        ZipArchive zip;
        try
        {
            zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException ex)
        {
            throw new ProjectFormatException(ProjectLoadError.CorruptArchive, "Dosya açılamıyor: geçerli bir proje paketi değil ya da bozuk.", ex);
        }

        using (zip)
        {
            try
            {
                return Read(zip, options);
            }
            catch (InvalidDataException ex)
            {
                throw new ProjectFormatException(ProjectLoadError.CorruptArchive, "Dosya açılamıyor: paket bozuk.", ex);
            }
        }
    }

    /// <summary>Paketteki bir ikili dosyayı (küçük resim, gömülü orijinal) hedef akışa çıkarır.</summary>
    public static void ExtractEntry(string packagePath, string entryPath, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var name = PackagePaths.Normalize(entryPath);
        using var stream = OpenFile(packagePath);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = zip.GetEntry(name) ?? throw new FileNotFoundException($"Pakette bulunamadı: {name}", name);
        using var source = entry.Open();
        source.CopyTo(destination);
    }

    private static LoadedProject Read(ZipArchive zip, LoadOptions options)
    {
        var entries = ValidateEntries(zip, options);

        // 1–2. manifest → schemaVersion
        var manifestDto = ReadJson<ManifestDto>(zip, ProjectFormat.ManifestEntry, options, ProjectLoadError.NotAProject)
            ?? throw new ProjectFormatException(ProjectLoadError.NotAProject, "Geçersiz proje: manifest.json yok.");
        if (manifestDto.Format != ProjectFormat.FormatName)
        {
            throw new ProjectFormatException(ProjectLoadError.NotAProject, "Geçersiz proje: bu bir NexAlbum projesi değil.");
        }

        var schemaVersion = manifestDto.SchemaVersion
            ?? throw new ProjectFormatException(ProjectLoadError.NotAProject, "Geçersiz proje: şema sürümü yok.");
        EnsureNotTooNew(schemaVersion);

        // 3. project.json (ham JSON) → gerekirse göç
        var projectNode = ReadNode(zip, ProjectFormat.ProjectEntry, options)
            ?? throw new ProjectFormatException(ProjectLoadError.NotAProject, "Geçersiz proje: project.json yok.");
        var projectVersion = ReadSchemaVersion(projectNode) ?? schemaVersion;
        EnsureNotTooNew(projectVersion);
        if (projectVersion < ProjectFormat.CurrentSchemaVersion)
        {
            projectNode = options.Migrations.Run(projectNode, projectVersion, ProjectFormat.CurrentSchemaVersion);
        }

        // 4. bütünlük
        IReadOnlyList<string> mismatches = options.VerifyChecksums ? VerifyChecksums(zip, manifestDto, options) : [];

        // 5–7. veriler
        var project = Deserialize(() => ProjectMapper.FromDto(
            projectNode.Deserialize<ProjectDto>(JsonDefaults.Options) ?? throw new FormatException("project.json boş.")));

        var profiles = Deserialize(() => ReadProfiles(zip, options));
        var templates = Deserialize(() => ReadTemplates(zip, options));

        var manifest = new PackageManifest(
            manifestDto.FormatVersion ?? ProjectFormat.FormatVersion,
            ProjectFormat.CurrentSchemaVersion,
            manifestDto.CreatedAt ?? project.UpdatedAt,
            manifestDto.AppVersion ?? project.AppVersion,
            manifestDto.ProjectId ?? project.Id,
            manifestDto.ProjectName ?? project.Name,
            manifestDto.Portable ?? false);

        return new LoadedProject
        {
            Project = project,
            Manifest = manifest,
            Profiles = profiles,
            Templates = templates,
            Entries = entries,
            ChecksumMismatches = mismatches,
            OriginalSchemaVersion = projectVersion,
        };
    }

    private static List<AlbumProfile> ReadProfiles(ZipArchive zip, LoadOptions options)
    {
        var file = ReadJson<ProfilesFileDto>(zip, ProjectFormat.ProfilesEntry, options, ProjectLoadError.InvalidData);
        return [.. (file?.Profiles ?? []).Select((p, i) => ProfileMapper.FromDto(p, $"profiles[{i}]"))];
    }

    private static List<TemplateDefinition> ReadTemplates(ZipArchive zip, LoadOptions options)
    {
        var file = ReadJson<TemplatesFileDto>(zip, ProjectFormat.TemplatesEntry, options, ProjectLoadError.InvalidData);
        return [.. (file?.Templates ?? []).Select((t, i) => TemplateMapper.FromDto(t, $"templates[{i}]"))];
    }

    private static FileStream OpenFile(string path)
    {
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (FileNotFoundException ex)
        {
            throw new ProjectFormatException(ProjectLoadError.CorruptArchive, $"Dosya bulunamadı: {path}", ex);
        }
    }

    private static void EnsureNotTooNew(int version)
    {
        if (version > ProjectFormat.CurrentSchemaVersion)
        {
            throw new ProjectFormatException(
                ProjectLoadError.TooNew,
                $"Bu proje daha yeni bir NexAlbum sürümüyle kaydedilmiş (şema v{version}). Lütfen uygulamayı güncelleyin.");
        }
    }

    /// <summary>Yol gezinme, zip bombası ve toplam boyut denetimi (Karar 09 §21.1).</summary>
    private static IReadOnlyList<string> ValidateEntries(ZipArchive zip, LoadOptions options)
    {
        var names = new List<string>(zip.Entries.Count);
        long total = 0;
        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.EndsWith('/'))
            {
                continue;
            }

            names.Add(PackagePaths.Normalize(entry.FullName));
            total += entry.Length;
            if (total > options.MaxTotalBytes)
            {
                throw new ProjectFormatException(ProjectLoadError.UnsafeArchive, "Paket izin verilen boyutu aşıyor.");
            }

            const long smallEntry = 1024 * 1024;
            if (entry.Length > smallEntry && entry.CompressedLength > 0
                && (double)entry.Length / entry.CompressedLength > options.MaxCompressionRatio)
            {
                throw new ProjectFormatException(ProjectLoadError.UnsafeArchive, $"Şüpheli sıkıştırma oranı: {entry.FullName}");
            }
        }

        return names;
    }

    private static JsonNode? ReadNode(ZipArchive zip, string name, LoadOptions options)
    {
        var bytes = ReadEntry(zip, name, options);
        if (bytes is null)
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(bytes, documentOptions: JsonDefaults.DocumentOptions)
                ?? throw new ProjectFormatException(ProjectLoadError.InvalidData, $"{name} boş.");
        }
        catch (JsonException ex)
        {
            throw new ProjectFormatException(ProjectLoadError.InvalidData, $"{name} okunamadı: {ex.Message}", ex);
        }
    }

    private static T? ReadJson<T>(ZipArchive zip, string name, LoadOptions options, ProjectLoadError error)
        where T : class
    {
        var bytes = ReadEntry(zip, name, options);
        if (bytes is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, JsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            throw new ProjectFormatException(error, $"{name} okunamadı: {ex.Message}", ex);
        }
    }

    private static byte[]? ReadEntry(ZipArchive zip, string name, LoadOptions options)
    {
        var entry = zip.GetEntry(name);
        if (entry is null)
        {
            return null;
        }

        if (entry.Length > options.MaxJsonBytes)
        {
            throw new ProjectFormatException(ProjectLoadError.UnsafeArchive, $"{name} izin verilen boyutu aşıyor.");
        }

        using var source = entry.Open();
        using var buffer = new MemoryStream((int)entry.Length);
        source.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static int? ReadSchemaVersion(JsonNode node)
    {
        try
        {
            return node["schemaVersion"]?.GetValue<int>();
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            throw new ProjectFormatException(ProjectLoadError.InvalidData, "project.json: schemaVersion geçersiz.", ex);
        }
    }

    private static List<string> VerifyChecksums(ZipArchive zip, ManifestDto manifest, LoadOptions options)
    {
        var name = manifest.ChecksumsFile ?? ProjectFormat.ChecksumsEntry;
        var dto = ReadJson<ChecksumsDto>(zip, PackagePaths.Normalize(name), options, ProjectLoadError.InvalidData);
        var mismatches = new List<string>();
        if (dto?.Files is null)
        {
            mismatches.Add(name);
            return mismatches;
        }

        if (dto.Algorithm is not null && !string.Equals(dto.Algorithm, ProjectFormat.ChecksumAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            throw new ProjectFormatException(ProjectLoadError.InvalidData, $"Desteklenmeyen özet algoritması: {dto.Algorithm}");
        }

        foreach (var (file, expected) in dto.Files)
        {
            var entry = PackagePaths.IsSafe(file) ? zip.GetEntry(PackagePaths.Normalize(file)) : null;
            if (entry is null)
            {
                mismatches.Add(file);
                continue;
            }

            using var s = entry.Open();
            if (!string.Equals(Checksums.Sha256(s), expected, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add(file);
            }
        }

        return mismatches;
    }

    private static T Deserialize<T>(Func<T> map)
    {
        try
        {
            return map();
        }
        catch (Exception ex) when (ex is FormatException or JsonException or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            throw new ProjectFormatException(ProjectLoadError.InvalidData, "Proje verisi okunamadı: " + ex.Message, ex);
        }
    }
}
