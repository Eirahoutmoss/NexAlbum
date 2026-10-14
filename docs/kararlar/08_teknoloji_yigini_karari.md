Karar Dokümanı 08 — Teknoloji Yığını Kararı
Proje: NexAlbum
Karar no: 08
Konu: Programlama dili, UI framework, kütüphaneler, mimari desen ve dağıtım araçlarının seçimi
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 01 (Ücretsiz), 02 (Açık Kaynak), 03 (Kullanıcı Profilleri), 04 (Tek Kişi), 05 (Zaman), 06 (Bütçe), 07 (Rakip Özellikleri)

Revizyon 1 — 12/10/2026
Değişiklik: .NET 8 → .NET 10 (LTS); C# 12 → C# 14. Bu belgede geçen ".NET 8" ve "C# 12" ifadeleri .NET 10 / C# 14 olarak okunur.
Gerekçe: .NET 8 desteği 10 Kasım 2026'da sona eriyor; MVP (4-7 ay) desteksiz bir platformda yayımlanmamalı. .NET 10 LTS desteği Kasım 2028'e kadar sürer. Bu geçiş, §18 risk tablosunda zaten öngörülmüştü ("sonra .NET 10 LTS").
Ek: FluentAssertions 7.x sürümüne sabitlenir; 8.0 ve sonrası Apache 2.0 değil, ticari lisanslıdır (GPL uyumu için, Karar 02).
Değişmeyenler: WPF, SkiaSharp, PDFsharp, SQLite, Serilog, MVVM + Clean Architecture ve katman yapısı.

Revizyon 2 — 13/10/2026
Değişiklik: Yeni katman `NexAlbum.Integrations` (harici düzenleyiciler: Photoshop COM, komut satırı araçları, dosya izleme). Bağımlılık: Integrations → Core, Assets; UI → Integrations. Ayrıntı: Karar 20.
Gerekçe: Dış süreç ve COM erişimi Core'a ve Assets'e sızmasın; özellik isteğe bağlı kalsın. Yeni NuGet bağımlılığı yoktur.

Revizyon 3 — 13/10/2026
Değişiklik: Uygulamaya iki gömülü varlık eklendi (NuGet paketi değil, kaynak dosya): Inter yazı tipi (SIL OFL 1.1) ve Lucide ikonları (ISC). Ayrıntı: Karar 17.
Gerekçe: Görünüm kurulu yazı tiplerine ve Windows sürümüne (Segoe Fluent Icons yalnızca Windows 11) bağlı kalmasın. Lisans metinleri `THIRD-PARTY-NOTICES.md`'de.

Revizyon 5 — 13/10/2026
Değişiklik: Yeni katman `NexAlbum.Video` (Karar 21): Video → Core, Rendering; UI → Video. `NexAlbum.Rendering` SkiaSharp ile kare çizer (SkiaSharp zaten onaylı; Linux testleri için SkiaSharp.NativeAssets.Linux.NoDependencies, MIT). FFmpeg (GPL) uygulamaya gömülmez; kullanıcı onayıyla indirilen ya da sistemde kurulu harici araç olarak çağrılır (Karar 20 yöntemi).
Gerekçe: Video kodlama C# içinde yazılmaz; FFmpeg ayrı süreçtir, lisansı GPL-3.0-or-later ile uyumludur ve dağıtım yükü kullanıcı onayına bağlıdır.

Revizyon 4 — 13/10/2026
Değişiklik: Albüm sayfa metinleri için yedi gömülü yazı tipi eklendi (kaynak dosya, NuGet değil): Great Vibes, Pinyon Script, Parisienne, Allura, Cormorant Garamond, Playfair Display, Montserrat — hepsi SIL OFL 1.1. Ayrıntı: Karar 17 §11.
Gerekçe: Ekran ve PDF aynı dosyayı kullansın; albüm, yazı tipi kurulu olmayan bilgisayarda da aynı çıksın. OFL yazı tipleri GPL yazılımla birlikte dağıtılabilir.

Revizyon 6 — 14/10/2026
Değişiklik: §14 uygulandı. Yayın `.github/workflows/release.yml` ile `v*` etiketinden üretilir: `dotnet publish -r win-x64 --self-contained` + tek dosya (.NET çalışma zamanı, SkiaSharp yerel kütüphanesi ve tüm NuGet bağımlılıkları NexAlbum.exe içinde), taşınabilir ZIP ve kullanıcı başına Inno Setup kurulumu (`installer/NexAlbum.iss`; yönetici izni yok, Türkçe sihirbaz, `.albumproj` ilişkilendirmesi HKCU'da). Yerelde aynı adımlar: `tools/yayin/paketle.ps1`. FFmpeg pakete girmez (Rev. 5). Kod imzalama ve MSIX hâlâ ertelendi (Karar 06); §14.4'teki taşınabilir `./data/` ayar klasörü henüz uygulanmadı (ayarlar %LOCALAPPDATA%'da kalır).
Gerekçe: Kullanıcı .NET kurmadan çift tıklayıp çalıştırabilsin; yayın her seferinde aynı, denetlenebilir CI adımlarıyla üretilsin (SHA-256 özetleri yayına eklenir). Inno Setup ücretsizdir ve yalnızca derleme aracıdır, pakete girmez.

1. Karar Özeti
NexAlbum'in teknoloji yığını, Windows öncelikli, ücretsiz, açık kaynak (GPL uyumlu), tek geliştirici tarafından sürdürülebilir ve yüksek performanslı olacak şekilde seçilmiştir.

Katman	Seçim	Lisans
Dil	C# 14 / .NET 10 (LTS) — Revizyon 1	MIT
UI Framework	WPF (.NET 10)	MIT
Tuval/Render	SkiaSharp	MIT
PDF Üretimi	PDFsharp	MIT
Görüntü İşleme	SkiaSharp + MetadataExtractor	MIT / Apache 2.0
Veritabanı	SQLite	Public Domain
JSON	System.Text.Json	MIT
DI Container	Microsoft.Extensions.DependencyInjection	MIT
Log	Serilog	Apache 2.0
Test	xUnit + Moq + FluentAssertions	Apache 2.0 / MIT
Mimari	MVVM + Clean Architecture	—
Paketleme	MSIX + Inno Setup	Ücretsiz
CI/CD	GitHub Actions	Ücretsiz
Bu karar, 05 (Zaman) yol haritasındaki Aşama 0'ın çıktısıdır ve 01, 02, 04, 06 kararlarıyla doğrudan tutarlıdır.

2. Teknoloji Seçim Kriterleri
Her teknoloji aşağıdaki kriterlerden geçmiştir:

#	Kriter	Ağırlık	Açıklama
1	Ücretsiz	Zorunlu	Parasal gider 0 (Karar 06)
2	GPL uyumlu	Zorunlu	Kapalı/ticari lisans yasak (Karar 02)
3	Açık kaynak	Zorunlu	Kaynak kodu okunabilir (Karar 02)
4	Windows performansı	Yüksek	Öncelikli platform (Karar 07)
5	Olgunluk	Yüksek	Stabil, uzun süreli destek
6	Dokümantasyon	Yüksek	Tek geliştirici için kritik
7	Topluluk	Orta	Sorun çözme hızı
8	Öğrenme eğrisi	Orta	Tek geliştirici zamanı (Karar 04)
9	Performans	Yüksek	1000+ fotoğraf, büyük tuval
10	Gelecek	Orta	Cross-platform potansiyeli (Karar 07)
Kural: Zorunlu kriterlerden herhangi birini karşılamayan teknoloji reddedilir.

3. Programlama Dili ve Runtime
3.1 Seçim: C# 12+ / .NET 8
Gerekçeler:

Olgun ekosistem: NuGet'te yüz binlerce paket.

Performans: .NET 8, JIT ve AOT ile yüksek performans.

Windows entegrasyonu: WPF, WinUI, P/Invoke.

Dil özellikleri: Records, pattern matching, nullable reference types.

Uzun süreli destek: .NET 8 LTS (3 yıl).

Tek geliştirici dostu: Yüksek üretkenlik, iyi araçlar.

Açık kaynak: .NET MIT lisanslı.

Alternatifler ve neden reddedildi:

Alternatif	Neden reddedildi
C++	Geliştirme süresi uzun, bellek yönetimi zor
Rust	Öğrenme eğrisi yüksek, UI ekosistemi zayıf
Python	Masaüstü UI zayıf, dağıtım zor
Electron (JS)	Bellek tüketimi yüksek, native değil
Java	Windows entegrasyonu zayıf
3.2 Hedef framework
.NET 8 (LTS) — Windows 10 (1809+) ve Windows 11.

Self-contained deployment: .NET runtime kullanıcıda olmasa bile çalışır.

Tek dosya yayın: PublishSingleFile ile tek exe.

AOT (ileride): Başlangıç hızı ve boyut optimizasyonu.

4. UI Framework Karşılaştırması
4.1 Adaylar
Framework	Lisans	Platform	Olgunluk	Performans	Cross-platform
WPF	MIT	Windows	⭐⭐⭐⭐⭐	⭐⭐⭐⭐	❌
Avalonia	MIT	Cross	⭐⭐⭐	⭐⭐⭐	✅
WinUI 3	MIT	Windows	⭐⭐	⭐⭐⭐⭐	❌
MAUI	MIT	Cross	⭐⭐	⭐⭐	✅
Uno	Apache 2.0	Cross	⭐⭐	⭐⭐	✅
4.2 Detaylı karşılaştırma
WPF (Windows Presentation Foundation)
Güçlü yanları:

2006'dan beri olgun, stabil.

Windows'ta en iyi performans (DirectX tabanlı).

XAML, veri bağlama, stiller, şablonlar.

Devasa topluluk, sayısız kaynak.

.NET 8 ile modern.

Visual Studio designer desteği.

Ücretsiz, MIT lisanslı.

Zayıf yanları:

Sadece Windows.

Bazı eski API'ler (async eksiklikleri).

Yeni özellikler yavaş geliyor.

Bizim için uygunluk: ✅ En uygun.

Avalonia
Güçlü yanları:

Cross-platform (Windows, Linux, macOS).

WPF benzeri XAML.

Modern mimari (Skia tabanlı).

Aktif geliştirme.

MIT lisanslı.

Zayıf yanları:

Windows'ta WPF'ten yavaş (compositor sorunları).

4K'da render gecikmesi.

Daha küçük topluluk.

Bazı WPF özellikleri eksik.

Bizim için uygunluk: ⚠️ İleride (v2.0) cross-platform için düşünülebilir.

WinUI 3
Güçlü yanları:

Modern Windows UI.

Fluent Design.

Microsoft'un gelecek planı.

Zayıf yanları:

Genç, olgunlaşmamış.

Dokümantasyon eksik.

Topluluk küçük.

XAML farklı (WPF'ten geçiş zor).

Bizim için uygunluk: ❌ Riskli.

MAUI
Güçlü yanları:

Cross-platform (mobil dahil).

Microsoft destekli.

Zayıf yanları:

Masaüstü performansı zayıf.

Olgunlaşmamış.

Albüm tasarımı için uygun değil.

Bizim için uygunluk: ❌ Uygun değil.

4.3 Karar: WPF
Gerekçeler:

Windows önceliği (Karar 07) — WPF tam uyumlu.

Performans — Windows'ta en iyi (Avalonia'dan hızlı).

Olgunluk — 18+ yıl, stabil.

Topluluk — Sorun çözme hızlı.

Dokümantasyon — Bol kaynak.

Tek geliştirici — Öğrenme eğrisi düşük (C# bilen için).

Cross-platform — Gerekirse Avalonia'ya geçiş mümkün (XAML benzer).

Gelecek notu: v2.0'da Linux/macOS desteği istenirse Avalonia XPF (WPF uyumlu) veya kod taşıma değerlendirilir.

5. Tuval ve Render Kütüphanesi
5.1 Seçim: SkiaSharp
Güçlü yanları:

Google Skia tabanlı, endüstri standardı.

Yüksek performans (GPU hızlandırma).

Cross-platform.

WPF ile entegre (SkiaSharp.Views.WPF).

2D grafik, metin, görüntü, path.

MIT lisanslı.

Performans karşılaştırması:

Kütüphane	JPEG→PNG (4K)	Not
SkiaSharp	~539 ms	En hızlı
ImageSharp	~2.754 ms	5x yavaş
Magick.NET	~4.333 ms	8x yavaş
Kaynak: Bağımsız benchmark.

5.2 Kullanım alanları
Tuval render: Sayfa, fotoğraf, metin, şekil.

Thumbnail üretimi: Hızlı küçültme.

JPG/PNG çıktı: Encode.

PDF render (önizleme): PDFtoImage ile (MIT; PDFium tabanlı, bkz. Bölüm 16).

5.3 Alternatifler
Kütüphane	Neden reddedildi
System.Drawing	Windows-only, eski, GDI+
ImageSharp	Yavaş (SkiaSharp'ın 5x altı)
Magick.NET	Çok yavaş, büyük boyut
6. PDF Üretimi
6.1 Adaylar
Kütüphane	Lisans	GPL Uyum	ICC	Crop Mark	Ücretsiz
PDFsharp	MIT	✅	✅	Manuel	✅
QuestPDF	Hibrit	⚠️	⚠️	✅	⚠️ Koşullu
iText	AGPL/Ticari	❌	✅	✅	❌
Aspose.PDF	Ticari	❌	✅	✅	❌
GemBox.Pdf	Ticari	❌	✅	✅	❌
MuPDF.NET	AGPL	⚠️	✅	✅	⚠️
6.2 Detaylı analiz
PDFsharp
Güçlü yanları:

MIT lisansı (GPL uyumlu).

ICC profil desteği (CMYK JPEG byte-for-byte pass-through).

Font gömme, subset.

Unicode desteği.

Vektör çizim.

.NET 8 desteği.

Ücretsiz.

Zayıf yanları:

Crop mark otomatik değil (manuel çizim).

Yüksek seviye API yok (düşük seviye).

Bizim için uygunluk: ✅ Seçildi.

QuestPDF
Güçlü yanları:

Modern API (fluent).

Layout otomatik.

Crop mark desteği.

Aktif geliştirme.

Zayıf yanları:

Hibrit lisans: yıllık geliri 1M$ altındaki projeler için ücretsiz.

Ancak kullanıcılar için de geçerli mi belirsiz.

GPL uyumu net değil (topluluk lisansı).

ICC profil desteği sınırlı.

Bizim için uygunluk: ⚠️ Riskli (lisans belirsizliği).

6.3 Karar: PDFsharp
Gerekçeler:

MIT lisansı — GPL uyumlu, kesin.

ICC profil desteği — Baskı doğruluğu için kritik.

Ücretsiz — Koşulsuz.

Font gömme — Profesyonel çıktı.

Düşük seviye kontrol — Crop mark, bleed manuel çizilir (esneklik).

Crop mark çizimi: PDFsharp ile manuel çizilir (standart formül). Bu, ek geliştirme gerektirir ama tam kontrol sağlar.

ICC profil: PDFsharp, CMYK JPEG ve ICC profilli RGB JPEG'leri byte-for-byte gömer. Bu, renk doğruluğu için yeterlidir.

7. Görüntü İşleme ve Metadata
7.1 Görüntü decode/encode
SkiaSharp — Decode, resize, encode.

Desteklenen formatlar: JPEG, PNG, TIFF, WebP, BMP, GIF.

RAW desteği yok (ileride LibRaw).

7.2 Metadata (EXIF)
Seçim: MetadataExtractor

Apache 2.0 lisansı (GPL uyumlu).

EXIF, IPTC, XMP okuma.

Yön (Orientation), tarih, kamera bilgisi.

Hafif, hızlı.

Alternatifler:

Kütüphane	Lisans	Neden reddedildi
ImageSharp	Apache 2.0 / Six Labors Split	Metadata için ağır
ExifLib	MIT	Bakımsız
System.Drawing	MIT	Windows-only, eski
7.3 Thumbnail stratejisi
SkiaSharp ile hızlı küçültme.

Arka plan thread'de üretim.

Disk önbelleği (%APPDATA%/NexAlbum/cache/).

LRU bellek önbelleği (son N thumbnail).

Boyutlar: 256px (galeri), 2048px (önizleme).

8. Veritabanı ve Kalıcılık
8.1 Seçim: SQLite
Gerekçeler:

Public domain.

Dosya tabanlı (sunucu yok).

Hızlı, güvenilir.

.NET entegrasyonu (Microsoft.Data.Sqlite).

Şema migration desteği.

Kullanım alanları:

Proje metadata (son açılanlar, ayarlar).

Thumbnail önbellek indeksi.

Arama geçmişi.

Otomatik kayıt/kurtarma.

Not: Proje verisi (albüm, sayfa, yerleşim) SQLite'ta değil; .albumproj ZIP içinde JSON olarak saklanır (Karar 09).

8.2 Alternatifler
Alternatif	Neden reddedildi
LiteDB	Daha az olgun, dokümantasyon zayıf
JSON dosya	Büyük veri için yavaş
SQL Server	Ağır, gereksiz
9. JSON ve Serileştirme
9.1 Seçim: System.Text.Json
Gerekçeler:

.NET 8 yerleşik.

Yüksek performans.

Source generator (AOT uyumlu).

MIT lisanslı.

9.2 Şema doğrulama
JSON Schema (Draft 2020-12).

JsonSchema.Net kütüphanesi (MIT).

Profil, proje, şablon şemaları.

9.3 Proje dosyası formatı
.albumproj = ZIP.

İçinde: manifest.json, project.json, profiles.json, assets/, thumbnails/.

Şema sürümü: schemaVersion.

Migration altyapısı.

10. Dependency Injection ve Konfigürasyon
10.1 Seçim: Microsoft.Extensions.DependencyInjection
Gerekçeler:

.NET yerleşik.

Hafif, hızlı.

Constructor injection.

Scoped/singleton/transient.

MIT lisanslı.

10.2 Konfigürasyon
Microsoft.Extensions.Configuration (JSON + ortam değişkenleri).

Kullanıcı ayarları: %APPDATA%/NexAlbum/settings.json.

11. Loglama
11.1 Seçim: Serilog
Gerekçeler:

Yapılandırılmış log (JSON).

Çoklu sink (dosya, konsol, hata ayıklama).

Yüksek performans.

Apache 2.0 lisanslı.

11.2 Kullanım
Hata logları: %APPDATA%/NexAlbum/logs/.

Günlük dosya rotasyonu.

Kullanıcıya "logları aç" seçeneği.

12. Test Araçları
12.1 Seçimler
Araç	Amaç	Lisans
xUnit	Test framework	Apache 2.0
Moq	Mock	MIT
FluentAssertions	Okunabilir assertion	Apache 2.0
Jint	HTML prova efekt betiğinin eşdeğerlik testi (yalnızca test)	BSD-2-Clause
Coverlet	Kod kapsamı	MIT
BenchmarkDotNet	Performans testi	MIT
12.2 Test piramidi
text
        /\
       /  \      E2E (5-10 test)
      /____\
     /      \    Entegrasyon (20-50 test)
    /________\
   /          \  Birim (100-500 test)
  /____________\
12.3 Kritik test alanları
Layout engine (izole, deterministik).

Profil şema doğrulama.

PDF üretimi (ölçü, bleed).

Proje kaydet/aç round-trip.

13. Mimari Desen
13.1 Seçim: MVVM + Clean Architecture
Katmanlar:

text
┌─────────────────────────────────────┐
│  NexAlbum.UI (WPF)              │
│  MVVM: View, ViewModel              │
├─────────────────────────────────────┤
│  NexAlbum.Core (Domain)         │
│  Album, Page, Photo, Template,      │
│  Layout Engine, Preflight           │
├─────────────────────────────────────┤
│  NexAlbum.Rendering             │
│  SkiaSharp tuval, thumbnail, JPG    │
├─────────────────────────────────────┤
│  NexAlbum.Export                │
│  PDF (PDFsharp), SVG, raster        │
├─────────────────────────────────────┤
│  NexAlbum.Assets                │
│  Fotoğraf, EXIF, önbellek           │
├─────────────────────────────────────┤
│  NexAlbum.Persistence           │
│  .albumproj, JSON, SQLite           │
└─────────────────────────────────────┘
13.2 Bağımlılık yönü
UI → Core, Rendering, Export, Assets, Persistence, Integrations.

Core → (hiçbir şey; saf domain).

Rendering → Core.

Export → Core, Rendering.

Assets → Core.

Persistence → Core.

Integrations → Core, Assets (Revizyon 2).

Kural: Core katmanı hiçbir UI/IO bağımlılığı içermez.

13.3 MVVM araçları
CommunityToolkit.Mvvm (MIT) — ObservableObject, RelayCommand.

Manuel MVVM (basit durumlarda).

Veri bağlama: WPF yerleşik.

14. Paketleme ve Dağıtım
14.1 Seçim: MSIX + Inno Setup + Portable ZIP
Format	Kullanım	Avantaj
MSIX	Microsoft Store, kurumsal	Modern, temiz kurulum
Inno Setup	Genel kullanıcı	Basit, yaygın
Portable ZIP	Taşınabilir	Kurulum yok
14.2 Inno Setup
Ücretsiz, açık kaynak.

Basit script.

Türkçe dil desteği.

Kurulum/kaldırma.

%APPDATA% yazma.

14.3 MSIX (opsiyonel)
Microsoft Store için.

Kod imzalama gerekir (Karar 06: ertelendi).

Sideload için self-signed sertifika.

14.4 Portable ZIP
ZIP'i aç, NexAlbum.exe çalıştır.

Ayarlar ./data/ altında (taşınabilir).

Kurulum gerektirmez.

14.5 Otomatik güncelleme
MVP'de yok.

v1.1'de: GitHub Releases kontrolü (opt-in).

Kullanıcı isterse indirir, kurar.

15. CI/CD
15.1 Seçim: GitHub Actions
Public repo için ücretsiz.

Windows runner.

Derleme, test, paketleme.

Release otomasyonu.

15.2 Workflow'lar
Workflow	Tetikleyici	İş
build.yml	Push/PR	Derleme + test
test.yml	Push/PR	Birim + entegrasyon
release.yml	Tag	Paketleme + GitHub Release
codeql.yml	Haftalık	Güvenlik analizi
15.3 Kalite kapıları
Tüm testler geçmeli.

Kod kapsamı > %70 (kritik yol > %80).

Lisans kontrolü geçmeli.

Kod stili uyumlu (EditorConfig).

16. Lisans Uyumluluk Matrisi
Teknoloji	Lisans	GPL-3.0+ Uyum	Durum
.NET 8	MIT	✅	Onaylı
WPF	MIT	✅	Onaylı
SkiaSharp	MIT	✅	Onaylı
PDFsharp	MIT	✅	Onaylı
MetadataExtractor	Apache 2.0	✅	Onaylı
SQLite	Public Domain	✅	Onaylı
System.Text.Json	MIT	✅	Onaylı
Microsoft.Extensions.DI	MIT	✅	Onaylı
PDFtoImage (önizleme)	MIT (NuGet sayfası)	✅	Onaylı; yerel PDFium bağımlılığının lisansı Aşama 0'da doğrulanacak
Serilog	Apache 2.0	✅	Onaylı
xUnit	Apache 2.0	✅	Onaylı
Moq	MIT	✅	Onaylı
FluentAssertions	Apache 2.0	✅	Onaylı
Jint (yalnızca test)	BSD-2-Clause	✅	Onaylı (Karar 23)
CommunityToolkit.Mvvm	MIT	✅	Onaylı
Inter (yazı tipi, gömülü)	SIL OFL 1.1	✅	Onaylı (Revizyon 3)
Lucide (ikonlar, gömülü)	ISC	✅	Onaylı (Revizyon 3)
Albüm yazı tipleri (7 aile, gömülü)	SIL OFL 1.1	✅	Onaylı (Revizyon 4)
SkiaSharp.NativeAssets.Linux.NoDependencies	MIT	✅	Onaylı (Revizyon 5)
FFmpeg (harici, kullanıcı onayıyla indirilir)	GPL-3.0	✅	Onaylı (Revizyon 5)
JsonSchema.Net	MIT	✅	Onaylı
QuestPDF	Hibrit	⚠️	Reddedildi
iText	AGPL/Ticari	❌	Reddedildi
ImageSharp	Apache 2.0 / Split	⚠️	Reddedildi (performans)
İlke: Tüm bağımlılıklar GPL-3.0+ uyumludur. Her PR'da otomatik lisans kontrolü yapılır (scripts/verify-licenses.ps1).

17. Teknoloji Yığını Özeti
text
┌──────────────────────────────────────────────────────┐
│                   NexAlbum                       │
├──────────────────────────────────────────────────────┤
│  Dil:        C# 12+ / .NET 8 (LTS)                   │
│  UI:         WPF (Windows)                           │
│  MVVM:       CommunityToolkit.Mvvm                   │
│  Tuval:      SkiaSharp                               │
│  PDF:        PDFsharp                                │
│  EXIF:       MetadataExtractor                       │
│  Veritabanı: SQLite                                  │
│  JSON:       System.Text.Json + JsonSchema.Net       │
│  DI:         Microsoft.Extensions.DI                 │
│  Log:        Serilog                                 │
│  Test:       xUnit + Moq + FluentAssertions          │
│  CI/CD:      GitHub Actions                          │
│  Paketleme:  Inno Setup + MSIX + Portable ZIP        │
│  Lisans:     GPL-3.0-or-later (proje)                │
└──────────────────────────────────────────────────────┘
18. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
WPF öğrenme eğrisi	Düşük	Orta	C# bilen için kolay; dokümantasyon bol
PDFsharp düşük seviye	Orta	Orta	Crop mark manuel; kabul edildi
PDFsharp ICC sorunu	Düşük	Yüksek	Erken prototip; test
SkiaSharp bellek	Orta	Orta	Proxy, LRU, dispose disiplini
Avalonia'ya geçiş ihtiyacı	Düşük	Yüksek	XAML benzer; v2.0'da değerlendir
GitHub Actions limiti	Düşük	Düşük	Public repo ücretsiz; yeterli
NuGet paket sorunu	Düşük	Orta	Kilit dosyası, versiyon sabitleme
.NET 8 sonrası migration	Orta	Düşük	LTS 3 yıl; sonra .NET 10 LTS
SkiaSharp cross-platform	Düşük	Düşük	Windows'ta sorunsuz
19. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Tüm teknolojiler ücretsiz.
02 — Açık Kaynak	Tüm lisanslar GPL uyumlu.
03 — Kullanıcı Profilleri	JSON + JSON Schema.
04 — Tek Kişi	WPF olgun, dokümantasyon bol.
05 — Zaman	Aşama 0 çıktısı; yol haritasına girdi.
06 — Bütçe	Sıfır parasal gider.
07 — Rakip Özellikleri	Windows önceliği, PDF/SVG çıktı, performans.
20. Açık Sorular (Sonraki Kararlara Bırakılan)
Avalonia'ya geçiş ne zaman? → v2.0 (cross-platform istenirse)

RAW desteği (LibRaw)? → v1.2

.NET 10'a geçiş? → 2026 sonrası

AOT (native derleme)? → v2.0

PDFsharp mı, alternatif mi? → Aşama 4'te prototip ile doğrulanacak

21. Özet
NexAlbum'in teknoloji yığını:

C# 12+ / .NET 8 — olgun, hızlı, ücretsiz.

WPF — Windows'ta en iyi performans, olgun, bol dokümantasyon.

SkiaSharp — En hızlı görüntü işleme, cross-platform potansiyeli.

PDFsharp — MIT lisanslı, ICC destekli, ücretsiz PDF.

SQLite — Hafif, dosya tabanlı veritabanı.

System.Text.Json + JsonSchema.Net — Profil/proje şemaları.

GitHub Actions — Ücretsiz CI/CD.

Inno Setup + MSIX + Portable ZIP — Çoklu dağıtım.

MVVM + Clean Architecture — Test edilebilir, bakımı kolay.

GPL-3.0+ uyumlu — Tüm bağımlılıklar.

Bu yığın, 01 (Ücretsiz), 02 (Açık Kaynak), 04 (Tek Kişi), 06 (Sıfır Bütçe) kararlarıyla tam tutarlıdır. Tek geliştirici için sürdürülebilir, Windows'ta yüksek performanslı, gelecekte cross-platform'a açık bir temel oluşturur.

Onay: Bu karar, ürünün teknik temelinin belgesidir ve Aşama 0'ın çıktısıdır.