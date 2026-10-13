# Kaldığımız yer (devir notu)

Son güncelleme: 13 Ekim 2026 (akşam). Başka bilgisayarda devam etmek için önce bunu okuyun.

## Yeni bilgisayarda kurulum
1. Git ve .NET SDK 10 (10.0.4xx) kurun: `dotnet --list-sdks`
2. `git clone https://github.com/Eirahoutmoss/NexAlbum.git` ve `cd NexAlbum`
3. Derle ve test et: `dotnet build NexAlbum.slnx` → `dotnet test NexAlbum.slnx`
4. Çalıştır: `dotnet run --project src/NexAlbum.UI`
5. Claude ile devam: depoyu açın; kurallar `CLAUDE.md`, kararlar `docs/kararlar/INDEKS.md`.

## Durum (main dalı)
Çalışan uçtan uca akış: yeni proje (profil) → fotoğraf ekle → otomatik albüm → elle düzenleme → tasarım → ön kontrol → baskı PDF'i.

| Alan | Hazır olanlar |
|---|---|
| Çekirdek (Core) | Profil, proje, sayfa, yuva modeli; 200+ şablon (1–12 fotoğraf); deterministik otomatik yerleşim; PageDesigner, PageEditor |
| Proje dosyası | `.albumproj` (ZIP + JSON), şema sürümü, migration, atomik kayıt, kilit dosyası |
| İçe aktarma | Dosya seçme (çoklu), klasör, Gezgin'den sürükle-bırak; EXIF; fon görselleri ayrı rol |
| Tasarım | 14 çerçeve (paspartu, polaroid, gölge, yuvarlak, oval…), düz/gradyan/özel renk/görsel fon, boşluk ve kenar payı |
| Elle düzenleme | Galeriden yuvaya sürükle, yuvadan yuvaya yer değiştir, Ctrl+sürükle kaydır, tekerlek yakınlaştır, kapla/sığdır, çıkar, tek yuvaya çerçeve |
| Çıktı | Baskı PDF'i (taşma payı, kesim işaretleri, TrimBox/BleedBox); ekranla aynı yerleşim hesabı |
| Harici düzenleme (Karar 20) | Düzenleyicide aç (kayıt izlenir), toplu rötuş: Photoshop action'ı (COM) veya darktable/RawTherapee/özel araç; önce dene, önce/sonra, düzenlemeyi kaldır; orijinaller değişmez |
| Arayüz | Galeri · tuval · sekmeli sağ panel (Sayfa / Tasarım / Şablonlar) · sayfa şeridi; Rötuş, Ayarlar, Hakkında |

Testler: Windows 472, Ubuntu 438 — hepsi geçiyor; arayüz kapsamı %71,8. Katmanlar: Core, Rendering, Export, Assets, Persistence, Integrations (Karar 08 Rev. 2), UI. CI: `.github/workflows/build.yml`, `test.yml`.

## Elle denenmesi gerekenler
Fare/sürükle-bırak davranışı otomatik test edilemiyor: Gezgin'den bırakma, galeriden yuvaya sürükleme,
yuvalar arası taşıma, Ctrl+sürükle kaydırma, tekerlekle yakınlaştırma, çerçeve/gölge görünümü.
Photoshop CI'da yok: "Rötuş → Action'ları yükle", bir action ile "Önce bir fotoğrafta dene" ve
"Düzenleyicide aç → Photoshop'ta kaydet → albüm yenilendi mi" gerçek Photoshop ile denenmeli.

## Sıradaki işler (öncelik sırasıyla)
1. Sayfa yönetimi: sayfa ekle/sil/çoğalt, şeritte sürükleyerek sırala, sayfalar arası fotoğraf taşıma.
2. Geri al / yinele (Ctrl+Z / Ctrl+Y) — proje değişmez kayıt (record) olduğundan anlık görüntü yığınıyla kolay.
3. Metin öğeleri (başlık, tarih, "Özel Anlar" gibi yazılar) — model hazır, arayüz ve PDF çizimi yok.
4. Marka: `docs/marka/` konseptinden Karar 17 → SVG logo, `.ico` uygulama simgesi, açılış ekranı, isteğe bağlı koyu/altın tema.
5. Kapak ve sırt (Karar 03 sırt genişliği), JPG/PNG çıktı (Karar 11).
6. Rötuş için sıradaki adımlar: Photoshop UXP paneli (isteğe bağlı), yüz/cilt odaklı hazır action paketi önerileri, rötuşçuya iş listesi devretme (Karar 20 §1).

## Notlar
- Eski `feature/*` dalları uzak depoda duruyor (silinmeleri sorun değil); her şey `main`'de.
- Commit kuralı: Conventional Commits + `git commit -s` (Karar 14).
