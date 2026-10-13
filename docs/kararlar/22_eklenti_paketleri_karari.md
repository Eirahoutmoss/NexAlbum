Karar Dokümanı 22 — Eklenti Paketleri (Stil ve Efekt Paketleri)
Proje: NexAlbum
Karar no: 22
Konu: Kullanıcının yükleyebildiği, yalnızca veri taşıyan stil/efekt paketleri
Durum: Kesinleşti (ilk aşama: film stilleri)
Tarih: 13/10/2026
İlgili kararlar: 08 (Teknoloji, katman kuralları), 09 (Dosya biçimi), 21 (Hikâye filmi), 17 (Marka), 15 (Güvenlik — bekliyor)
İstek: Hasan Güler — "stil, efekt gibi şeyler için eklenti paketi yayınlarsak kullanıcı yükleyip yeni ve pro efektlerle film oluşturabilir; programımız buna uygun olsun".

1. Karar Özeti
NexAlbum'e yeni görünüm, geçiş ve stil eklemek için kod değil **veri paketi** kullanılır. Paket bir `manifest.json` (ve ileride görseller,
LUT, yazı tipi gibi kaynak dosyaları) taşır; uygulama paketi okur, doğrular ve stillerini yerleşik stillerin yanına ekler. Paketler betik,
DLL ya da çalıştırılabilir dosya içeremez; böylece güvenlik riski (Karar 15) ve sürüm kırılması en aza iner. Paket olmadan da her şey çalışır.

2. Paket biçimi (formatVersion 1)
- Dağıtım: `.nexpaket` (ZIP) ya da aynı içerikli klasör. Yer: `%LOCALAPPDATA%\NexAlbum\eklentiler`. Yükleme = dosyayı oraya kopyalamak
  (Komut paleti → «Eklenti paketi yükle»).
- `manifest.json`: `format: "nexalbum-extension"`, `formatVersion: 1`, `id` (küçük harf/rakam/tire, en fazla 64), `name`, `version` (SemVer),
  `author`, `license`, `description`; içerik dizileri: `videoStyles` (bu sürümde), `textStyles`, `frameStyles`, `photoLooks`, `templates`
  (ayrılmış; bu sürüm atlar ve bildirir). Bildirim en fazla 1 MB.
- Film stili alanları (Karar 21 §4): `id`, `name`, `hint`, `transitions` (cut, crossfade, dipToBlack, dipToWhite, focusShift, iris,
  pushLeft, pushRight, pushUp, zoomThrough), `chapterTransition`, `photoSeconds` (0,8–15), `transitionSeconds` (0–4), `motion` (0–0,3),
  `look` (natural, warm, cool, blackWhite, sepia), `vignette`, `grain`. Ayrıntı ve örnek: `docs/eklentiler.md`, `samples/eklentiler/ornek-sinema`.

3. Kurallar
- Kimlikler: paketin öğeleri `paket-id/öğe-id` olarak kaydedilir; yerleşik stillerle ve birbirleriyle çakışmaz. Aynı kimlikli ikinci paket yüklenmez.
- Doğrulama: geçersiz paket atlanır; geçersiz öğe atlanır, geçerliler yüklenir; nedenler kullanıcıya bildirilir. Bilinmeyen biçim sürümü
  "NexAlbum'ü güncelleyin" der.
- Projeler: proje dosyası yalnızca stil kimliğini saklar. Paketi kaldırılmış projede film, ilk yerleşik stille üretilir; kimlik silinmez,
  paket geri yüklenince yeniden çalışır.
- Katmanlar: katalog Core'da (`ExtensionCatalog`, saf), okuma Persistence'ta (`ExtensionPackageReader`), klasör/yükleme UI'da (`ExtensionService`).
- Lisans: her paket kendi lisansını bildirir; NexAlbum ile dağıtılan örnek paketler GPL-uyumlu ya da CC0 olmalıdır.

4. Sonraki aşamalar (bu kararla açıldı, uygulanmadı)
- Yazı stilleri (font + etki hazırları), çerçeve stilleri, fotoğraf görünümleri (LUT/.cube), şablon paketleri.
- Paket imzası ve yayıncı doğrulaması (Karar 15 ile), paket galerisi (çevrimdışı dosya ile; zorunlu çevrimiçi mağaza yok).
