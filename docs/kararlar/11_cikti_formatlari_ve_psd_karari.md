Revizyon 2 — 13/10/2026 (çevrimdışı HTML prova)
Değişiklik: Müşteri onayı için tek dosyalık HTML prova eklendi. İstek: Hasan Güler (çift/müşteri için çevrimdışı prova, beğeni/not, geri içe aktarma).
- Dosya: "ad - prova.html"; sayfalar (ve kapak) uzun kenarı ~1600 piksel JPEG olarak dosyaya gömülür (data URI). İnternet, sunucu, dış yazı tipi ya da betik yoktur; dosya hiçbir yere veri göndermez. Açık/koyu tema, telefonda da okunur.
- Müşteri her sayfayı ♥ ile beğenir, not yazar, genel not bırakır; yazılanlar tarayıcıda saklanır. «Geri bildirimi kaydet» küçük bir JSON indirir: {format: "nexalbum-proof-feedback", version: 1, projectId, pages: [{id, liked, note}], general}.
- İçe aktarma: başka projenin dosyası reddedilir; notlar sayfa notuna "Prova: ♥ beğenildi · not" satırı olarak yazılır (önceki prova satırı değişir, tasarımcının notları kalır), genel not proje açıklamasına. Not 2000 karakterle sınırlı, denetim karakterleri atılır. Geri alınabilir. Notlu sayfalar şeritte işaretlenir, not sayfa panelinde görünür.
Değişmeyenler: prova baskı dosyası değildir.

Revizyon 1 — 13/10/2026 (kapak PDF'i ve JPG/PNG önizleme)
Değişiklik: (1) Kapak varsa baskı PDF'inin yanına tek sayfalık "ad_kapak.pdf" yazılır: TrimBox = kapak yaprağı (arka + sırt + ön), taşma payı ve kesim işaretleri iç sayfalarla aynı kural; kesim işaretleri açıksa katlama (sırt) çizgileri üstte ve altta taşma payının dışında 0,25 pt çizgiyle işaretlenir. (2) §4 JPG ve §7.1 PNG uygulandı: sayfalar ekrandaki çizimle aynı sonuçla, bitmiş ölçüye kırpılarak 150 DPI kaydedilir ("ad_sayfa-001.jpg", kapak "ad_kapak.jpg"); JPEG kalitesi proje ayarından (varsayılan 90). "Bu dosya önizleme amaçlıdır. Baskı için PDF çıktısı kullanın." notu arayüzde ve JPEG meta verisinde (açıklama) yer alır. Renk sRGB.
Değişmeyenler: baskı dosyası PDF'tir; JPG/PNG önizlemedir.

Karar Dokümanı 11 — Çıktı Formatları, PSD Analizi ve Preflight
Proje: NexAlbum
Karar no: 11
Konu: PDF, JPG, SVG, PSD ve diğer çıktı formatları; baskı öncesi kontrol (preflight)
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 03 (Kullanıcı Profilleri), 04 (Tek Kişi), 05 (Zaman), 07 (Rakip Özellikleri), 08 (Teknoloji), 09 (Veri Modeli), 10 (Şablon Motoru)

1. Karar Özeti
NexAlbum'in çıktı formatları, ücretsiz, GPL uyumlu, tek geliştirici sürdürebilir ve baskı doğruluğu yüksek olacak şekilde seçilmiştir.

Format	MVP	v1.x	v2.x	Amaç
PDF	✅	✅	✅	Baskı ana çıktısı
JPG	✅	✅	✅	Önizleme / sunum
SVG	✅	✅	✅	Düzenlenebilir vektörel
PNG	✅	✅	✅	Şeffaf önizleme
TIFF	⚠️	✅	✅	Yüksek kaliteli raster
PSD (flattened)	❌	❌	⚠️	Opsiyonel, sınırlı
PSD (layered)	❌	❌	⏳	İleride (maliyet yüksek)
CDR	❌	❌	⏳	İleride (kapalı format)
Kritik karar: PSD ve CDR çıktısı MVP ve v1.x kapsamında yoktur; ileride değerlendirilecektir. Gerekçesi §6'da detaylıdır.

2. Çıktı Felsefesi
2.1 İlkeler
Gerçek dosya üret. Sahte indirme yok; her çıktı gerçek bir dosyadır.

Doğru ölçü. PDF ölçüsü profil ile eşleşir; bleed doğru uygulanır.

Profil tabanlı. Renk, çözünürlük, kesim işaretleri profile göre.

Anlaşılır. Kullanıcı ne aldığını bilir; format farkları açıklanır.

Açık format tercih. PDF, SVG, PNG açık; PSD/CDR kapalı.

Uydurma yok. ICC yoksa "yok" denir; değer uydurulmaz.

Preflight zorunlu. Çıktı öncesi kontrol; hata/uyarı/bilgi.

Test edilebilir. Her format için doğrulama testi.

Hızlı. Makul süreler (aşağıda hedefler).

Sürdürülebilir. Bağımlılıklar minimal; GPL uyumlu.

2.2 Anti-ilkeler
❌ Yapılmayan formatı "destekleniyor" gibi gösterme

❌ Sahte çıktı düğmesi

❌ Değer uydurma (DPI, ICC, bleed)

❌ Kullanıcıyı yanıltma (JPG = baskı dosyası değil)

❌ Kapalı format bağımlılığı (CDR)

❌ Aşırı karmaşık format (layered PSD)

3. PDF — Baskı Ana Çıktısı
3.1 Amaç
Kullanıcının baskı evine göndereceği doğru ölçüde, doğru bleed'li, doğru renkli PDF dosyası.

3.2 Teknik gereksinimler
Özellik	Zorunlu	Açıklama
Sayfa ölçüsü	✅	Profil ile eşleşir (mm)
Bleed	✅	Profil değerleri (her kenar ayrı)
Crop marks	✅	Bleed dışına, standart
Registration marks	⚠️	Profil istiyorsa
Renk çubukları	⚠️	Profil istiyorsa
ICC profil gömme	⚠️	Profil varsa
Font gömme	✅	Subset
Görüntü gömme	✅	Kaynak çözünürlükte
Metadata	✅	Proje adı, tarih
Katmanlar	❌	MVP'de yok
Şifreleme	❌	MVP'de yok
PDF/A uyumu	❌	Baskı için gerekmez
PDF/X uyumu	⚠️	İleride (baskı evi isterse)
3.3 PDF türleri
Tür	Amaç	MVP
Tek sayfa PDF	Her sayfa ayrı	✅
Çift sayfa PDF	Açık albüm görünümü	✅
Karışık PDF	Kapak tek, iç çift	⚠️ İleride
Tek dosya PDF	Tüm albüm tek PDF	✅ (varsayılan)
Ayrı PDF'ler	Her sayfa ayrı dosya	✅ (opsiyonel)
3.4 Kütüphane: PDFsharp
Gerekçeler (Karar 08):

MIT lisansı (GPL uyumlu)

ICC profil desteği (CMYK JPEG byte-for-byte)

Font gömme (subset)

Vektör çizim

Ücretsiz

Crop mark çizimi: PDFsharp düşük seviye; crop marks manuel çizilir.

3.5 Crop mark standardı
text
Sayfa kenarı
    │
    │ ← bleed (3 mm)
    │
    ├─────── (crop mark başlangıcı)
    │
    │ ← offset (3 mm)
    │
    ├─────── (crop mark çizgisi, 5 mm uzunluk)
    │
Offset: Bleed'den sonra 3 mm.

Uzunluk: 5 mm.

Çizgi kalınlığı: 0.25 pt.

Renk: Registration (tüm kanallar).

Konum: Her 4 köşe.

3.6 ICC profil gömme
PDFsharp, ICC profilli JPEG'leri byte-for-byte gömer.

Ayrı ICC dosyası gömülemez (PDFsharp kısıtı).

Çözüm: Fotoğraflar ICC profilli JPEG'e dönüştürülür.

Alternatif: PDF çıktısında "ICCsiz" uyarısı; kullanıcı baskı eviyle konuşur.

3.7 Font gömme
Tüm kullanılan fontlar subset olarak gömülür.

Sistem fontu yoksa: fallback + uyarı.

Lisans kontrolü: Font GPL uyumlu olmalı.

3.8 PDF çıktı boyutu
Albüm boyutu	Tahmini PDF boyutu
20 sayfa, 100 fotoğraf	~30-80 MB
50 sayfa, 300 fotoğraf	~100-250 MB
100 sayfa, 800 fotoğraf	~300-700 MB
Sıkıştırma seçenekleri:

jpegQuality: 85 (varsayılan), 95 (yüksek), 75 (küçük)

downsampleImages: Yok (varsayılan), 300 DPI (küçük dosya)

3.9 Performans hedefleri
Albüm	Hedef süre
20 sayfa, 100 fotoğraf	< 30 saniye
50 sayfa, 300 fotoğraf	< 90 saniye
100 sayfa, 800 fotoğraf	< 5 dakika
İlerleme: Progress bar + iptal.

4. JPG — Önizleme ve Sunum
4.1 Amaç
Albümün tasarım fikrini göstermek; baskı dosyası değildir.

4.2 JPG türleri
Tür	Amaç	Çözünürlük
Sayfa tekli	Tek sayfa önizleme	150 DPI
Açık çift sayfa	İki sayfa yan yana	150 DPI
Tüm albüm (grid)	Küçük resimler	Thumbnail
Yüksek çözünürlük	Sunum/müşteri	300 DPI
Mockup	Masa üstü görünüm	150 DPI
4.3 Uyarı
Her JPG çıktısında:

"Bu dosya önizleme amaçlıdır. Baskı için PDF çıktısı kullanın."

Uyarı hem UI'da hem de dosya metadata'sında yer alır.

4.4 Kalite
Varsayılan: JPEG kalite 90.

Seçenekler: 75 (küçük), 85 (dengeli), 95 (yüksek).

Progressive: Evet (web için).

Renk: sRGB (ekran).

4.5 Naming convention
text
proje_adi_sayfa-001.jpg
proje_adi_sayfa-002.jpg
proje_adi_spread-001-002.jpg
proje_adi_onizleme.jpg
4.6 Performans
Tür	Hedef
Tek sayfa	< 2 saniye
20 sayfa	< 20 saniye
100 sayfa	< 90 saniye
5. SVG — Düzenlenebilir Vektörel Çıktı
5.1 Amaç
Başka bir tasarım programında (Illustrator, Inkscape, CorelDRAW) açılıp düzenlenebilir vektörel çıktı.

5.2 İçerik
Öğe	Temsil
Sayfa	<svg> kök
Fotoğraf	<image> (base64 gömülü)
Metin	<text> (vektörel)
Şekil	<rect>, <circle>, <path>
Arka plan	<rect>
Bleed çizgileri	<line> (opsiyonel)
Katman	<g id="...">
5.3 Kısıtlar
Fotoğraf gömme: Base64 → dosya boyutu büyük.

Alternatif: Bağlantılı (harici) → taşınabilirlik kaybı.

Varsayılan: Gömülü (taşınabilirlik öncelikli).

Uyarı: "Dosya boyutu büyük olabilir."

5.4 Font
Metin gerçek <text> olarak gömülür.

Fontlar SVG'ye gömülür (@font-face).

Font yoksa: sistem fontu referansı + uyarı.

5.5 SVG çıktısı
Albüm	Tahmini boyut
20 sayfa	~50-150 MB
50 sayfa	~150-400 MB
Küçük tutma: Fotoğraflar 300 DPI'de gömülür; daha yüksek değil.

5.6 Test
Inkscape'te aç → katmanlar doğru mu?

Illustrator'da aç (varsa) → metin düzenlenebilir mi?

Tarayıcıda aç → doğru render mı?

6. PSD — Analiz ve Karar
6.1 PSD nedir?
Adobe Photoshop'un yerel katmanlı görüntü formatıdır. Kapalı, karmaşık, dokümantasyonu sınırlı bir binary formattır.

6.2 PSD türleri
Tür	Açıklama	Zorluk
Flattened PSD	Tek katman, düz görüntü	Kolay
Layered PSD	Her öğe ayrı katman	Çok zor
Smart Object PSD	Gömülü PSB	Aşırı zor
Text Layer PSD	Düzenlenebilir metin	İmkansız (Adobe text engine)
6.3 Layered PSD maliyeti (gerçekçi)
Kod tarafı:

PSD binary format yazma: ~3000-5000 satır

Katman kayıtları, kanallar, blend mode'lar

Renk modları (RGB, CMYK, Lab)

Bit derinlikleri (8, 16, 32)

Maskeleme, vektör path, layer effects

Yazı tipi katmanları (Adobe text engine — dokümante edilmemiş)

Akıllı nesneler (gömülü PSB)

Toplam: 3-6 ay tam zamanlı geliştirme

Test tarafı:

Gerçek Photoshop'ta test gerekli

Her Photoshop sürümünde doğrulama

Adobe format değiştirirse kırılır

Test otomasyonu çok zor

Bakım tarafı:

Adobe format değiştirirse güncelleme

Hata ayıklama zor (binary)

Topluluk katkısı zor

Lisans tarafı:

Hazır kütüphaneler:

Aspose.PSD — Ticari, ~$4000+/yıl → Reddedildi

PhotoshopAPI (C++) — C# entegrasyonu zor

PSD.NET — Sadece okuma

psd-tools (Python) — Sadece okuma

Sonuç: C# için hazır yazma kütüphanesi yok.

6.4 Flattened PSD maliyeti
Kod tarafı:

Basit PSD container yazma: ~200-500 satır

Header + image data + kanal bilgisi

RLE veya ZIP sıkıştırma

Toplam: 1-2 hafta

Değer:

Kullanıcı Photoshop'ta açar → tek katman görür.

Katman düzenlemesi yapamaz.

PDF/SVG zaten aynı işi görür.

Fark: Sadece ".psd" uzantısı.

Sonuç: Değer düşük; maliyet orta.

6.5 Karşılaştırma
Seçenek	Maliyet	Değer	Karar
Layered PSD (sıfırdan)	3-6 ay	Yüksek	❌ Reddedildi
Layered PSD (Aspose)	~$4000/yıl	Yüksek	❌ Reddedildi (bütçe + GPL)
Flattened PSD	1-2 hafta	Düşük	⚠️ Ertelendi (v2.0)
PSD (Photoshop script)	1 hafta	Yüksek	⚠️ Koşullu (PS varsa)
PDF (katmanlı)	Zaten planlı	Yüksek	✅ Seçildi
SVG	Zaten planlı	Yüksek	✅ Seçildi
TIFF (katmanlı)	2-4 hafta	Orta	⚠️ Ertelendi
6.6 Alternatif: PDF katmanlı
Photoshop, katmanlı PDF açabilir. PDF spec, katmanları (OCG - Optional Content Groups) destekler.

PDFsharp ile katmanlı PDF üretilebilir (manuel çizim).

Photoshop'ta açıldığında katmanlar görünür ve düzenlenebilir.

Değer: PSD'nin ~%70'i.

Maliyet: PDF çıktısına ek ~1-2 hafta.

Karar: Katmanlı PDF v1.1'de değerlendirilir.

6.7 Alternatif: Photoshop scripting
Eğer kullanıcıda Photoshop kuruluysa:

Uygulama bir JSX scripti üretir.

Photoshop scripti çalıştırır → katmanlı PSD üretir.

Maliyet: ~1 hafta (script yazma).

Avantaj: Gerçek katmanlı PSD, Adobe motoru kullanır.

Dezavantaj: Photoshop zorunlu (paralı).

Karar: Opsiyonel özellik olarak v1.1'de değerlendirilir. Photoshop yoksa çalışmaz; uygulama bu özelliği "Photoshop bulundu" durumunda sunar.

6.8 KESİN KARAR
MVP ve v1.0: PSD yok.

v1.1:

Katmanlı PDF değerlendirilir.

Photoshop scripting (opsiyonel) değerlendirilir.

v2.0:

Flattened PSD (opsiyonel, düşük öncelikli) değerlendirilir.

Layered PSD (sıfırdan): İleride; sıfırdan yazım önerilmez (maliyet çok yüksek).

Gerekçe:

Tek geliştirici 3-6 ayını PSD'ye harcayamaz.

Hazır kütüphane yok (C# için).

Aspose bütçe ve GPL dışı.

Photoshop'ta test imkanı belirsiz.

PDF ve SVG aynı ihtiyacın %70-80'ini karşılar.

Kullanıcıya açık mesaj:

"PSD katmanlı çıktı desteklenmiyor. Bunun yerine katmanlı PDF veya SVG kullanabilirsiniz. Photoshop'ta düzenlemek için PDF'yi açabilirsiniz."

7. PNG ve TIFF
7.1 PNG
Amaç: Şeffaf önizleme, web paylaşımı.

Kayıpsız: Evet.

Şeffaflık: Evet.

Renk: sRGB.

Kullanım: Sayfa önizleme (şeffaf arka plan).

MVP: ✅ (SkiaSharp ile).

7.2 TIFF
Amaç: Yüksek kaliteli raster çıktı; baskı iş akışında.

Sıkıştırma: LZW (kayıpsız), ZIP.

Renk: RGB, CMYK (ICC profilli).

Katman: Opsiyonel (Photoshop TIFF).

MVP: ⚠️ (temel, tek katman).

v1.x: ✅ (gelişmiş).

Kütüphane: SkiaSharp (TIFF encode).

Kısıt: CMYK TIFF için ICC gerekli; SkiaSharp sınırlı. Gerekirse Magick.NET eklenir (Apache 2.0).

8. Reddedilen Formatlar
8.1 CDR (CorelDRAW)
Neden reddedildi:

Kapalı format (dokümantasyon yok).

CorelDRAW'a özel.

Yazma kütüphanesi yok.

Test için CorelDRAW gerekli.

Alternatif: PDF ve SVG; CorelDRAW bunları açar.

8.2 INDD (InDesign)
Neden reddedildi:

Kapalı format.

Adobe'ye özel.

Çok karmaşık.

Yazma kütüphanesi yok.

Alternatif: PDF ve IDML (ileride değerlendirilebilir).

8.3 AI (Illustrator)
Neden reddedildi:

Kapalı format.

Aslında PDF tabanlı; Illustrator PDF açar.

Alternatif: PDF ve SVG.

8.4 DOCX, PPTX
Neden reddedildi:

Alakasız.

Albüm için uygun değil.

9. Preflight — Baskı Öncesi Kontrol
9.1 Amaç
Çıktı öncesi tüm olası sorunları tespit et; kullanıcıyı bilgilendir.

9.2 Kontroller
#	Kontrol	Seviye
1	Sayfa ölçüsü girilmiş mi?	Hata
2	Bleed girilmiş mi?	Hata
3	Safe area girilmiş mi?	Hata
4	Gutter girilmiş mi (çift sayfa)?	Hata
5	Fotoğraf DPI hedefin altında mı?	Uyarı
6	Fotoğraf çok düşük DPI (< 150)?	Hata
7	Fotoğraf güvenli alan dışına çıkıyor mu?	Uyarı
8	Metin güvenli alan dışında mı?	Uyarı
9	Metin taşmış mı?	Uyarı
10	Boş fotoğraf yuvası var mı?	Uyarı
11	Kaynak fotoğraf bulunamıyor mu?	Hata
12	Fotoğraf yanlışlıkla tekrarlanmış mı?	Uyarı
13	Font eksik mi?	Uyarı
14	Orta cilt alanında önemli içerik?	Uyarı
15	Kapak/sırt ölçüleri doğrulanmış mı?	Uyarı
16	Renk profili eksik mi?	Uyarı
17	Profil doğrulanmış mı?	Bilgi
18	Kilitli sayfa var mı?	Bilgi
9.3 Seviye tanımları
Seviye	Anlam	Davranış
Hata	Dışa aktarmayı engeller	Kullanıcı düzeltmeli
Uyarı	Onay ile devam	"Devam et?" sorusu
Bilgi	Sadece bilgilendirme	Kullanıcı okur
9.4 Preflight sonucu
csharp
public class PreflightResult
{
    public bool HasErrors { get; }
    public bool HasWarnings { get; }
    public List<PreflightIssue> Issues { get; }
    public DateTime CheckedAt { get; }
}

public class PreflightIssue
{
    public PreflightLevel Level { get; }  // Error, Warning, Info
    public string Message { get; }
    public string? Detail { get; }
    public string? PageId { get; }
    public string? ElementId { get; }
    public string? Suggestion { get; }
}
9.5 UI gösterimi
text
┌─────────────────────────────────────────┐
│  Baskı Öncesi Kontrol                    │
├─────────────────────────────────────────┤
│  ❌ 2 hata, ⚠️ 5 uyarı, ℹ️ 3 bilgi       │
│                                          │
│  HATALAR                                 │
│  • Sayfa 5: Kaynak fotoğraf bulunamadı   │
│    → Yeniden bağla                       │
│  • Bleed değeri girilmemiş               │
│    → Profili düzenle                     │
│                                          │
│  UYARILAR                                │
│  • Sayfa 12: DPI 180 (hedef 300)         │
│    → Devam et                            │
│  ...                                     │
│                                          │
│  [ PDF Oluştur ]  [ İptal ]              │
└─────────────────────────────────────────┘
9.6 Preflight türleri
Hızlı: Sadece kritik kontroller (< 1 saniye).

Tam: Tüm kontroller (< 10 saniye).

Varsayılan: Tam.

9.7 Önbellek
Preflight sonucu sayfa değişmedikçe önbellekte.

Sayfa değişince: O sayfa için tekrar kontrol.

Tam kontrol: Kullanıcı isterse.

10. Dışa Aktarma Akışı
10.1 Genel akış
text
1. Kullanıcı "Dışa Aktar" der
2. Format seçer (PDF, JPG, SVG, PNG, TIFF)
3. Seçenekler (kalite, çözünürlük)
4. Preflight çalışır
5. Hatalar varsa: durur, kullanıcı düzeltir
6. Uyarılar varsa: onay ister
7. Geçici klasörde üretim başlar
8. İlerleme gösterilir
9. Bitti → hedefe taşı
10. Başarılı → kullanıcıya bildir
11. Başarısız → hata mesajı + kısmi dosya silinir
10.2 Atomik çıktı
Geçici klasörde üretim.

Başarılı olunca hedefe taşı.

Başarısızsa geçici silinir.

Yarım dosya asla "başarılı" görünmez.

10.3 Çoklu çıktı
Kullanıcı birden fazla format seçebilir.

Sırayla üretilir.

Her biri ayrı preflight'tan geçer.

10.4 Dosya adı
text
{proje_adı}_{format}_{tarih}.{uzantı}
Ahmet_Zeynep_Dugun_pdf_2025-01-20.pdf
Ahmet_Zeynep_Dugun_onizleme_2025-01-20.jpg
Güvenli ad: Türkçe karakter, boşluk temizlenir.

10.5 Hedef klasör
Kullanıcı seçer.

Varsayılan: proje klasörü.

Yazma izni yoksa: uyarı, yeniden seç.

10.6 İptal
Uzun süren çıktılar iptal edilebilir.

İptal sonrası: kısmi dosya silinir.

Kullanıcıya "İptal edildi" mesajı.

11. Performans ve Bellek
11.1 PDF üretimi
Albüm	Hedef	Bellek
20 sayfa	< 30 sn	< 500 MB
50 sayfa	< 90 sn	< 1 GB
100 sayfa	< 5 dk	< 2 GB
Optimizasyon:

Streaming (sayfa sayfa).

Paralel fotoğraf yükleme.

Bellek sonrası dispose.

11.2 JPG üretimi
Paralel (birden çok sayfa).

Thumbnail cache kullanımı.

Hızlı encode (SkiaSharp).

11.3 SVG üretimi
Base64 encode (bellek ağır).

Streaming (yavaş ama güvenli).

Fotoğraf boyutu sınırı.

11.4 Bellek stratejisi
Bir seferde bir sayfa render.

Fotoğraflar talep üzerine yüklenir.

LRU cache (max 1 GB).

Dispose zorunlu (SkiaSharp).

12. Test Stratejisi
12.1 Birim testleri
Test	Amaç
Crop mark konumu	Standart formül
Bleed hesabı	Profil değerleri
PDF sayfa ölçüsü	Doğru mm
ICC gömme	Byte-for-byte
Font gömme	Subset
JPG kalite	Boyut/kalite dengesi
SVG geçerlilik	Şema doğrulama
Preflight kontrolleri	Her seviye
12.2 Entegrasyon testleri
Uçtan uca: 20 sayfa → PDF.

PDF'i PDF okuyucuda aç → ölçü doğru mu?

PDF'i Illustrator/CorelDRAW'da aç → ölçü doğru mu?

SVG'yi Inkscape'te aç → düzenlenebilir mi?

JPG'yi gözle kontrol et.

12.3 Görsel testler
Snapshot karşılaştırma.

Bleed, crop mark görsel doğrulama.

Font render doğrulama.

12.4 Preflight testleri
Her kontrol için pozitif/negatif test.

Hata/uyarı/bilgi seviyeleri.

Kullanıcı onay akışı.

12.5 Performans testleri
Büyük albüm (100 sayfa, 800 foto).

Bellek profili.

İptal senaryosu.

12.6 Manuel test
Farklı PDF okuyucular (Acrobat, Foxit, Sumatra).

Farklı tasarım programları (Illustrator, CorelDRAW, Inkscape).

Farklı baskı evleri (varsa).

13. Kullanıcı Deneyimi
13.1 Dışa aktarma ekranı
text
┌─────────────────────────────────────────┐
│  Dışa Aktar                              │
├─────────────────────────────────────────┤
│  Format:                                 │
│  ☑ PDF (baskı)                          │
│  ☐ JPG (önizleme)                       │
│  ☐ SVG (düzenlenebilir)                 │
│  ☐ PNG (şeffaf)                         │
│  ☐ TIFF (yüksek kalite)                 │
│                                          │
│  Seçenekler:                             │
│  JPEG kalite: [====|====] 90            │
│  Çözünürlük:  ○ 150 DPI  ● 300 DPI      │
│  Sayfa:       ● Tümü  ○ Seçili          │
│                                          │
│  Hedef klasör: [C:\...] [Gözat]         │
│                                          │
│  [ Önizleme ]  [ Dışa Aktar ]            │
└─────────────────────────────────────────┘
13.2 İlerleme
text
┌─────────────────────────────────────────┐
│  PDF Oluşturuluyor...                    │
│  ████████████░░░░░░░  %60                │
│  Sayfa 12 / 20                           │
│  [ İptal ]                               │
└─────────────────────────────────────────┘
13.3 Başarı
text
┌─────────────────────────────────────────┐
│  ✅ Dışa Aktarma Başarılı                │
│                                          │
│  Dosya: album.pdf (45 MB)                │
│  Süre: 32 saniye                         │
│  Konum: C:\Users\...\Desktop             │
│                                          │
│  [ Klasörü Aç ]  [ Tamam ]               │
└─────────────────────────────────────────┘
13.4 Hata
text
┌─────────────────────────────────────────┐
│  ❌ Dışa Aktarma Başarısız               │
│                                          │
│  Hata: Hedef klasöre yazılamadı          │
│  Detay: Erişim reddedildi                │
│                                          │
│  Öneri: Farklı bir klasör seçin          │
│                                          │
│  [ Yeniden Dene ]  [ Kapat ]             │
└─────────────────────────────────────────┘
13.5 Format açıklaması
Her format için kısa bilgi:

PDF: "Baskı evine gönderilecek ana dosya."

JPG: "Ekran önizlemesi; baskı için değil."

SVG: "Illustrator/Inkscape'te düzenlenebilir."

PNG: "Şeffaf arka planlı önizleme."

TIFF: "Yüksek kaliteli raster; baskı iş akışı için."

14. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
PDFsharp ICC sorunu	Orta	Yüksek	Erken prototip; alternatif (Magick.NET)
PDFsharp crop mark manuel	Yüksek	Orta	Kabul edildi; formül test
Büyük PDF boyutu	Orta	Orta	Sıkıştırma seçenekleri
Yavaş PDF üretimi	Orta	Orta	Streaming, paralel
Bellek taşması	Orta	Yüksek	Streaming, dispose
Font gömme sorunu	Orta	Orta	Fallback + uyarı
SVG büyük dosya	Yüksek	Orta	Sınır, uyarı
Preflight yanlış pozitif	Orta	Düşük	Kalibrasyon, test
PSD talebi gelir	Yüksek	Orta	"PDF/SVG kullanın" mesajı
Kullanıcı PSD bekler	Orta	Orta	Açık iletişim
Test Photoshop yok	Yüksek	Orta	Ücretsiz alternatifler (GIMP, Krita)
Baskı evi uyumsuzluğu	Orta	Yüksek	Profil tabanlı; kullanıcı sorumlu
Çıktı yarıda kalır	Orta	Yüksek	Atomik çıktı, kısmi silme
15. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Tüm formatlar ücretsiz kütüphanelerle
02 — Açık Kaynak	PDF/SVG açık; PSD/CDR kapalı → ileride
03 — Kullanıcı Profilleri	PDF profil değerleriyle üretilir
04 — Tek Kişi	Layered PSD ileriye bırakıldı (maliyet)
05 — Zaman	Aşama 4'ün çıktısı
06 — Bütçe	Aspose.PSD reddedildi (~$4000)
07 — Rakip Özellikleri	PDF/SVG öncelikli; PSD opsiyonel
08 — Teknoloji	PDFsharp, SkiaSharp
09 — Veri Modeli	ExportJob yapısı
10 — Şablon Motoru	Sayfa render → çıktı
16. Açık Sorular (Sonraki Kararlara Bırakılan)
Katmanlı PDF ne zaman? → v1.1

Photoshop scripting? → v1.1 (opsiyonel)

Flattened PSD? → v2.0 (opsiyonel, düşük öncelik)

TIFF katmanlı? → v2.0

PDF/X uyumu? → Baskı evi talep ederse

IDML? → v2.0 (değerlendirme)

CMYK dönüşümü? → v1.2 (soft proof ile birlikte)

17. Özet
NexAlbum'in çıktı formatları:

PDF: Baskı ana çıktısı; PDFsharp; profil tabanlı; crop marks manuel.

JPG: Önizleme; baskı değil; uyarı zorunlu.

SVG: Düzenlenebilir vektörel; gömülü fotoğraf; Inkscape/Illustrator uyumlu.

PNG: Şeffaf önizleme.

TIFF: Yüksek kaliteli raster (temel MVP, gelişmiş v1.x).

PSD (layered): İleride — 3-6 ay geliştirme, hazır kütüphane yok, Aspose bütçe dışı.

PSD (flattened): Ertelendi — düşük değer, orta maliyet.

CDR: ileride. INDD, AI: reddedildi — kapalı format, kütüphane yok.

PSD yerine:

PDF katmanlı (v1.1) — Photoshop açar, katmanlar düzenlenebilir.

Photoshop scripting (v1.1, opsiyonel) — Photoshop varsa gerçek katmanlı PSD.

SVG — düzenlenebilir vektörel.

Preflight: Her çıktı öncesi zorunlu; hata/uyarı/bilgi.

Bu karar, "uydurma yok, doğru çıktı, sürdürülebilir" ilkesinin çıktı boyutundaki yansımasıdır. PSD gibi cazip ama pahalı formatlar ileriye bırakıldı; yerine açık ve sürdürülebilir alternatifler önerildi.

Onay: Bu karar, ürünün çıktı ve preflight temelinin belgesidir ve Aşama 4'ün çıktısıdır.