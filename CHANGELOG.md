# Değişiklik günlüğü

Biçim [Keep a Changelog](https://keepachangelog.com/tr-TR/1.1.0/), sürümleme [SemVer](https://semver.org/lang/tr/).
Ayrıntılı gerekçeler: [`docs/kararlar/`](docs/kararlar/INDEKS.md).

## [Yayımlanmadı]

## [1.0.0] — 2026-10-14

### Eklendi
- **Kurulum paketi**: .NET gerektirmeyen tek dosyalık `NexAlbum.exe`, Türkçe kurulum programı (yönetici izni yok,
  `.albumproj` ilişkilendirmesi), taşınabilir ZIP ve SHA-256 özetleri; `v*` etiketinden otomatik GitHub sürümü,
  yerelde `tools/yayin/paketle.ps1`.
- **Pro efektler** (Karar 23): 33 geçiş (Aperture, diyafram kanatları, çift iris, elmas, netlik kaydırma, tilt-shift, derinlikten,
  ışık sızıntısı, film yanığı, flaş, projektör, Polaroid banyosu, perde, yarık, panjur, çapraz silme, mozaik, petek, mürekkep lekesi,
  paralaks, hızlı kaydırma, kart yığını, sayfa çevirme…), 4 başlık animasyonu, odak galerisi / kolaj akışı / albüm açılışı sahneleri,
  11 yeni film stili; sunumda tıklanan noktadan açılış, mercek imleci, otomatik oynatma, «Göz at» odak şeridi;
  HTML provada odak şeridi ve tam ekran gösteri. (#17)
- **Eklenti paketleri** (`.nexpaket`, Karar 22): yalnızca veri taşıyan film stili paketleri, örnek paket ve biçim belgesi. (#16)
- **Çevrimdışı HTML prova**: tek dosya; beğeni, not, JSON geri bildirim ve içe aktarma. (#15)
- **Kapak ve sırt**: arka + sırt + ön yaprak, kapak PDF'i (katlama işaretleri); sayfaları **JPG/PNG** önizleme olarak kaydetme. Şema v8. (#14)
- **Çerçeve kipi**: yuva taşıma/boyutlandırma, geometrik köşe düzenleme, hizalama kılavuzları. (#13)
- **Makine saat farkı** düzeltme ve otomatik eşitleme. Şema v7. (#12)
- **Akıllı hizalama** çizgileri ve yapışma. (#11)
- **Fotoğraf ayarları**: pozlama, parlaklık, kontrast, sıcaklık, doygunluk, siyah-beyaz, düzeltme, 90° döndürme; tahribatsız. Şema v6. (#10)
- **Hikâye filmi** ve sosyal medya klipleri: 9:16 / 1:1 / 4:5 / Full HD / 2K / 4K, müzik, vuruş bulma, izinli FFmpeg kurulumu. Şema v5. (#8)
- **Hikâye akışı**: çekim zamanından bölümler, zaman çizelgesi, açılış sayfaları. Şema v4. (#7)
- Yazı etkileri ve yuvada fotoğraf konumlandırma (#6); favoriler, sıralama, sayfalar arası taşıma (#5);
  sayfa metinleri ve gömülü albüm yazı tipleri (#4); geri al/yinele ve sayfa yönetimi (#3);
  geometrik şablonlar, biçimli çerçeveler, kitap görünümü, sunum modu (#2); premium arayüz, Gece/Fildişi temaları, komut paleti (#1).

### Değişti
- Bölüm adları etkinlikten bağımsız zamansal belirteçler oldu (Sabah, Öğle, «Cumartesi · Akşam»). (#9)

## [0.1.0]

### Eklendi
- Baskı profili, fotoğraf içe aktarma, otomatik yerleşim, `.albumproj` biçimi, ön kontrol ve baskı PDF'i.
