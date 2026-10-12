namespace NexAlbum.Persistence.Packaging;

/// <summary>
/// Pakete yazılacak ikili dosya (küçük resim, gömülü orijinal, font). İçerik yazma anında açılır; belleğe yığılmaz.
/// </summary>
/// <param name="Path">Paket içi göreceli yol, ör. "assets/thumbnails/asset-001.jpg".</param>
/// <param name="Open">İçeriği okumak için akış açan işlev. Akışı yazıcı kapatır.</param>
public sealed record PackageFile(string Path, Func<Stream> Open)
{
    public static PackageFile FromBytes(string path, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new PackageFile(path, () => new MemoryStream(content, writable: false));
    }

    public static PackageFile FromDisk(string path, string sourceFile) =>
        new(path, () => File.OpenRead(sourceFile));
}
