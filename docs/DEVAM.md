# Kaldığımız yer (devir notu)

Son güncelleme: 13 Ekim 2026 (saat düzeltme, çerçeve düzenleme, kapak, HTML prova, eklenti paketleri). Başka bilgisayarda devam etmek için önce bunu okuyun.

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
| **Hikâye akışı (Karar 10 Rev. 2)** | Hikâye sekmesi: çekim saatinden bölümler (ara ayarlı, gün değişimi böler, küçükler birleşir), zamansal bölüm adı önerisi (Sabah, Öğle, Akşam…; çok günde «Cumartesi · Akşam», Karar 10 Rev. 3), ad/hazır ad menüsü, birleştir, galeriden «Buradan yeni bölüm»; zaman çizelgesi; galeride bölüm süzgeci ve renk çizgisi; şeritte bölüm etiketleri; «Hikâyeye göre oluştur» (her bölüm yeni sayfada, isteğe bağlı tam sayfa açılış + okunur başlık); şema v4 |
| **Hikâye filmi (Karar 21)** | Film penceresi (araç çubuğu «Film», Hikâye sekmesi, palet): biçimler 9:16 / 1:1 / 4:5 / Full HD / 2K / 4K, 24–60 kare/sn; stiller Zarif / Sinematik / Dinamik / Nostalji; geçişler çapraz, siyaha/beyaza, odak kaydırma, iris, itme, yakınlaşarak, kesme; Ken Burns; görünümler sıcak/soğuk/siyah-beyaz/sepya, vinyet, gren; açılış ve bölüm başlıkları; elle bölüm efektleri; yerel müzik klasörleri, vuruş bulma ve kesmeleri vuruşa oturtma; canlı önizleme (müzikle oynatma); FFmpeg yoksa izin isteyip indirir/kurar; MP4 dışa aktarma (çok çekirdek, ilerleme, iptal); ayarlar projede (şema v5) |

| **Fotoğraf ayarları** | Pozlama, parlaklık, kontrast, sıcaklık, doygunluk, siyah-beyaz, ±15° düzeltme, 90° döndürme; öncesi/sonrası, sıfırla, sayfadakilere uygula; tahribatsız, ekran ve PDF aynı (şema v6) |
| **Akıllı hizalama** | Metin ve yuva sürüklerken sayfa ortası, kenarlar, güvenli alan ve diğer öğelere yapışma, kılavuz çizgileri (Alt: serbest) |
| **Çerçeve düzenleme (Karar 10 Rev. 4)** | Yuva paneli «Çerçeveyi düzenle»: taşı, 8 tutamaçla boyutlandır (Shift oran), geometrik yuvalarda ve dikdörtgende köşe sürükle (dışbükey kalır), «Dikdörtgene döndür» |
| **Makine saatleri (Karar 09 Rev. 6)** | Hikâye sekmesi: her makinenin saat farkı (yaz, ±1 sa), otomatik eşitleme (çekim yoğunluğundan), galeride «başvuru yap / başvuruya eşitle»; orijinal EXIF korunur (şema v7) |
| **Kapak (Karar 03 Rev. 1)** | Profilde kapak tipi, ön/arka ölçü, sırt; şeridin başındaki «Kapak» kutusu; arka+sırt+ön tek yaprak, katlama çizgileri, iç sayfa gibi düzenleme; PDF'te `ad_kapak.pdf` (katlama işaretli); şema v8 |
| **Görüntü çıktısı (Karar 11 Rev. 1)** | Başlıktaki «Paylaş» menüsü: sayfalar ve kapak 150 DPI JPG/PNG önizleme (`ad_sayfa-001.jpg`), önizleme notu meta veride |
| **HTML prova (Karar 11 Rev. 2)** | Tek dosya, çevrimdışı; müşteri ♥ ve not, JSON geri bildirim indirir; içe aktarınca notlar sayfalara yazılır, şeritte ✎ işareti |
| **Eklenti paketleri (Karar 22)** | `.nexpaket` (yalnızca veri): film stilleri; `%LOCALAPPDATA%\NexAlbum\eklentiler`; «Paylaş» menüsünden yükle; biçim `docs/eklentiler.md`, örnek `samples/eklentiler/ornek-sinema` |

Testler: Windows ~730, Ubuntu ~610 (Ubuntu CI'da FFmpeg kurulup uçtan uca MP4 üretimi de denenir) — hepsi geçiyor. Takılan test 5 dakikada kesilir (`--blame-hang-timeout`). CI: `build.yml`, `test.yml` ve `screenshots.yml`
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
- **Film:** «Film» → FFmpeg izni ve indirme; müzik klasörü ekleyip parça seçme (BPM görünüyor mu); oynat (müzik eşleşiyor mu); 4K dışa aktarma süresi ve dosyanın telefonda/TV'de oynaması.
- **Hikâye:** gerçek bir etkinlik klasöründe «Bölümleri bul» makul mü; iki makineli çekimde «Otomatik eşitle» doğru farkı buluyor mu; açılış sayfasında başlık okunuyor mu.
- **Çerçeve kipi:** tutamaç ve köşe noktaları farklı yakınlaştırmalarda tutulabiliyor mu; Shift/Alt davranışı.
- **Kapak:** baskı evinin verdiği ölçülerle `ad_kapak.pdf` katlama işaretleri doğru yerde mi (bir deneme baskısıyla).
- **HTML prova:** dosya telefonda/başka bilgisayarda açılıyor mu; «Geri bildirimi kaydet» indiriyor mu; içe aktarınca notlar doğru sayfada mı.
- **Eklenti:** örnek paketi ZIP'leyip `.nexpaket` yapın, yükleyin; «Noir» film penceresinde görünüyor mu.
- **Konumlandırma:** yuvaya çift tık → sürükleyince fotoğraf kayıyor mu, soluk taşan kısım doğru mu; tekerlek imlecin altındaki noktaya mı yakınlaşıyor; yön tuşları panel kaydırıcısına değil fotoğrafa mı gidiyor (önce tuvale tıklayın).
- **Yazı etkileri PDF'te:** hale/gölge ekrandaki kadar yumuşak mı; kontur ince harflerde (el yazısı) kalın durmuyor mu.
- **Metinler:** el yazısı yazı tiplerinde Türkçe harfler (ğ, ş, İ, ı) PDF'te doğru mu; çift tıklayınca yazı kutusu odaklanıyor mu; sürüklerken kaymıyor mu.
- **Geometrik sayfaların PDF'i:** çapraz kenarlardaki boşluk ekranla aynı mı (baskı evine göndermeden önce bir sayfa basıp bakın).
- Photoshop CI'da yok: "Rötuş → Action'ları yükle", "Önce bir fotoğrafta dene", "Düzenleyicide aç → kaydet → albüm yenilendi mi".

## Sıradaki işler (öncelik sırasıyla)
1. **Eklenti paketleri 2. aşama (Karar 22 §4):** yazı stilleri, çerçeve stilleri, fotoğraf görünümleri (LUT/.cube), şablon paketleri; paket listesi/kaldırma penceresi; imza (Karar 15).
2. **Fotoğraf efektleri P1 (efekt belgesi):** yerel ve tahribatsız ek efektler (keskinlik, vinyet, film greni, ton eğrisi), otomatik kırpma önerisi.
3. **Sırt yazısı:** dönen metin (TextElement'e açı) — kapak sırtında albüm adı.
4. **Rötuş:** Photoshop UXP paneli, hazır action paketi önerileri, rötuşçuya iş listesi (Karar 20 §1).
5. Ürün dili: başlangıç ekranı ve örnek metinlerde etkinlikten bağımsız ifadeler (Karar 00/17 revizyonu gerekiyorsa önce sor).

## Notlar
- Commit kuralı: Conventional Commits + `git commit -s` (Karar 14).
- Yeni ikon: `tools/ikonlar/lucide_to_xaml.py` içindeki listeye ekleyip `Themes/Icons.xaml`'ı yeniden üretin.
- WPF düzen tuzağı: boyutu kendi genişliğine bağlı öğeler (galeri kareleri) "Auto" kaydırma çubuklu bir alanda sonsuz düzen
  döngüsüne girer; çubuk her zaman yer ayırmalı (`VerticalScrollBarVisibility="Visible"`).
- Renk yalnızca anlamsal anahtarla (`{DynamicResource AccentBrush}` vb.); XAML/C# içinde sabit renk yazılmaz (sayfa içeriği hariç).
