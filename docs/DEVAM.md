# Kaldığımız yer (devir notu)

Son güncelleme: 13 Ekim 2026 (geri al / yinele, sayfa yönetimi). Başka bilgisayarda devam etmek için önce bunu okuyun.

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

| **Geometrik şablonlar (Karar 10 Rev. 1)** | Çokgen yuvalar (üçgen, elmas, altıgen, sekizgen/kesik köşe, kemer, eğik); 28 geometrik şablon (çapraz ikili, karşılıklı üçgenler + üçlü, elmas merkez, petek, eğik bantlar, yelpaze, kemerler); çapraz kenarlarda da eşit boşluk; otomatik yerleşimde en fazla 5 sayfada bir geometrik sayfa; `.albumproj` şema v2 + göç |
| **Biçimli çerçeveler** | Kemer, altıgen, elmas, kesik köşe, üçgen, eğik (+ altın/paspartu çeşitleri); Tasarım sekmesinde ayrı bölüm |
| **Şablon galerisi** | Biçim süzgeci (Tümü/Geometrik/Kolaj/Izgara/Sade); önizlemeler seçili sayfanın kendi fotoğraflarıyla |
| **Kitap görünümü / sunum** | Ctrl+B karşılıklı sayfalar (cilt gölgesi); F5 tam ekran sunum (kırpılmış, kılavuzsuz, çift sayfa) |

| **Geri al / yinele** | Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z), başlıkta düğmeler; 100 adım, kaydırıcı sürüklemesi tek adım; kaydedilmiş hâle dönülünce "Kaydedildi" |
| **Sayfa yönetimi** | Ekle (komşu fonuyla), çoğalt (Ctrl+D), sil (onaysız, geri alınır), sola/sağa taşı, şeritte sürükleyerek sırala, sağ tık menüsü, sonda "+" kutusu; kilitli sayfa silinmez/taşınmaz |

Testler: Windows 545, Ubuntu 472 — hepsi geçiyor. Takılan test 5 dakikada kesilir (`--blame-hang-timeout`). CI: `build.yml`, `test.yml` ve `screenshots.yml`
(her dalda iki temada ekran görüntüsü üretir ve `ekran/<dal>` dalına yazar; ör. `ekran/main`).

## Elle denenmesi gerekenler
Otomatik test edilemeyenler:
- **Pencere çerçevesi:** başlıktan sürükleme, çift tıkla büyütme, kenardan boyutlandırma, Win+ok ile yaslama, ekranı kaplarken
  kenarların taşmaması, simge durumu/kapat düğmeleri; diyaloglarda koyu Windows başlık çubuğu (Gece teması).
- **Tema geçişi:** başlıktaki güneş/ay ile Gece ↔ Fildişi; açık diyaloglar ve açılır menüler (ComboBox, sağ tık menüsü, ipuçları) doğru renkte mi?
- **Açılış ekranı** uygulama açılırken görünüp soluyor mu? Görev çubuğu/Alt+Tab simgesi altın "N" mi?
- Fare/sürükle-bırak: Gezgin'den galeriye bırakma, galeri karesinden yuvaya sürükleme, yuvalar arası taşıma, Ctrl+sürükle kaydırma,
  tekerlekle yuva yakınlaştırma, Ctrl+tekerlekle tuval yakınlaştırma; galeri karesine tıklayıp sağ tık menüsü.
- **Sayfa şeridi:** küçük resmi sürükleyip başka bir yere bırakma (altın çizgi doğru yerde mi), sağ tık menüsü, Ctrl+Z ile silinen sayfanın geri gelmesi.
- **Kitap görünümü:** karşı sayfadaki yuvaya tıklama/galeriden bırakma o sayfayı seçip oraya uygulanmalı.
- **Sunum (F5):** ← → / boşluk / tıklama ile gezinme, Esc ile çıkış, birkaç saniye sonra alt bilginin ve imlecin gizlenmesi; çok monitörde doğru ekranda açılması.
- **Geometrik sayfaların PDF'i:** çapraz kenarlardaki boşluk ekranla aynı mı (baskı evine göndermeden önce bir sayfa basıp bakın).
- Photoshop CI'da yok: "Rötuş → Action'ları yükle", "Önce bir fotoğrafta dene", "Düzenleyicide aç → kaydet → albüm yenilendi mi".

## Sıradaki işler (öncelik sırasıyla)
1. **Metin öğeleri:** başlık, tarih, "Özel Anlar" gibi yazılar (model hazır; arayüz, yazı tipi seçimi ve PDF çizimi yok).
2. Sayfalar arası fotoğraf taşıma (tuvaldeki fotoğrafı şeritteki başka bir sayfaya sürükleme); fotoğrafa yıldız/favori.
3. **Hikâye akışı:** EXIF saatine göre düğün evreleri (hazırlık, gelin alma, nikâh, dış çekim, düğün) → bölümler.
4. Kapak ve sırt (Karar 03 sırt genişliği); JPG/PNG çıktı (Karar 11).
5. Çift için çevrimdışı HTML prova (beğeni/not, geri içe aktarma); geometrik yuvalarda elle köşe düzenleme.
6. Rötuş: Photoshop UXP paneli, hazır action paketi önerileri, rötuşçuya iş listesi (Karar 20 §1).

## Notlar
- Commit kuralı: Conventional Commits + `git commit -s` (Karar 14).
- Yeni ikon: `tools/ikonlar/lucide_to_xaml.py` içindeki listeye ekleyip `Themes/Icons.xaml`'ı yeniden üretin.
- WPF düzen tuzağı: boyutu kendi genişliğine bağlı öğeler (galeri kareleri) "Auto" kaydırma çubuklu bir alanda sonsuz düzen
  döngüsüne girer; çubuk her zaman yer ayırmalı (`VerticalScrollBarVisibility="Visible"`).
- Renk yalnızca anlamsal anahtarla (`{DynamicResource AccentBrush}` vb.); XAML/C# içinde sabit renk yazılmaz (sayfa içeriği hariç).
