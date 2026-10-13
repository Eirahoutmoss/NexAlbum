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
| **Sayfa metinleri (Karar 17 §11)** | Metin sekmesi: 6 hazır stil (başlık, alt başlık, tarih, isimler, alıntı, serbest), 8 gömülü yazı tipi (4 el yazısı, 2 serif, 2 sans; OFL), boyut, satır aralığı, kutu genişliği, kalın/eğik, hizalama, renk; tuvalde seç/sürükle/çift tıkla yaz, Ctrl+T, Del, geri al; PDF'te aynı satır kaydırma ve gömülü yazı tipi |
| **Galeri ve taşıma** | Favori yıldızı (karede, F, sağ tık, palet) + Favoriler süzgeci (otomatik yerleşimde önce yerleşir); eklenme/çekim zamanı/dosya adı sıralaması (IMG_2 < IMG_10); fotoğrafı şeritteki sayfaya bırak (galeriden kopyalar, tuvalden taşır; oranına en uygun boş yuva); kitap görünümünde karşı sayfanın yuvasına taşı/yer değiştir |
| **Yazı etkileri** | Gölge, Hale, Kontur, Bant, Kabartma (renk + güç, kendi yazı tipiyle önizlemeli), «Okunur yap», harf aralığı; ekran ve PDF ortak `TextLayout`; şema v3 |
| **Fotoğraf konumlandırma** | Tekerlek imlecin olduğu noktaya yakınlaşır; çift tık = konumlandırma kipi (düz sürükle kaydırır, kırpılan kısım soluk görünür, Esc biter); yön tuşları (Shift ×10), + / −, Home; 3×3 hizalama ızgarası; yön tuşları seçili metni de taşır |
| **Hikâye akışı (Karar 10 Rev. 2)** | Hikâye sekmesi: çekim saatinden bölümler (ara ayarlı, gün değişimi böler, küçükler birleşir), Türkçe evre adı önerisi (Kına Gecesi, Hazırlık, Gelin Alma, Dış Çekim, Nikâh, Düğün…), ad/hazır ad menüsü, birleştir, galeriden «Buradan yeni bölüm»; zaman çizelgesi; galeride bölüm süzgeci ve renk çizgisi; şeritte bölüm etiketleri; «Hikâyeye göre oluştur» (her bölüm yeni sayfada, isteğe bağlı tam sayfa açılış + okunur başlık); şema v4 |

Testler: Windows 586, Ubuntu 494 — hepsi geçiyor. Takılan test 5 dakikada kesilir (`--blame-hang-timeout`). CI: `build.yml`, `test.yml` ve `screenshots.yml`
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
- **Şeride bırakma:** galeriden ve tuvalden şeritteki küçük resme sürükleyince altın çerçeve çıkıyor mu; dolu/kilitli sayfada bırakma reddediliyor mu.
- **Hikâye:** gerçek bir düğün klasöründe «Bölümleri bul» makul mü (fotoğrafçıların saatleri farklıysa: iki makine saati kaymışsa bölümler bölünür — ileride «makine saat farkı düzeltme»); açılış sayfasında başlık okunuyor mu.
- **Konumlandırma:** yuvaya çift tık → sürükleyince fotoğraf kayıyor mu, soluk taşan kısım doğru mu; tekerlek imlecin altındaki noktaya mı yakınlaşıyor; yön tuşları panel kaydırıcısına değil fotoğrafa mı gidiyor (önce tuvale tıklayın).
- **Yazı etkileri PDF'te:** hale/gölge ekrandaki kadar yumuşak mı; kontur ince harflerde (el yazısı) kalın durmuyor mu.
- **Metinler:** el yazısı yazı tiplerinde Türkçe harfler (ğ, ş, İ, ı) PDF'te doğru mu; çift tıklayınca yazı kutusu odaklanıyor mu; sürüklerken kaymıyor mu.
- **Geometrik sayfaların PDF'i:** çapraz kenarlardaki boşluk ekranla aynı mı (baskı evine göndermeden önce bir sayfa basıp bakın).
- Photoshop CI'da yok: "Rötuş → Action'ları yükle", "Önce bir fotoğrafta dene", "Düzenleyicide aç → kaydet → albüm yenilendi mi".

## Sıradaki işler (öncelik sırasıyla)
1. **Makine saat farkı düzeltme:** iki fotoğrafçı/iki makine varsa saatler kayar; makine modeline göre (EXIF) saat kaydırma, hikâye bölümleri bunu kullanır.
2. Metin ve yuvalarda hizalama kılavuzları (sayfa ortasına / birbirine yapışma).
3. Kapak ve sırt (Karar 03 sırt genişliği); JPG/PNG çıktı (Karar 11).
4. Çift için çevrimdışı HTML prova (beğeni/not, geri içe aktarma); geometrik yuvalarda elle köşe düzenleme.
5. Rötuş: Photoshop UXP paneli, hazır action paketi önerileri, rötuşçuya iş listesi (Karar 20 §1).

## Notlar
- Commit kuralı: Conventional Commits + `git commit -s` (Karar 14).
- Yeni ikon: `tools/ikonlar/lucide_to_xaml.py` içindeki listeye ekleyip `Themes/Icons.xaml`'ı yeniden üretin.
- WPF düzen tuzağı: boyutu kendi genişliğine bağlı öğeler (galeri kareleri) "Auto" kaydırma çubuklu bir alanda sonsuz düzen
  döngüsüne girer; çubuk her zaman yer ayırmalı (`VerticalScrollBarVisibility="Visible"`).
- Renk yalnızca anlamsal anahtarla (`{DynamicResource AccentBrush}` vb.); XAML/C# içinde sabit renk yazılmaz (sayfa içeriği hariç).
