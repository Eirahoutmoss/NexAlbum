Karar Dokümanı 09 — Veri Modeli ve Proje Dosyası Formatı
Proje: NexAlbum
Karar no: 09
Konu: Domain veri modeli, .albumproj dosya formatı, şema sürümleme ve kalıcılık stratejisi
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 03 (Kullanıcı Profilleri), 04 (Tek Kişi), 05 (Zaman), 08 (Teknoloji Yığını)

Revizyon 1 — 13/10/2026 (şema v2)
Değişiklik: project.json şema sürümü 1 → **2**. Yuvaya isteğe bağlı "polygon" alanı (0–1 normalize köşeler) ve yeni "shape" değerleri (polygon, triangle, diamond, hexagon, octagon, arch, slant) eklendi; şablon dosyasındaki yuvalar da aynı alanları taşır. Ayrıntı: Karar 10 Revizyon 1.
Göç (§14): `V1ToV2PolygonSlots` — v1 dosyalarında bu alanlar yoktur; yapı değişmez, yalnızca sürüm yükselir; açılışta yedek (.bak-v1) alınır. Sürüm yükseltmenin amacı: eski uygulama v2 dosyayı "bilinmeyen biçim" hatasıyla değil, "daha yeni sürümle kaydedilmiş" uyarısıyla reddeder.
Doğrulama: köşe sayısı ≥ 3, her köşe 0–1 aralığında; şablon çokgenleri ayrıca dışbükey olmalıdır.

Revizyon 2 — 13/10/2026 (şema v3)
Değişiklik: project.json şema sürümü 2 → **3**. Metin öğesine isteğe bağlı "effect" alanı: { "kind": "shadow|glow|outline|band|emboss", "color": "#RRGGBB[AA]", "strength": 0–1 }. Yoksa etki yoktur. "letterSpacing" artık tanımlıdır: punto katı (0,1 = puntonun %10'u kadar harf arası).
Göç (§14): `V2ToV3TextEffects` — v2 dosyalarında "effect" yoktur; yapı değişmez, yalnızca sürüm yükselir; açılışta yedek (.bak-v2) alınır.
Gerekçe: Fotoğraf üstündeki yazı her zeminde okunabilsin (Karar 17 §11). Etki ölçüleri punto ile oranlıdır; ekran ve PDF aynı hesabı (`TextLayout`) kullanır.

Revizyon 3 — 13/10/2026 (şema v4)
Değişiklik: project.json şema sürümü 3 → **4**. Projeye isteğe bağlı "story": { "enabled": bool, "openers": bool, "chapters": [ { "id", "name", "start": ISO-8601 } ] }. Bölüm üyeliği saklanmaz; fotoğraf, çekim zamanından önce başlayan son bölüme aittir (zamansızlar bölümsüz, sonda).
Göç (§14): `V3ToV4Story` — v3 dosyalarında "story" yoktur (tek akış); yapı değişmez, yalnızca sürüm yükselir; açılışta yedek (.bak-v3) alınır.
Gerekçe: Hikâye akışı (Karar 10 Rev. 2). Başlangıç zamanıyla saklamak, sonradan eklenen fotoğrafların kendiliğinden doğru bölüme düşmesini sağlar.

Revizyon 4 — 13/10/2026 (şema v5)
Değişiklik: project.json şema sürümü 4 → **5**. Projeye isteğe bağlı "video": { "format", "style", "fps", "targetSeconds", "music" (yerel yol), "reduceMotion", "chapterTitles", "overrides": [ { "chapter", "transition", "look" } ] }. Yoksa varsayılanlar. Müzik dosyası pakete gömülmez.
Göç (§14): `V4ToV5Video` — yapı değişmez, yalnızca sürüm yükselir.
Gerekçe: Hikâye filmi (Karar 21 §6): aynı ayarlarla aynı video.

Revizyon 5 — 13/10/2026 (şema v6)
Değişiklik: project.json şema sürümü 5 → **6**. Yerleşim "filters" alanına isteğe bağlı "exposure" (−1…1, ±1,5 durak), "temperature" (−1…1, soğuk↔sıcak) ve "blackWhite" (bool). Ayarlar tahribatsızdır; ekran ve PDF aynı renk matrisini (Core `ColorMatrix`) uygular. PDF'te ayarlı fotoğraf ayarlanmış piksellerle yeniden kodlanır (JPEG kalite 92), ayarsız fotoğraf eskisi gibi yeniden sıkıştırılmadan gömülür.
Göç (§14): `V5ToV6PhotoAdjustments` — yapı değişmez, yalnızca sürüm yükselir.
Döndürme: yuvadaki "rotation" alanı düzeltme açısı olarak ±15° ile kullanılır; fotoğraf yuvayı kaplamaya devam eder (ImageFit, ekran ve PDF aynı hesap). Kullanıcının 90° döndürmesi fotoğrafın kendisine aittir: varlığın "orientation" (EXIF kodu 1–8) alanına işlenir ve albümün her yerinde geçerli olur; dosya yeniden okunsa bile projedeki yön esas alınır.
Gerekçe: Efekt belgesi §1 (P0) — parlaklık/kontrast/pozlama, sıcaklık/doygunluk, siyah-beyaz.

Revizyon 6 — 13/10/2026 (şema v7)
Değişiklik: project.json şema sürümü 6 → **7**. Varlığa isteğe bağlı "clockOffsetSeconds" (makine saat düzeltmesi, saniye) eklendi. "capturedAt" düzeltilmiş zamandır; makinenin kaydettiği zaman = capturedAt − clockOffsetSeconds (EXIF bilgisi kaybolmaz). Düzeltme makine modeline göre (EXIF "Model"; yoksa "Bilinmeyen makine") bütün fotoğraflara birlikte uygulanır; sonradan eklenen aynı makine fotoğrafları da aynı düzeltmeyi alır. Hikâye bölümleri, zaman sıralaması ve hikâye filmi düzeltilmiş zamanı kullanır.
Eşitleme yolları: (1) farkı elle yazmak (+1:07, −0:12, ±1 sa düğmeleri), (2) otomatik öneri — en çok fotoğraflı makine başvuru alınır, diğer makinenin çekim anları ±14 saat aralığında dakika adımlarıyla kaydırılıp en çok örtüşen fark seçilir (belirgin örtüşme yoksa öneri yapılmaz), (3) aynı anın iki fotoğrafı (galeride "başvuru yap" → "başvuruya eşitle"). Hepsi geri alınabilir.
Göç (§14): `V6ToV7CameraClocks` — yapı değişmez, yalnızca sürüm yükselir.
Gerekçe: İki fotoğrafçı/iki makine ile çekilen etkinliklerde saat farkı bölümleri karıştırır.

1. Karar Özeti
NexAlbum'in veri modeli katmanlı, sürümlenmiş, açık ve taşınabilir olacaktır.

Proje dosyası: .albumproj — ZIP tabanlı paket.

İçerik: JSON (okunabilir) + thumbnail + opsiyonel orijinal fotoğraflar.

Şema sürümü: Her dosyada schemaVersion alanı.

Migration: Eski sürümler otomatik yükseltilir.

Orijinal fotoğraf: Varsayılan olarak referans; taşınabilir pakette gömülü.

Kaydetme: Atomik (geçici klasör → hedef).

Otomatik kayıt: Arka planda, kurtarma için.

Format: JSON + System.Text.Json (Karar 08).

Doğrulama: JSON Schema (Karar 08).

Veritabanı: SQLite sadece yardımcı veri (önbellek, ayar, son açılanlar); proje verisi değil.

Bu karar, 03 (Profil şeması) ve 08 (Teknoloji) kararlarının doğal devamıdır ve 05 (Yol haritası) Aşama 1'in çıktısıdır.

2. Tasarım İlkeleri
2.1 İlkeler
İnsan okunabilir. JSON açık, formatlı, yorumlanabilir.

Sürümlenmiş. Her dosya schemaVersion taşır.

İleri uyumlu. Bilinmeyen alanlar yok sayılır, hata vermez.

Geri uyumlu. Migration mekanizması ile eski projeler açılır.

Taşınabilir. Farklı bilgisayarda açılabilir.

Atomik. Yarım kaydetme olmaz.

Kurtarılabilir. Çökme sonrası otomatik kayıt kurtarır.

Referans + gömülü mod. Kullanıcı seçer.

Şeffaf. Dosya içeriği açılabilir, incelenebilir.

Küçük. Gereksiz veri saklanmaz.

2.2 Anti-ilkeler
❌ Binary-only format (kilitli)

❌ Özel uzantı, özel araç (açılamayan)

❌ İnsan okuyamaz (şifreli, sıkıştırılmış)

❌ Sürüm bilgisi olmayan

❌ Migration'sız

❌ Veri kaybı riskli

3. Domain Veri Modeli
3.1 Ana nesneler
Nesne	Açıklama
AlbumProject	Proje kökü (albüm, sayfalar, varlıklar, ayarlar)
AlbumProfile	Baskı profili (Karar 03)
AlbumPage	Tek sayfa (ön/arka kapak ayrı)
PhotoAsset	Fotoğraf varlığı (kaynak dosya referansı + metadata)
PhotoPlacement	Fotoğrafın bir yuvaya yerleşimi
Slot	Fotoğraf yuvası (konum, boyut, kırpma)
TextElement	Metin öğesi (başlık, tarih, not)
BackgroundElement	Arka plan (renk, desen, fotoğraf)
TemplateDefinition	Şablon tanımı (JSON)
ExportJob	Dışa aktarma işi (format, hedef, durum)
PreflightResult	Preflight sonucu (hatalar, uyarılar)
3.2 İlişki diyagramı
text
AlbumProject
├── metadata (id, ad, tarih, sürüm)
├── profile (AlbumProfile referansı)
├── pages[] (AlbumPage)
│   ├── elements[] (Slot, TextElement, BackgroundElement)
│   └── locked, notes
├── assets[] (PhotoAsset)
│   └── placements[] (PhotoPlacement)
├── settings (export, save, ui)
└── history (opsiyonel: undo/redo)
3.3 Nesne detayları
Her nesne için alanlar, tipler ve zorunluluk durumu aşağıda verilmiştir.

4. AlbumProject — Proje Kökü
4.1 Yapı
json
{
  "schemaVersion": 1,
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Ahmet & Zeynep Düğün Albümü",
  "description": "30×30 layflat, 40 sayfa",
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-20T14:45:00Z",
  "appVersion": "0.1.0",
  "profileId": "kullanici-profil-01",
  "profileSnapshot": { /* tam profil JSON */ },
  "page": {
    "width": 300,
    "height": 300,
    "unit": "mm",
    "orientation": "square"
  },
  "pages": [ /* AlbumPage[] */ ],
  "assets": [ /* PhotoAsset[] */ ],
  "settings": {
    "export": { /* ExportSettings */ },
    "ui": { /* UiSettings */ },
    "save": { /* SaveSettings */ }
  },
  "history": {
    "enabled": true,
    "maxSteps": 100
  }
}
4.2 Alan açıklamaları
Alan	Tip	Zorunlu	Açıklama
schemaVersion	int	✅	Şema sürümü (şu an: 1)
id	UUID	✅	Benzersiz proje kimliği
name	string	✅	Proje adı
description	string	❌	Serbest açıklama
createdAt	ISO 8601	✅	Oluşturma zamanı
updatedAt	ISO 8601	✅	Son güncelleme
appVersion	string	✅	Oluşturan uygulama sürümü
profileId	string	✅	Kullanılan profil kimliği
profileSnapshot	object	✅	Profilin proje oluşturulduğu andaki hali
page	object	✅	Sayfa ölçüsü (hızlı erişim)
pages	array	✅	Sayfalar
assets	array	✅	Fotoğraf varlıkları
settings	object	✅	Ayarlar
history	object	❌	Geçmiş (opsiyonel)
4.3 profileSnapshot neden var?
Profil sonradan değişse bile proje eski değerleriyle açılır.

Kullanıcı "hangi profille oluşturmuştum?" sorusuna cevap bulur.

Farklı bilgisayarda profil yoksa bile proje doğru açılır.

5. AlbumPage — Sayfa
5.1 Yapı
json
{
  "id": "page-001",
  "index": 0,
  "type": "content",
  "spreadPosition": "single",
  "locked": false,
  "notes": "",
  "background": {
    "type": "solid",
    "color": "#FFFFFF",
    "opacity": 1.0
  },
  "elements": [
    { /* Slot[] */ },
    { /* TextElement[] */ },
    { /* BackgroundElement[] */ }
  ],
  "templateId": "template-4-grid",
  "createdAt": "2025-01-15T10:35:00Z",
  "updatedAt": "2025-01-15T10:35:00Z"
}
5.2 Alan açıklamaları
Alan	Tip	Zorunlu	Açıklama
id	string	✅	Benzersiz sayfa kimliği
index	int	✅	Sıra (0'dan başlar)
type	enum	✅	content, frontCover, backCover, spine, title
spreadPosition	enum	❌	single, left, right (çift sayfa için)
locked	bool	✅	Kilitli mi?
notes	string	❌	Kullanıcı notu
background	object	✅	Arka plan
elements	array	✅	Öğeler (Slot, Text, Background)
templateId	string	❌	Uygulanan şablon
createdAt	ISO 8601	✅	Oluşturma
updatedAt	ISO 8601	✅	Güncelleme
5.3 Sayfa tipleri
Tip	Açıklama
content	Normal iç sayfa
frontCover	Ön kapak
backCover	Arka kapak
spine	Sırt
title	Başlık/açılış sayfası
Not: Kapak/sırt sayfaları content ile aynı yapıyı kullanır; sadece type farklıdır. Bu, motoru basitleştirir.

6. Slot — Fotoğraf Yuvası
6.1 Yapı
json
{
  "id": "slot-001",
  "type": "slot",
  "x": 20,
  "y": 20,
  "width": 260,
  "height": 260,
  "unit": "mm",
  "cropMode": "cover",
  "minDPI": 200,
  "targetDPI": 300,
  "placement": {
    "assetId": "asset-001",
    "offsetX": 0,
    "offsetY": 0,
    "scale": 1.0,
    "rotation": 0,
    "cropRect": { "x": 0, "y": 0, "width": 1, "height": 1 },
    "filters": {
      "brightness": 0,
      "contrast": 0,
      "saturation": 0
    }
  },
  "shape": "rectangle",
  "cornerRadius": 0,
  "border": { "width": 0, "color": "#000000" },
  "shadow": null,
  "zIndex": 0
}
6.2 Alan açıklamaları
Alan	Tip	Zorunlu	Açıklama
id	string	✅	Benzersiz yuva kimliği
type	string	✅	"slot"
x, y	double	✅	Konum (mm)
width, height	double	✅	Boyut (mm)
unit	enum	✅	mm, cm, inch, px
cropMode	enum	✅	cover, contain, fill
minDPI	int	❌	Alt sınır DPI (uyarı)
targetDPI	int	❌	Hedef DPI
placement	object	❌	Fotoğraf yerleşimi (boş olabilir)
shape	enum	❌	rectangle, circle, rounded [Rev. 1: + polygon, triangle, diamond, hexagon, octagon, arch, slant]
polygon	array	❌	[Rev. 1, şema v2] Köşeler, yuva kutusu içinde 0–1
cornerRadius	double	❌	Yuvarlatılmış köşe
border	object	❌	Çerçeve
shadow	object	❌	Gölge
zIndex	int	✅	Katman sırası
6.3 placement — Fotoğraf yerleşimi
Alan	Tip	Açıklama
assetId	string	Hangi fotoğraf (PhotoAsset.id)
offsetX, offsetY	double	Kaydırma (0-1 normalize)
scale	double	Ölçek (1.0 = tam sığdır)
rotation	double	Döndürme (derece)
cropRect	object	Kırpma dikdörtgeni (0-1 normalize)
filters	object	Basit renk ayarları
Kritik: Bu değerler tahribatsızdır. Orijinal fotoğraf dosyasına dokunulmaz.

7. PhotoAsset — Fotoğraf Varlığı
7.1 Yapı
json
{
  "id": "asset-001",
  "sourcePath": "C:\\Photos\\Wedding\\IMG_0001.jpg",
  "sourceType": "reference",
  "fileName": "IMG_0001.jpg",
  "fileSize": 5242880,
  "fileHash": "sha256:abc123...",
  "width": 6000,
  "height": 4000,
  "orientation": 1,
  "capturedAt": "2024-09-15T14:30:00Z",
  "importedAt": "2025-01-15T10:40:00Z",
  "cameraModel": "Canon EOS R5",
  "lens": "RF 24-70mm F2.8",
  "iso": 400,
  "focalLength": 50,
  "thumbnail": {
    "path": "thumbnails/asset-001.jpg",
    "width": 256,
    "height": 171
  },
  "favorite": true,
  "tags": ["dugün", "favori"],
  "usedInPages": ["page-001", "page-005"],
  "missing": false
}
7.2 Alan açıklamaları
Alan	Tip	Zorunlu	Açıklama
id	string	✅	Benzersiz varlık kimliği
sourcePath	string	✅	Kaynak dosya yolu
sourceType	enum	✅	reference, embedded
fileName	string	✅	Dosya adı
fileSize	long	❌	Boyut (byte)
fileHash	string	❌	SHA-256 (bütünlük kontrolü)
width, height	int	✅	Piksel ölçüsü
orientation	int	❌	EXIF yönü (1-8)
capturedAt	ISO 8601	❌	EXIF çekim tarihi
importedAt	ISO 8601	✅	İçe aktarma zamanı
cameraModel	string	❌	Kamera
lens	string	❌	Lens
iso	int	❌	ISO
focalLength	double	❌	Odak uzaklığı
thumbnail	object	✅	Thumbnail bilgisi
favorite	bool	✅	Favori mi?
tags	array	❌	Etiketler
usedInPages	array	✅	Kullanıldığı sayfalar
missing	bool	✅	Dosya bulunamıyor mu?
7.3 sourceType
Tür	Açıklama
reference	Fotoğraf orijinal konumunda (varsayılan)
embedded	Fotoğraf projeye gömülü (taşınabilir)
7.4 Eksik dosya stratejisi
Açılışta tüm sourcePath kontrol edilir.

Bulunamayanlar missing: true işaretlenir.

Kullanıcıya liste gösterilir.

"Yeniden bağla" ile yeni yol atanır.

Eksik dosya varsa preflight hata verir.

8. TextElement — Metin Öğesi
8.1 Yapı
json
{
  "id": "text-001",
  "type": "text",
  "x": 20,
  "y": 280,
  "width": 260,
  "height": 15,
  "unit": "mm",
  "content": "15 Eylül 2024",
  "font": {
    "family": "Inter",
    "size": 12,
    "weight": 400,
    "italic": false,
    "color": "#333333"
  },
  "alignment": "center",
  "verticalAlignment": "middle",
  "lineHeight": 1.2,
  "letterSpacing": 0,
  "zIndex": 10
}
8.2 Alan açıklamaları
Alan	Tip	Zorunlu	Açıklama
id	string	✅	Benzersiz kimlik
type	string	✅	"text"
x, y, width, height	double	✅	Konum/boyut
content	string	✅	Metin içeriği
font	object	✅	Font bilgisi
alignment	enum	✅	left, center, right, justify
verticalAlignment	enum	✅	top, middle, bottom
lineHeight	double	❌	Satır yüksekliği
letterSpacing	double	❌	Harf aralığı
zIndex	int	✅	Katman
8.3 Font yönetimi
Gömülü font: Projeye gömülebilir (taşınabilirlik için).

Sistem fontu: Kullanıcı sisteminde varsa kullanılır.

Eksik font: Preflight uyarısı; fallback font.

9. BackgroundElement — Arka Plan Öğesi
9.1 Yapı
json
{
  "id": "bg-001",
  "type": "background",
  "mode": "solid",
  "color": "#F5F5F5",
  "gradient": null,
  "image": null,
  "opacity": 1.0,
  "zIndex": -100
}
9.2 Modlar
Mod	Açıklama
solid	Tek renk
gradient	Renk geçişi
image	Fotoğraf/desen
transparent	Şeffaf
10. TemplateDefinition — Şablon
10.1 Yapı
json
{
  "schemaVersion": 1,
  "id": "template-4-grid",
  "name": "4'lü Izgara",
  "category": "grid",
  "tags": ["sade", "kolaj", "4-foto"],
  "orientation": "any",
  "photoCount": { "min": 4, "max": 4 },
  "slots": [
    { "id": "s1", "x": 0, "y": 0, "width": 50, "height": 50, "unit": "%" },
    { "id": "s2", "x": 50, "y": 0, "width": 50, "height": 50, "unit": "%" },
    { "id": "s3", "x": 0, "y": 50, "width": 50, "height": 50, "unit": "%" },
    { "id": "s4", "x": 50, "y": 50, "width": 50, "height": 50, "unit": "%" }
  ],
  "textFields": [],
  "background": { "type": "solid", "color": "#FFFFFF" },
  "rules": {
    "preserveAspectRatio": true,
    "allowUserEdit": true
  },
  "preview": "previews/template-4-grid.jpg",
  "author": "builtin",
  "version": "1.0.0"
}
10.2 Kritik nokta: Yüzde tabanlı ölçüler
Şablon yuvaları yüzde (%) ile tanımlanır.

Sayfa ölçüsü ne olursa olsun, şablon ölçeklenir.

Bu, "ölçüden bağımsız şablon" ilkesidir (Karar 07).

10.3 Şablon kategorileri
Kategori	Açıklama
single	Tek büyük fotoğraf
grid	Eşit ızgara
collage	Asimetrik kolaj
featured	Bir büyük + küçükler
fullBleed	Kenardan kenara
minimal	Boşluk ağırlıklı
cover	Kapak
spine	Sırt
custom	Kullanıcı tanımlı
11. ExportJob — Dışa Aktarma İşi
11.1 Yapı
json
{
  "id": "export-001",
  "format": "pdf",
  "targetPath": "C:\\Users\\Ahmet\\Desktop\\album.pdf",
  "profileId": "kullanici-profil-01",
  "options": {
    "pages": "all",
    "spreadOutput": false,
    "includeCropMarks": true,
    "includeBleed": true,
    "jpegQuality": 95,
    "colorProfile": "CoatedFOGRA39.icc"
  },
  "status": "completed",
  "startedAt": "2025-01-20T15:00:00Z",
  "completedAt": "2025-01-20T15:00:45Z",
  "error": null,
  "outputFiles": ["C:\\Users\\Ahmet\\Desktop\\album.pdf"]
}
11.2 Durumlar
Durum	Açıklama
pending	Kuyrukta
running	Çalışıyor
completed	Tamamlandı
failed	Hata
cancelled	İptal
12. .albumproj Dosya Formatı
12.1 Genel yapı
.albumproj bir ZIP dosyasıdır. Uzantı özel ama içerik standart ZIP'tir.

text
proje.albumproj (ZIP)
├── manifest.json
├── project.json
├── profiles.json
├── templates.json
├── assets/
│   ├── thumbnails/
│   │   ├── asset-001.jpg
│   │   └── asset-002.jpg
│   ├── previews/           (opsiyonel)
│   │   └── asset-001.jpg
│   └── originals/          (sadece taşınabilir mod)
│       ├── asset-001.jpg
│       └── asset-002.jpg
├── fonts/                  (opsiyonel)
│   └── Inter-Regular.ttf
├── checksums.json
└── README.txt
12.2 Dosya rolleri
Dosya	Amaç
manifest.json	Şema sürümü, paket bilgisi, checksum
project.json	AlbumProject verisi
profiles.json	Kullanılan profiller
templates.json	Kullanılan şablonlar (gömülü)
assets/thumbnails/	Küçük önizlemeler
assets/previews/	Orta boy önizlemeler (opsiyonel)
assets/originals/	Orijinal fotoğraflar (taşınabilir mod)
fonts/	Gömülü fontlar
checksums.json	Bütünlük kontrolü
README.txt	Açıklama (elle açan için)
12.3 manifest.json
json
{
  "format": "nexalbum-project",
  "formatVersion": "1.0",
  "schemaVersion": 1,
  "createdAt": "2025-01-20T14:45:00Z",
  "appVersion": "0.1.0",
  "projectId": "550e8400-e29b-41d4-a716-446655440000",
  "projectName": "Ahmet & Zeynep Düğün Albümü",
  "portable": false,
  "checksumsFile": "checksums.json"
}
12.4 checksums.json
json
{
  "algorithm": "sha256",
  "files": {
    "project.json": "abc123...",
    "profiles.json": "def456...",
    "templates.json": "ghi789...",
    "assets/thumbnails/asset-001.jpg": "jkl012..."
  }
}
Amaç: Paket bozulması veya kurcalama tespiti.

12.5 README.txt
Paketi elle açan kullanıcı için kısa açıklama:

text
Bu dosya NexAlbum projesidir.
- Düzenlemek için: NexAlbum uygulamasını açın ve bu dosyayı seçin.
- Elle incelemek için: ZIP olarak açın.
- Daha fazla bilgi: https://github.com/.../nexalbum
13. Referans Modu vs Taşınabilir Mod
13.1 Referans modu (varsayılan)
Özellikler:

Fotoğraflar orijinal konumunda kalır.

.albumproj içinde sadece yol + thumbnail.

Dosya boyutu küçük.

Hızlı kaydetme.

Dezavantaj:

Fotoğraf taşınırsa/silinirse proje kırılır.

Farklı bilgisayarda açılırsa fotoğraflar bulunamaz.

Kullanım: Aynı bilgisayarda çalışan kullanıcı.

13.2 Taşınabilir mod
Özellikler:

Fotoğraflar .albumproj içine kopyalanır.

Her yerde açılır.

Paylaşılabilir.

Dezavantaj:

Dosya boyutu büyük (GB olabilir).

Kaydetme yavaş.

Disk alanı kullanımı.

Kullanım: Başka bilgisayara taşıma, arşivleme, paylaşım.

13.3 Geçiş
Referans → Taşınabilir: "Taşınabilir yap" menüsü.

Taşınabilir → Referans: "Referansa dön" menüsü (fotoğraflar dışarı çıkarılır).

Kullanıcı seçer, varsayılan referans.

13.4 Karma mod (ileride)
Bazı fotoğraflar referans, bazıları gömülü.

Şu an planlanmıyor; basitlik öncelikli.

14. Şema Sürümleme ve Migration
14.1 Sürüm alanı
Her JSON dosyasında schemaVersion (int).

Şu anki sürüm: 1.

14.2 Ne zaman artırılır?
MAJOR (uyumsuz): Alan kaldırma, tip değişikliği, anlam değişikliği.

MINOR (uyumlu): Yeni opsiyonel alan.

Şema sürümü tek sayı ile ifade edilir (1, 2, 3...).

14.3 İleri uyumluluk
Yeni alanlar opsiyonel eklenir.

Eski uygulamalar yeni alanları yok sayar.

Zorunlu alan kaldırılırsa schemaVersion artar.

14.4 Geri uyumluluk (migration)
Uygulama açılışta schemaVersion'a bakar.

Düşükse migration zinciri çalışır:

text
v1 → v2 → v3 → ... → güncel
Her adım bir IMigration sınıfı:

csharp
public interface IMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    JsonNode Migrate(JsonNode input);
}
Migration başarılıysa proje açılır.

Başarısızsa: kullanıcıya "eski sürüm, açılamıyor" mesajı + orijinal dosya korunur.

14.5 Yedekleme
Migration öncesi eski dosya yedeklenir:
proje.albumproj.bak-v1.

Kullanıcı isterse eski sürümü saklar.

30 gün sonra otomatik silinir (opsiyonel).

15. Kaydetme Stratejisi
15.1 Atomik kaydetme
text
1. Geçici klasör oluştur: proje.albumproj.tmp/
2. Tüm dosyaları yaz
3. Checksum hesapla
4. ZIP oluştur: proje.albumproj.tmp
5. Mevcut dosyayı yedekle: proje.albumproj.bak
6. Geçici ZIP'i hedefe taşı (rename)
7. Yedeği sil (başarılıysa)
Kural: Yarım dosya asla "kaydedildi" olarak görünmez.

15.2 Kaydetme tetikleyicileri
Manuel: Ctrl+S.

Otomatik: Her N dakikada (varsayılan: 5).

Kritik an: Sayfa değişikliği, şablon uygulama.

Kapanış: Uygulama kapanırken.

15.3 Otomatik kayıt
Arka planda çalışır, kullanıcıyı engellemez.

Ayrı dosya: proje.albumproj.autosave.

Çökme sonrası: "Kurtarılmamış değişiklikler var, açılsın mı?"

Kullanıcı "Evet" derse otomatik kayıt yüklenir.

Ana dosya etkilenmez.

15.4 Kilit dosyası
Proje açıkken: proje.albumproj.lock oluşturulur.

İçinde: PID, kullanıcı, zaman.

İkinci örnek açılırsa: "Başka bir uygulama bu projeyi kullanıyor" uyarısı.

Çökme sonrası kilit kalırsa: kullanıcı "kilidi kaldır" der.

16. Yükleme Stratejisi
16.1 Açma akışı
text
1. ZIP aç
2. manifest.json oku → schemaVersion kontrol
3. Migration gerekli mi? → uygula
4. checksums.json oku → bütünlük doğrula
5. project.json oku → AlbumProject oluştur
6. profiles.json oku → profilleri yükle
7. templates.json oku → şablonları yükle
8. assets/thumbnails/ → önbelleğe al
9. Eksik dosya kontrolü → kullanıcıya bildir
10. Proje açıldı
16.2 Hata senaryoları
Hata	Davranış
ZIP bozuk	"Dosya açılamıyor"
manifest yok	"Geçersiz proje"
schemaVersion > uygulama	"Daha yeni sürüm, güncelleyin"
Migration başarısız	"Dönüştürülemedi, yedek korundu"
Checksum uyuşmuyor	Uyarı; kullanıcı onayıyla devam
project.json bozuk	"Proje verisi okunamadı"
Eksik fotoğraf	Liste göster; devam et
16.3 Performans
Thumbnail'lar: Anında (küçük).

Preview'lar: Arka planda.

Orijinaller: Talep üzerine.

Büyük proje (1000+ foto): Kademeli yükleme.

17. SQLite Kullanımı (Yardımcı)
17.1 Neden?
Proje verisi JSON'da (taşınabilir).

Ama bazı yardımcı veri SQLite'ta daha uygun.

17.2 SQLite'ta saklananlar
Tablo	Amaç
recent_projects	Son açılan projeler
settings	Kullanıcı ayarları
thumbnail_cache	Thumbnail indeksi (dosya yolu + hash)
preflight_history	Preflight sonuçları
error_log	Hata kayıtları
17.3 SQLite'ta olmayanlar
Proje verisi (AlbumProject).

Sayfalar, yuvalar, fotoğraflar.

Profiller (JSON dosyaları).

Şablonlar (JSON dosyaları).

İlke: Proje ile ilgili her şey .albumproj içinde; uygulama ile ilgili geçici veri SQLite'ta.

18. JSON Şema Örnekleri
18.1 project-v1.json
json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://nexalbum.dev/schemas/project-v1.json",
  "type": "object",
  "required": ["schemaVersion", "id", "name", "pages", "assets"],
  "properties": {
    "schemaVersion": { "type": "integer", "const": 1 },
    "id": { "type": "string", "format": "uuid" },
    "name": { "type": "string", "minLength": 1 },
    "pages": { "type": "array", "items": { "$ref": "#/$defs/page" } },
    "assets": { "type": "array", "items": { "$ref": "#/$defs/asset" } }
  },
  "$defs": {
    "page": { /* ... */ },
    "asset": { /* ... */ }
  }
}
18.2 Şema dosyaları
schemas/project-v1.json

schemas/profile-v1.json (Karar 03)

schemas/template-v1.json

schemas/manifest-v1.json

schemas/export-job-v1.json

18.3 Doğrulama
Her kaydetmede önce şema doğrulaması.

Geçmezse: hata mesajı, kaydetme engellenir.

Migration sonrası da doğrulama.

19. Geri Al / Yinele (Undo/Redo)
19.1 Strateji
Command pattern: Her değişiklik bir ICommand.

Yığın: Undo stack + redo stack.

Sınır: Varsayılan 100 adım.

Kalıcı: İsteğe bağlı, .albumproj içinde (opsiyonel).

19.2 Komut örnekleri
AddPageCommand

DeletePageCommand

MovePageCommand

PlacePhotoCommand

CropPhotoCommand

ApplyTemplateCommand

ChangeProfileCommand

19.3 Snapshot vs delta
Snapshot: Tüm proje kopyası (basit, bellek ağır).

Delta: Sadece değişiklik (karmaşık, hafif).

Karar: Delta (command pattern). Bellek dostu, geri alma hızlı.

19.4 Kaydetme ile ilişki
Undo/redo geçmişi proje dosyasına yazılmaz (varsayılan).

Sadece oturum içinde geçerli.

İsteğe bağlı: history alanı ile kalıcı (opsiyonel, ileride).

20. Performans ve Bellek
20.1 Proje boyutu
Küçük proje (20 sayfa, 100 foto): ~50 MB (thumbnail dahil).

Orta proje (50 sayfa, 500 foto): ~200 MB.

Büyük proje (100 sayfa, 2000 foto): ~800 MB.

Taşınabilir mod: + orijinal fotoğraf boyutu (GB).

20.2 Bellek stratejisi
Lazy loading: Sayfalar talep üzerine.

Thumbnail cache: LRU, max 500 MB.

Preview cache: LRU, max 1 GB.

Full-res: Sadece aktif düzenleme sırasında.

Dispose disiplini: SkiaSharp nesneleri using ile.

20.3 Kaydetme performansı
Incremental: Sadece değişen dosyalar.

Paralel: Thumbnail'lar paralel yazılır.

Arka plan: Kullanıcıyı engellemez.

İlerleme: Büyük kayıtlarda progress bar.

20.4 Yükleme performansı
Hedef: 1000 fotoğraf < 3 saniye (ilk açılış).

Thumbnail'lar: Paralel yükleme.

UI: Donmaz; arka planda.

21. Güvenlik
21.1 ZIP güvenliği
Path traversal engeli: ZIP içindeki ../ yolları reddedilir.

Zip bomb engeli: Aşırı sıkıştırma oranı reddedilir.

Boyut limiti: Yapılandırılabilir üst sınır.

21.2 JSON güvenliği
Derinlik limiti: Aşırı iç içe JSON reddedilir.

Boyut limiti: Dosya başına üst sınır.

Tip kontrolü: Şema doğrulama.

Kod çalıştırma yok: JSON sadece veri; kod yok.

21.3 Dosya yolu güvenliği
Göreceli yollar: Proje içinde.

Mutlak yollar: Sadece sourcePath (referans mod).

Yol normalizasyonu: .. temizlenir.

UNC yolları: Dikkatli işlenir.

21.4 Checksum
SHA-256: Dosya bütünlüğü.

Doğrulama: Açılışta (opsiyonel; büyük projede yavaş).

Uyarı: Uyuşmazsa kullanıcı bilgilendirilir.

22. Örnek Proje Dosyası (Küçük)
Aşağıda 2 sayfalık, 3 fotoğraflı minimal bir proje örneği:

22.1 manifest.json
json
{
  "format": "nexalbum-project",
  "formatVersion": "1.0",
  "schemaVersion": 1,
  "createdAt": "2025-01-20T14:45:00Z",
  "appVersion": "0.1.0",
  "projectId": "550e8400-e29b-41d4-a716-446655440000",
  "projectName": "Örnek Proje",
  "portable": false
}
22.2 project.json
json
{
  "schemaVersion": 1,
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Örnek Proje",
  "createdAt": "2025-01-20T14:45:00Z",
  "updatedAt": "2025-01-20T14:45:00Z",
  "appVersion": "0.1.0",
  "profileId": "ornek-30x30",
  "profileSnapshot": { /* ... */ },
  "page": { "width": 300, "height": 300, "unit": "mm", "orientation": "square" },
  "pages": [
    {
      "id": "page-001",
      "index": 0,
      "type": "content",
      "locked": false,
      "background": { "type": "solid", "color": "#FFFFFF" },
      "elements": [
        {
          "id": "slot-001",
          "type": "slot",
          "x": 20, "y": 20, "width": 260, "height": 260, "unit": "mm",
          "cropMode": "cover",
          "placement": { "assetId": "asset-001", "scale": 1.0 },
          "zIndex": 0
        }
      ]
    },
    {
      "id": "page-002",
      "index": 1,
      "type": "content",
      "locked": false,
      "background": { "type": "solid", "color": "#FFFFFF" },
      "elements": [
        {
          "id": "slot-002",
          "type": "slot",
          "x": 20, "y": 20, "width": 125, "height": 125, "unit": "mm",
          "cropMode": "cover",
          "placement": { "assetId": "asset-002", "scale": 1.0 },
          "zIndex": 0
        },
        {
          "id": "slot-003",
          "type": "slot",
          "x": 155, "y": 20, "width": 125, "height": 125, "unit": "mm",
          "cropMode": "cover",
          "placement": { "assetId": "asset-003", "scale": 1.0 },
          "zIndex": 0
        }
      ]
    }
  ],
  "assets": [
    {
      "id": "asset-001",
      "sourcePath": "C:\\Photos\\IMG_0001.jpg",
      "sourceType": "reference",
      "fileName": "IMG_0001.jpg",
      "width": 6000, "height": 4000,
      "importedAt": "2025-01-20T14:45:00Z",
      "thumbnail": { "path": "thumbnails/asset-001.jpg", "width": 256, "height": 171 },
      "favorite": false,
      "usedInPages": ["page-001"],
      "missing": false
    }
  ],
  "settings": {
    "export": { "jpegQuality": 95 },
    "ui": { "theme": "light" },
    "save": { "autosaveIntervalMinutes": 5 }
  }
}
23. Test Stratejisi
23.1 Birim testleri
JSON serileştirme round-trip (her nesne).

Şema doğrulama (geçerli/geçersiz örnekler).

Migration (v1 → v2 senaryoları).

Checksum hesaplama.

Yol normalizasyonu.

23.2 Entegrasyon testleri
Proje kaydet → aç → veri aynı mı?

Taşınabilir mod: fotoğraflar gömülü mü?

Referans mod: yollar korunuyor mu?

Eksik dosya senaryosu.

Bozuk ZIP senaryosu.

Migration senaryosu (eski sürüm).

23.3 Performans testleri
1000 fotoğraf yükleme süresi.

Büyük proje kaydetme süresi.

Thumbnail cache bellek kullanımı.

23.4 Örnek veri
samples/projects/minimal.albumproj

samples/projects/medium.albumproj

samples/projects/large.albumproj (büyük test)

24. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
Veri kaybı	Düşük	Yüksek	Atomik kaydetme, otomatik kayıt
Proje bozulur	Düşük	Yüksek	Checksum, yedek
Migration hatası	Orta	Yüksek	Yedek, test, rollback
Fotoğraf taşınır	Yüksek	Orta	"Yeniden bağla" özelliği
Büyük dosya boyutu	Orta	Orta	Referans modu varsayılan
Yavaş kaydetme	Orta	Orta	Incremental, paralel
Yavaş yükleme	Orta	Orta	Lazy loading
Bellek taşması	Orta	Orta	LRU cache, dispose
ZIP traversal	Düşük	Yüksek	Yol doğrulama
Bozuk JSON	Orta	Orta	Şema doğrulama, hata mesajı
Şema uyumsuzluğu	Orta	Yüksek	Migration, yedek
Kilit dosyası kalır	Orta	Düşük	"Kilidi kaldır"
Eski proje açılamıyor	Orta	Orta	Migration; başarısızsa yedek
25. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Format açık; ücretsiz araçlarla açılabilir
02 — Açık Kaynak	Şema açık; topluluk katkısına uygun
03 — Kullanıcı Profilleri	Profil JSON'u proje içine gömülür (snapshot)
04 — Tek Kişi	Basit format; sürdürülebilir
05 — Zaman	Aşama 1'in çıktısı
06 — Bütçe	SQLite, JSON, ZIP — hepsi ücretsiz
07 — Rakip Özellikleri	Taşınabilir, açık, şeffaf format
08 — Teknoloji	System.Text.Json, SQLite, JSON Schema
26. Açık Sorular (Sonraki Kararlara Bırakılan)
Undo/redo proje dosyasına yazılsın mı? → İleride

Karma mod (referans + gömülü)? → v1.2

Şema sürümü v2 ne zaman? → İhtiyaç doğunca

Bulut yedekleme? → Reddedildi (Karar 01)

Şifreleme? → İleride (müşteri albümleri için)

Proje şablonu (boş başlangıç)? → v1.1

27. Özet
NexAlbum'in veri modeli ve proje formatı:

.albumproj: ZIP tabanlı, açık, taşınabilir.

JSON: İnsan okunabilir, sürümlenmiş, şema doğrulamalı.

Şema sürümü: Her dosyada; migration mekanizması var.

Referans vs Taşınabilir: Kullanıcı seçer; varsayılan referans.

Atomik kaydetme: Yarım dosya olmaz.

Otomatik kayıt: Çökme sonrası kurtarma.

Checksum: Bütünlük kontrolü.

SQLite: Sadece yardımcı veri; proje verisi JSON'da.

Undo/redo: Command pattern; oturum içi.

Güvenlik: Path traversal, zip bomb, JSON derinlik engeli.

Performans: Lazy loading, LRU cache, incremental save.

Bu format, 03 (Profil), 08 (Teknoloji) kararlarıyla tutarlıdır. Açık, taşınabilir, sürdürülebilir bir temel oluşturur. Kullanıcı verisini kilitli tutmaz; şeffaflık ve güven sağlar.