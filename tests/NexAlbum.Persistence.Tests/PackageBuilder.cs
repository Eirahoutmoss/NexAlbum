using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Packaging;

namespace NexAlbum.Persistence.Tests;

/// <summary>Geçerli bir paketi alıp içini değiştirerek bozuk/kötü niyetli paketler üretir.</summary>
internal static class PackageBuilder
{
    public static byte[] Valid()
    {
        using var ms = new MemoryStream();
        ProjectPackageWriter.Write(ms, Samples.FullProject(), new SaveOptions { AppVersion = "0.1.0", Clock = new FixedClock(Samples.Now) });
        return ms.ToArray();
    }

    /// <summary>Bir girdinin JSON içeriğini değiştirir (checksums güncellenmez → kurcalama gibi görünür).</summary>
    public static byte[] EditJson(byte[] package, string entry, Action<JsonNode> edit)
    {
        return Edit(package, zip =>
        {
            var e = zip.GetEntry(entry)!;
            JsonNode node;
            using (var s = e.Open())
            {
                node = JsonNode.Parse(s)!;
            }

            edit(node);
            e.Delete();
            Write(zip, entry, node.ToJsonString());
        });
    }

    public static byte[] Edit(byte[] package, Action<ZipArchive> edit)
    {
        using var ms = new MemoryStream();
        ms.Write(package);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            edit(zip);
        }

        return ms.ToArray();
    }

    public static void Write(ZipArchive zip, string entry, string content, CompressionLevel level = CompressionLevel.Optimal)
    {
        using var s = zip.CreateEntry(entry, level).Open();
        s.Write(Encoding.UTF8.GetBytes(content));
    }

    public static ProjectFormatException LoadFails(byte[] package)
    {
        using var ms = new MemoryStream(package);
        try
        {
            ProjectPackageReader.Load(ms);
        }
        catch (ProjectFormatException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Yükleme başarısız olmalıydı.");
    }

    public static LoadedProject Load(byte[] package, LoadOptions? options = null)
    {
        using var ms = new MemoryStream(package);
        return ProjectPackageReader.Load(ms, options);
    }
}
