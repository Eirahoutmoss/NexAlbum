# NexAlbum

Fotoğrafçılar için **ücretsiz ve açık kaynak** düğün/albüm tasarım stüdyosu. Windows öncelikli, yerel çalışır; hesap, bulut ve abonelik yok.

> Durum: geliştirme aşamasında (Aşama 1 — temel proje).

## Derleme

Gereksinim: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```powershell
dotnet build NexAlbum.slnx
dotnet test NexAlbum.slnx
dotnet run --project src/NexAlbum.UI
```

## Yapı

| Klasör | İçerik |
|---|---|
| `src/NexAlbum.Core` | Saf domain: albüm, sayfa, şablon, yerleşim motoru, preflight |
| `src/NexAlbum.Rendering` | SkiaSharp tuval, küçük resim |
| `src/NexAlbum.Export` | PDF, SVG, raster çıktı |
| `src/NexAlbum.Assets` | Fotoğraf, EXIF, önbellek |
| `src/NexAlbum.Persistence` | `.albumproj`, JSON, SQLite |
| `src/NexAlbum.UI` | WPF arayüz (MVVM) |
| `tests/` | Testler |
| `docs/kararlar/` | Karar belgeleri — önce `00_giris_ve_indeks_karari.md` |

## Lisans

GPL-3.0-or-later — bkz. [LICENSE](LICENSE).
