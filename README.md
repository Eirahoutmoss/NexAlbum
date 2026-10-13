# NexAlbum

Fotoğrafçılar için **ücretsiz ve açık kaynak** düğün/albüm tasarım stüdyosu. Windows öncelikli, yerel çalışır; hesap, bulut ve abonelik yok.

> Durum: erken geliştirme (v0.1). Çalışan akış: profil seç → fotoğraf ekle → otomatik oluştur → profili tamamla → baskı PDF'i.

![NexAlbum — Gece teması](https://raw.githubusercontent.com/Eirahoutmoss/NexAlbum/ekran/main/02-calisma-gece.png)

<sub>Gece (varsayılan) ve Fildişi temaları · Ekran görüntüleri her değişiklikte CI tarafından üretilir: [`ekran/main`](https://github.com/Eirahoutmoss/NexAlbum/tree/ekran/main) dalı.</sub>

## Neler var?

- **Baskı profili** (Karar 03): değerler kullanıcıdan gelir, hiçbiri tahmin edilmez; eksik değer ön kontrolde hata verir.
- **Fotoğraf içe aktarma**: JPEG, PNG, TIFF, WebP, BMP, GIF; EXIF yönü ve çekim zamanı.
- **Otomatik yerleşim** (Karar 10): Hızlı / Dengeli / Kontrollü; tohumlu ve deterministik; kilitli sayfalar korunur; fotoğraf asla esnetilmez.
- **.albumproj** (Karar 09): açık ZIP + JSON, atomik kaydetme, bütünlük denetimi, şema göçü.
- **Ön kontrol** (Karar 11 §9): DPI, eksik dosya, boş yuva, güvenli alan, cilt payı, font.
- **Baskı PDF'i** (Karar 11 §3): taşma payı, kesim işaretleri, TrimBox/BleedBox; JPEG'ler yeniden sıkıştırılmaz.
- **Arayüz** (Karar 12 Rev. 1, Karar 17): Gece/Fildişi temaları, komut paleti (Ctrl+K), galeri ızgarası ve süzgeçleri,
  tuval yakınlaştırma (%100 = gerçek baskı boyutu), başlangıç ekranı ve son projeler. Inter ve Lucide gömülüdür.

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
| `tests/` | Testler (UI testleri yalnızca Windows'ta çalışır) |
| `src/NexAlbum.UI/Themes` | Renk paletleri (Gece, Fildişi), denetim stilleri, ikonlar, logo |
| `docs/kararlar/` | Karar belgeleri — önce `00_giris_ve_indeks_karari.md` |
| `docs/marka/`, `tools/` | Logo kaynakları; simge/açılış ekranı ve ikon üretim betikleri |

## Lisans

GPL-3.0-or-later — bkz. [LICENSE](LICENSE). Gömülü yazı tipi ve ikonlar: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
