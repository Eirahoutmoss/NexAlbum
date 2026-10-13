using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.Services;

/// <summary>
/// Önizleme görüntülerini küçültülmüş olarak çözer ve önbelleğe alır. EXIF yönünü uygular
/// (WPF bunu kendiliğinden yapmaz). Önbellek boyutu sınırlıdır; eski girdiler atılır.
/// </summary>
public sealed class ImageLoader
{
    /// <summary>Galeri ve sayfa şeridi için küçük resim genişliği (piksel).</summary>
    public const int ThumbnailWidth = 320;

    /// <summary>Tuvaldeki seçili sayfa için önizleme genişliği (piksel).</summary>
    public const int DetailWidth = 1600;

    private readonly Lock gate = new();
    private readonly Dictionary<(string Path, int Width, int Orientation), LinkedListNode<CacheEntry>> map = [];
    private readonly LinkedList<CacheEntry> order = new();
    private readonly int capacity;

    public ImageLoader(int capacity = 600)
    {
        this.capacity = capacity;
    }

    public static ImageLoader Shared { get; } = new();

    /// <summary>Önbellekte varsa görüntüyü hemen döndürür (diske gitmez); yeniden kurulan görünüm modelleri titremesin diye.</summary>
    public ImageSource? TryGetCached(PhotoAsset asset, int decodeWidth)
    {
        ArgumentNullException.ThrowIfNull(asset);
        lock (gate)
        {
            return map.TryGetValue(Key(asset, decodeWidth), out var node) ? node.Value.Image : null;
        }
    }

    /// <summary>Dosya yerinde değiştiğinde (harici düzenleyici kaydetti) o yolun tüm önbellek girdilerini atar.</summary>
    public void Invalidate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        lock (gate)
        {
            foreach (var key in map.Keys.Where(k => string.Equals(k.Path, path, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                order.Remove(map[key]);
                map.Remove(key);
            }
        }
    }

    /// <summary>Görüntüyü yükler; başarısızsa null döner. Herhangi bir iş parçacığından çağrılabilir (sonuç dondurulmuştur).</summary>
    public ImageSource? Load(PhotoAsset asset, int decodeWidth)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var key = Key(asset, decodeWidth);
        lock (gate)
        {
            if (map.TryGetValue(key, out var node))
            {
                order.Remove(node);
                order.AddFirst(node);
                return node.Value.Image;
            }
        }

        var image = Decode(asset, decodeWidth);
        if (image is null)
        {
            return null;
        }

        lock (gate)
        {
            if (!map.ContainsKey(key))
            {
                map[key] = order.AddFirst(new CacheEntry(key, image));
                while (order.Count > capacity)
                {
                    var last = order.Last!;
                    order.RemoveLast();
                    map.Remove(last.Value.Key);
                }
            }
        }

        return image;
    }

    private static BitmapSource? Decode(PhotoAsset asset, int decodeWidth)
    {
        if (asset.SourceType != AssetSourceType.Reference || !File.Exists(asset.SourcePath))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.UriSource = new Uri(asset.SourcePath, UriKind.Absolute);

            // DecodePixelWidth dosyadaki ham genişliğe uygulanır; 90° dönük fotoğrafta bu görünen yüksekliktir.
            bitmap.DecodePixelWidth = Math.Min(decodeWidth, asset.PixelWidth);
            bitmap.Rotation = asset.ExifOrientation switch
            {
                3 or 4 => Rotation.Rotate180,
                5 or 6 => Rotation.Rotate90,
                7 or 8 => Rotation.Rotate270,
                _ => Rotation.Rotate0,
            };
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Önbellek anahtarı yönü de içerir: kullanıcı fotoğrafı döndürünce eski görüntü kullanılmaz.</summary>
    private static (string Path, int Width, int Orientation) Key(PhotoAsset asset, int width) => (asset.SourcePath, width, asset.ExifOrientation ?? 1);

    private sealed record CacheEntry((string Path, int Width, int Orientation) Key, ImageSource Image);
}
