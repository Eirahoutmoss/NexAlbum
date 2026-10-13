# Kaldığımız yer (devir notu)

Son güncelleme: 13 Ekim 2026 (Faz 1 — premium arayüz). Başka bilgisayarda devam etmek için önce bunu okuyun.

## Yeni bilgisayarda kurulum
1. Git ve .NET SDK 10 (10.0.1xx veya üstü) kurun: `dotnet --list-sdks`
2. `git clone https://github.com/Eirahoutmoss/NexAlbum.git` ve `cd NexAlbum`
3. Derle ve test et: `dotnet build NexAlbum.slnx` → `dotnet test NexAlbum.slnx`
4. Çalıştır: `dotnet run --project src/NexAlbum.UI`
5. Claude ile devam: depoyu açın; kurallar `CLAUDE.md`, kararlar `docs/kararlar/INDEKS.md`.

## Durum
Çalışan uçtan uca akış: yeni proje (profil) → fotoğraf ekle → otomatik albüm → elle düzenleme → tasarım → ön kontrol → baskı PDF'i.

| Alan | Hazır olanlar |
|---|---|
| Çekirdek (Core) | Profil, proje, sayfa, yuva modeli; 200+ şablon (1–12 fotoğraf); deterministik otomatik yerleşim; PageDesigner, PageEditor |
| Proje dosyası | `.albumproj` (ZIP + JSON), şema sürümü, migration, atomik kayıt, kilit dosyası |
| İçe aktarma | Dosya seçme (çoklu), klasör, Gezgin'den sürükle-bırak; EXIF; fon görselleri ayrı rol |
| Tasarım | 14 çerçeve (paspartu, polaroid, gölge, yuvarlak, oval…), düz/gradyan/özel renk/görsel fon, boşluk ve kenar payı |
| Elle düzenleme | Galeriden yuvaya sürükle, yuvadan yuvaya yer değiştir, Ctrl+sürükle kaydır, tekerlek yakınlaştır, kapla/sığdır, çıkar, tek yuvaya çerçeve |
| Çıktı | Baskı PDF'i (taşma payı, kesim işaretleri, TrimBox/BleedBox); ekranla aynı yerleşim hesabı |
| Harici düzenleme (Karar 20) | Düzenleyicide aç (kayıt izlenir), toplu rötuş: Photoshop action'ı (COM) veya darktable/RawTherapee/özel araç; önce dene, önce/sonra, düzenlemeyi kaldır |
| **Marka (Karar 17)** | Altın kurdele "N" logosu (SVG + XAML), çok boyutlu `.ico`, açılış ekranı, yazı markası; kaynaklar `docs/marka/`, üretim `tools/marka/uret.py` |
| **Arayüz (Karar 12 Rev. 1)** | Gece (varsayılan) / Fildişi teması, çalışırken değişir (başlıktaki güneş/ay, Ayarlar). Marka başlık çubuğu (WindowChrome), ikonlu araç çubuğu, galeri ızgarası + süzgeçler (Tümü/Kullanılmamış/Albümde/Rötuşlu), tuval yakınlaştırma (Ctrl+tekerlek, Ctrl±, Ctrl+0 = %100 baskı boyutu, Ctrl+F sığdır), sekmeli sağ panel, sayfa şeridi, başlangıç ekranı + son projeler, komut paleti (Ctrl+K; "on kontrol" → "Ön kontrol") |

Testler: Windows 496, Ubuntu 438 — hepsi geçiyor. CI: `build.yml`, `test.yml` ve `screenshots.yml`
(her dalda iki temada ekran görüntüsü üretir ve `ekran/<dal>` dalına yazar; ör. `ekran/main`).

## Elle denenmesi gerekenler
Otomatik test edilemeyenler:
- **Pencere çerçevesi:** başlıktan sürükleme, çift tıkla büyütme, kenardan boyutlandırma, Win+ok ile yaslama, ekranı kaplarken
  kenarların taşmaması, simge durumu/kapat düğmeleri; diyaloglarda koyu Windows başlık çubuğu (Gece teması).
- **Tema geçişi:** başlıktaki güneş/ay ile Gece ↔ Fildişi; açık diyaloglar ve açılır menüler (ComboBox, sağ tık menüsü, ipuçları) doğru renkte mi?
- **Açılış ekranı** uygulama açılırken görünüp soluyor mu? Görev çubuğu/Alt+Tab simgesi altın "N" mi?
- Fare/sürükle-bırak: Gezgin'den galeriye bırakma, galeri karesinden yuvaya sürükleme, yuvalar arası taşıma, Ctrl+sürükle kaydırma,
  tekerlekle yuva yakınlaştırma, Ctrl+tekerlekle tuval yakınlaştırma; galeri karesine tıklayıp sağ tık menüsü.
- Photoshop CI'da yok: "Rötuş → Action'ları yükle", "Önce bir fotoğrafta dene", "Düzenleyicide aç → kaydet → albüm yenilendi mi".

## Sıradaki işler (öncelik sırasıyla)
1. **Faz 2 — profesyonel iş akışı:** sayfa yönetimi (ekle/sil/çoğalt, şeritte sürükleyerek sırala, sayfalar arası fotoğraf taşıma);
   geri al / yinele (Ctrl+Z / Ctrl+Y — proje değişmez kayıt olduğundan anlık görüntü yığını); metin öğeleri (model hazır; arayüz ve PDF
   çizimi yok); fotoğrafa yıldız/favori; kapak ve sırt (Karar 03 sırt genişliği); JPG/PNG çıktı (Karar 11).
2. **Faz 3 — farklılaştırıcılar:** hikâye akışı (EXIF saatine göre düğün evreleri: hazırlık, gelin alma, nikâh, dış çekim, düğün → bölümler);
   çift için çevrimdışı HTML prova (beğeni/not, geri içe aktarma); sunum modu (Karar 17 §10); çift sayfa (spread) görünümü.
3. Rötuş: Photoshop UXP paneli (isteğe bağlı), yüz/cilt odaklı hazır action paketi önerileri, rötuşçuya iş listesi devretme (Karar 20 §1).

## Notlar
- Commit kuralı: Conventional Commits + `git commit -s` (Karar 14).
- Yeni ikon: `tools/ikonlar/lucide_to_xaml.py` içindeki listeye ekleyip `Themes/Icons.xaml`'ı yeniden üretin.
- Renk yalnızca anlamsal anahtarla (`{DynamicResource AccentBrush}` vb.); XAML/C# içinde sabit renk yazılmaz (sayfa içeriği hariç).
