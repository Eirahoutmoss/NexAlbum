using System.IO.Compression;
using System.Text;
using System.Text.Json;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Json;
using NexAlbum.Persistence.Json.Dtos;

namespace NexAlbum.Persistence.Packaging;

/// <summary>
/// .albumproj yazıcı (Karar 09 §12, §15.1). Kaydetme atomiktir: yarım dosya asla "kaydedildi" görünmez.
/// </summary>
public static class ProjectPackageWriter
{
    private const string Readme = """
        Bu dosya bir NexAlbum projesidir.
        - Düzenlemek için: NexAlbum uygulamasını açın ve bu dosyayı seçin.
        - Elle incelemek için: ZIP olarak açın; project.json okunabilir JSON'dur.
        - Daha fazla bilgi: https://github.com/Eirahoutmoss/NexAlbum
        """;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Projeyi atomik olarak kaydeder: geçici dosyaya yazar, sonra hedefin yerine geçirir.
    /// Hata olursa mevcut dosya olduğu gibi kalır.
    /// </summary>
    public static void Save(string path, AlbumProject project, SaveOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);
        options ??= new SaveOptions();

        var full = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(full);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var temp = ProjectFiles.TempPath(full);
        try
        {
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Write(stream, project, options);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(full))
            {
                var backup = ProjectFiles.BackupPath(full);
                File.Replace(temp, full, backup, ignoreMetadataErrors: true);
                File.Delete(backup);
            }
            else
            {
                File.Move(temp, full);
            }
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    /// <summary>Paketi verilen akışa yazar (testler ve otomatik kayıt için).</summary>
    public static void Write(Stream destination, AlbumProject project, SaveOptions options)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);

        var now = options.Clock.GetUtcNow();
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var written = new HashSet<string>(StringComparer.Ordinal);

        using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Utf8NoBom);

        var projectDto = ProjectMapper.ToDto(project with { AppVersion = options.AppVersion });
        AddJson(zip, ProjectFormat.ProjectEntry, projectDto, hashes, written);
        AddJson(zip, ProjectFormat.ProfilesEntry, new ProfilesFileDto { SchemaVersion = 1, Profiles = [ProfileMapper.ToDto(project.Profile)] }, hashes, written);
        AddJson(zip, ProjectFormat.TemplatesEntry, new TemplatesFileDto { SchemaVersion = 1, Templates = [.. options.Templates.Select(TemplateMapper.ToDto)] }, hashes, written);

        foreach (var file in options.Files)
        {
            var name = PackagePaths.Normalize(file.Path);
            if (IsReserved(name) || !written.Add(name))
            {
                throw new ArgumentException($"Paket yolu kullanılamaz ya da yinelendi: {name}", nameof(options));
            }

            var entry = zip.CreateEntry(name, IsCompressible(name) ? CompressionLevel.Optimal : CompressionLevel.NoCompression);
            using var source = file.Open();
            using var target = entry.Open();
            using var hashing = new HashingStream(target);
            source.CopyTo(hashing);
            hashes[name] = hashing.Finish();
        }

        AddText(zip, ProjectFormat.ReadmeEntry, Readme.ReplaceLineEndings("\r\n"), hashes);

        var checksums = new ChecksumsDto { Algorithm = ProjectFormat.ChecksumAlgorithm, Files = new Dictionary<string, string>(hashes, StringComparer.Ordinal) };
        AddJson(zip, ProjectFormat.ChecksumsEntry, checksums, hashes: null, written);

        var manifest = new ManifestDto
        {
            Format = ProjectFormat.FormatName,
            FormatVersion = ProjectFormat.FormatVersion,
            SchemaVersion = ProjectFormat.CurrentSchemaVersion,
            CreatedAt = now,
            AppVersion = options.AppVersion,
            ProjectId = project.Id,
            ProjectName = project.Name,
            Portable = options.Portable,
            ChecksumsFile = ProjectFormat.ChecksumsEntry,
        };
        AddJson(zip, ProjectFormat.ManifestEntry, manifest, hashes: null, written);
    }

    private static bool IsReserved(string name) => name is ProjectFormat.ManifestEntry or ProjectFormat.ProjectEntry
        or ProjectFormat.ProfilesEntry or ProjectFormat.TemplatesEntry or ProjectFormat.ChecksumsEntry or ProjectFormat.ReadmeEntry;

    /// <summary>JPEG gibi zaten sıkıştırılmış dosyaları yeniden sıkıştırmak boşa zaman harcar.</summary>
    private static bool IsCompressible(string name)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".heic" or ".tif" or ".tiff" or ".zip");
    }

    private static void AddJson<T>(ZipArchive zip, string name, T value, SortedDictionary<string, string>? hashes, HashSet<string> written)
    {
        written.Add(name);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonDefaults.Options);
        AddBytes(zip, name, bytes, hashes);
    }

    private static void AddText(ZipArchive zip, string name, string text, SortedDictionary<string, string> hashes) =>
        AddBytes(zip, name, Utf8NoBom.GetBytes(text), hashes);

    private static void AddBytes(ZipArchive zip, string name, byte[] bytes, SortedDictionary<string, string>? hashes)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using (var stream = entry.Open())
        {
            stream.Write(bytes);
        }

        if (hashes is not null)
        {
            hashes[name] = Checksums.Sha256(bytes);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Geçici dosya silinemezse bir sonraki kayıtta üzerine yazılır.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
