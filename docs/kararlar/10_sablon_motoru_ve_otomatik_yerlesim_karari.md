1. Karar Özeti
NexAlbum'in kalbi şablon motoru ve otomatik yerleşim algoritmasıdır. Bu iki bileşen, ürünün "hızlı, doğru, çeşitli" vaadini gerçekleştirir.

Şablon motoru: Yüzde tabanlı, ölçüden bağımsız, JSON tabanlı, kullanıcı tanımlı şablonları destekler.

Otomatik yerleşim: Katmanlı algoritma; fotoğrafı esnetmez, yüz korur, çeşitlilik gözetir, kilitli sayfalara dokunmaz.

Deterministik: Aynı girdi + aynı tohum → aynı çıktı (test edilebilirlik için).

UI'dan bağımsız: Motor, ILayoutEngine arayüzü arkasında; birim testleriyle doğrulanır.

Üç mod: Hızlı (varsayılan), Dengeli (çeşitlilik), Kontrollü (parametreli).

Açıklanabilir: Her sayfa için "neden bu düzen?" sorusu cevaplanabilir (opsiyonel).

Basit başlar, gelişir: MVP'de aç gözlü algoritma; ileride daha akıllı sürümler.

Bu karar, 05 (Yol haritası) Aşama 3'ün çıktısıdır ve 07 (Rakip Özellikleri) ile doğrudan ilişkilidir.

2. Tasarım İlkeleri
2.1 İlkeler
Esnetme yok. Fotoğrafın en-boy oranı her zaman korunur.

Yüz koruma. Mümkün olduğunca yüz/ana konu kırpılmaz.

Çeşitlilik. Aynı şablon arka arkaya tekrar etmez.

Ölçeklenebilirlik. Şablon yüzde tabanlı; her ölçüde çalışır.

Determinizm. Aynı girdi + tohum = aynı çıktı.

Kilit saygısı. Kilitli sayfalara dokunulmaz.

Açıklanabilirlik. Her karar izlenebilir.

Hız. 1000 fotoğraf < 10 saniye (hedef).

Bellek dostu. Streaming, lazy evaluation.

Test edilebilir. Saf fonksiyonlar, deterministik.

2.2 Anti-ilkeler
❌ Rastgele yerleştirme (anlamsız sonuç)

❌ Fotoğraf esnetme (oran bozulması)

❌ Aynı şablon tekrarı (sıkıcı)

❌ Kilitli sayfa değişikliği (kullanıcı kaybı)

❌ Belirsiz sonuç (test edilemez)

❌ UI bağımlılığı (bakım zor)

3. Şablon Veri Modeli
3.1 Şablon yapısı (tam)
json
{
  "schemaVersion": 1,
  "id": "template-featured-1plus2",
  "name": "Öne Çıkan + İki Küçük",
  "description": "Bir büyük fotoğraf üstte, iki küçük altta",
  "category": "featured",
  "tags": ["sade", "modern", "3-foto", "öne-çıkan"],
  "orientation": "any",
  "photoCount": { "min": 3, "max": 3 },
  "recommendedOrientations": {
    "primary": ["portrait", "landscape"],
    "secondary": ["square", "landscape"]
  },
  "aspectRatio": "any",
  "slots": [
    {
      "id": "slot-main",
      "x": 0, "y": 0,
      "width": 100, "height": 65,
      "unit": "%",
      "cropMode": "cover",
      "priority": 1,
      "recommendedOrientation": ["portrait", "landscape"],
      "minAspectRatio": 0.5,
      "maxAspectRatio": 2.0
    },
    {
      "id": "slot-small-1",
      "x": 0, "y": 65,
      "width": 50, "height": 35,
      "unit": "%",
      "cropMode": "cover",
      "priority": 2,
      "recommendedOrientation": ["square", "landscape"]
    },
    {
      "id": "slot-small-2",
      "x": 50, "y": 65,
      "width": 50, "height": 35,
      "unit": "%",
      "cropMode": "cover",
      "priority": 2,
      "recommendedOrientation": ["square", "landscape"]
    }
  ],
  "textFields": [
    {
      "id": "text-date",
      "x": 5, "y": 95,
      "width": 90, "height": 5,
      "unit": "%",
      "placeholder": "{{date}}",
      "defaultFont": { "family": "Inter", "size": 10, "color": "#333333" },
      "alignment": "center"
    }
  ],
  "background": {
    "type": "solid",
    "color": "#FFFFFF"
  },
  "gutter": 0,
  "outerMargin": 0,
  "rules": {
    "preserveAspectRatio": true,
    "allowUserEdit": true,
    "minPhotos": 3,
    "maxPhotos": 3
  },
  "preview": "previews/template-featured-1plus2.jpg",
  "author": "builtin",
  "version": "1.0.0",
  "createdAt": "2025-01-15T10:00:00Z"
}
3.2 Alan açıklamaları
Alan	Tip	Zorunlu	Açıklama
schemaVersion	int	✅	Şema sürümü
id	string	✅	Benzersiz kimlik
name	string	✅	Kullanıcıya görünen ad
description	string	❌	Açıklama
category	enum	✅	Kategori (§4)
tags	array	❌	Arama/filtre etiketleri
orientation	enum	✅	portrait, landscape, square, any
photoCount	object	✅	min, max fotoğraf sayısı
recommendedOrientations	object	❌	Önerilen fotoğraf yönleri
aspectRatio	enum/string	❌	Sayfa oranı (any veya 3:2 gibi)
slots	array	✅	Fotoğraf yuvaları
textFields	array	❌	Metin alanları
background	object	✅	Arka plan
gutter	double	❌	Yuvalar arası boşluk (%)
outerMargin	double	❌	Sayfa kenarı boşluğu (%)
rules	object	✅	Kurallar
preview	string	❌	Önizleme görseli yolu
author	string	✅	builtin, user, community
version	string	✅	Şablon sürümü
3.3 Yuva tanımı
Alan	Tip	Açıklama
id	string	Benzersiz yuva kimliği
x, y	double	Konum (%)
width, height	double	Boyut (%)
unit	string	% (yüzde) veya mm
cropMode	enum	cover, contain, fill
priority	int	Öncelik (1 = en önemli)
recommendedOrientation	array	Önerilen yönler
minAspectRatio	double	Minimum en-boy oranı
maxAspectRatio	double	Maksimum en-boy oranı
shape	enum	rectangle, circle, rounded
cornerRadius	double	Yuvarlatılmış köşe (mm)
3.4 Kritik: Yüzde tabanlı ölçüler
Yuvalar yüzde ile tanımlanır (0-100).

Sayfa ölçüsü ne olursa olsun, şablon ölçeklenir.

x: 0, width: 50 = sayfanın sol yarısı.

Bu, "ölçüden bağımsız şablon" ilkesidir.

Örnek dönüşüm:

Sayfa: 300×300 mm

Yuva: x: 0, y: 0, width: 50, height: 50 (%)

Sonuç: x: 0, y: 0, width: 150, height: 150 mm

3.5 Gutter ve outer margin
gutter: Yuvalar arası boşluk.

outerMargin: Sayfa kenarı boşluğu.

İkisi de yüzde; sayfa ölçüsüne göre ölçeklenir.

Yuvalar bu boşlukları otomatik hesaba katar.

4. Şablon Kategorileri
4.1 Kategori listesi
Kategori	Açıklama	Fotoğraf sayısı
single	Tek büyük fotoğraf / tam sayfa	1
featured	Bir büyük + küçükler	2-4
grid	Eşit ızgara	4, 6, 9, 12
collage	Asimetrik kolaj	3-8
fullBleed	Kenardan kenara	1-3
minimal	Boşluk ağırlıklı	1-2
text	Fotoğraf + metin	1-3
panorama	Çift sayfaya yayılan	1
portrait	Portre ağırlıklı	2-4
landscape	Manzara ağırlıklı	2-4
square	Kare fotoğraf ızgarası	4, 9
frame	Çerçeveli / polaroid	1-4
background	Arka plan fotoğrafı + küçükler	2-5
cover	Kapak	1-2
spine	Sırt	0-1
custom	Kullanıcı tanımlı	değişken
4.2 Şablon aileleri (doküman 4.1)
Orijinal dokümanda belirtilen 17 aile:

Tek büyük fotoğraf / tam sayfa → single, fullBleed

Bir büyük + bir küçük → featured

İki fotoğraf üst üste → featured, collage

Üç fotoğraf: bir büyük, iki küçük → featured

Dört eşit fotoğraf → grid

Dergi tarzı asimetrik kolaj → collage

Kenardan kenara (full bleed) → fullBleed

Beyaz/boşluk ağırlıklı minimal → minimal

Fotoğraf + başlık/tarih/metin → text

Panorama / çift sayfaya yayılan → panorama

Portre ağırlıklı → portrait

Manzara ağırlıklı → landscape

Kare fotoğraf ızgarası → square

Çerçeveli / polaroid → frame

Arka plan fotoğrafı + küçükler → background

Kapak, arka kapak, sırt → cover, spine

Kullanıcı tanımlı → custom

4.3 MVP şablon hedefi
Minimum: 10 farklı şablon.

Hedef: 20-30 şablon.

Kategoriler: En az 8 kategori.

Varyasyon: Her ailede 2-3 varyasyon.

4.4 Şablon üretim stratejisi
Elle yazılan: Yerleşik şablonlar (JSON).

Şablon jeneratörü (ileride): Parametreli üretim (örn. "N eşit ızgara").

Kullanıcı tanımlı: v1.1'de kullanıcı şablon kaydetme.

5. Otomatik Yerleşim Algoritması
5.1 Genel akış
text
Girdi:
  - Fotoğraf listesi (seçili/sıralı)
  - Albüm profili (ölçü, bleed, gutter)
  - Hedef sayfa sayısı (veya otomatik)
  - Parametreler (mod, min/max fotoğraf, stil)
  - Kilitli sayfalar

Aşamalar:
  1. Ön işleme (fotoğraf sınıflandırma)
  2. Gruplama (sayfa başına fotoğraf dağıtımı)
  3. Şablon seçimi (her grup için)
  4. Yuva eşleştirme (fotoğraf → yuva)
  5. Çeşitlilik kontrolü (şablon tekrarını azalt)
  6. Kilit entegrasyonu (kilitli sayfaları koru)
  7. Doğrulama (esnetme, çakışma, boşluk)

Çıktı:
  - Sayfa listesi (her sayfa: şablon + yerleşimler)
  - Uyarılar (yetersiz fotoğraf, tekrar, düşük DPI)
  - Açıklamalar (opsiyonel)
5.2 Algoritma katmanları
text
┌─────────────────────────────────────────────┐
│  ILayoutEngine (arayüz)                     │
├─────────────────────────────────────────────┤
│  PhotoClassifier    → Fotoğraf sınıflandırma │
│  PageGrouper        → Sayfa gruplama         │
│  TemplateSelector   → Şablon seçimi          │
│  SlotMatcher        → Yuva eşleştirme        │
│  DiversityController→ Çeşitlilik kontrolü    │
│  LockIntegrator     → Kilit entegrasyonu     │
│  Validator          → Doğrulama              │
└─────────────────────────────────────────────┘
Her katman bağımsız ve test edilebilir.

6. Aşama 1 — Fotoğraf Sınıflandırma
6.1 Amaç
Her fotoğrafı özelliklerine göre etiketle.

6.2 Sınıflandırma boyutları
Boyut	Değerler	Kaynak
Yön	portrait, landscape, square	En-boy oranı
En-boy oranı	double (örn. 1.5)	width/height
Çözünürlük	low, medium, high	Piksel + DPI
Yüz var mı?	true, false, unknown	(opsiyonel, ileride)
Benzerlik grubu	group-1, group-2...	(opsiyonel, ileride)
Çekim zamanı	ISO 8601	EXIF
Favori	true, false	Kullanıcı
6.3 Yön sınıflandırması
csharp
public static PhotoOrientation Classify(double width, double height)
{
    var ratio = width / height;
    if (ratio > 1.15) return PhotoOrientation.Landscape;
    if (ratio < 0.87) return PhotoOrientation.Portrait;
    return PhotoOrientation.Square;
}
Eşikler:

> 1.15 → yatay (landscape)

< 0.87 → dikey (portrait)

Arası → kare (square)

6.4 Çözünürlük sınıflandırması
text
effectiveDPI = (photoWidth / slotWidth_mm) * 25.4
DPI	Sınıf
< 150	low (uyarı)
150-250	medium
250-350	high
> 350	veryHigh
6.5 Yüz algılama (opsiyonel, ileride)
MVP'de yok.

v1.1'de: OpenCV veya ML.NET ile basit yüz algılama.

Amaç: Kırpma sırasında yüzü koruma.

Vaat edilmez: "Yüz her zaman korunur" denmez.

Uyarı verilir: "Bu kırpma yüzü kesebilir."

6.6 Benzerlik gruplama (opsiyonel, ileride)
MVP'de yok.

v1.2'de: Perceptual hash ile benzer fotoğraf gruplama.

Amaç: Aynı sahnenin tekrarını önlemek.

Kullanıcı manuel de yapabilir.

7. Aşama 2 — Sayfa Gruplama
7.1 Amaç
Fotoğrafları sayfalara dağıt.

7.2 Girdi parametreleri
minPhotosPerPage (varsayılan: 1)

maxPhotosPerPage (varsayılan: 6)

targetPageCount (kullanıcı verirse)

mode (quick, balanced, controlled)

7.3 Algoritma
text
totalPhotos = N
if targetPageCount verilmiş:
    photosPerPage = N / targetPageCount
else:
    photosPerPage = ortalama (min + max) / 2

sayfaGruplari = []
kalan = N
while kalan > 0:
    buSayfa = min(maxPhotosPerPage, kalan)
    if kalan - buSayfa < minPhotosPerPage and kalan > minPhotosPerPage:
        buSayfa = kalan - minPhotosPerPage  // tek fotoğraf kalmasın
    sayfaGruplari.ekle(buSayfa)
    kalan -= buSayfa
7.4 Örnek dağıtım
Girdi: 10 fotoğraf, min 1, max 4.
Çıktı: [4, 4, 2] veya [3, 3, 4] gibi dengeli dağılım.

Girdi: 7 fotoğraf, min 2, max 4.
Çıktı: [4, 3] (tek kalan olmasın).

7.5 Modlara göre dağıtım
Mod	Strateji
Hızlı	Dengeli dağıtım, varsayılan parametreler
Dengeli	Çeşitlilik için farklı boyutlar (2, 3, 4 karışık)
Kontrollü	Kullanıcı parametreleri kesin
7.6 Yetersiz fotoğraf
Fotoğraf sayısı hedef sayfa sayısına yetmezse:

Uyarı: "X fotoğraf, Y sayfa için yetersiz."

Seçenek: "Sayfa azalt" veya "Boş yuvaları kabul et."

Boş yuvalar açıkça gösterilir (gri placeholder).

8. Aşama 3 — Şablon Seçimi
8.1 Amaç
Her sayfa grubu için uygun şablonu seç.

8.2 Filtreleme
Şablonlar şu kriterlere göre filtrelenir:

Fotoğraf sayısı: min <= N <= max.

Yön uyumu: Şablon orientation = any veya grubun çoğunluğu ile uyumlu.

Sayfa oranı: Şablon aspectRatio = any veya profil ile uyumlu.

Kategori: Kullanıcı seçimi (örn. "sade").

Kullanılabilirlik: Aktif şablonlar.

8.3 Puanlama
Her aday şablon puanlanır:

text
score = w1 * orientationMatch
      + w2 * aspectRatioMatch
      + w3 * diversityBonus
      + w4 * userPreference
      + w5 * styleMatch
Ağırlıklar:

w1 (yön uyumu): 0.3

w2 (oran uyumu): 0.2

w3 (çeşitlilik): 0.3

w4 (kullanıcı tercihi): 0.1

w5 (stil uyumu): 0.1

8.4 Çeşitlilik bonusu
Son N sayfada kullanılan şablonlar ceza alır.

Farklı kategorideki şablonlar bonus alır.

Örnek: Son 3 sayfada featured kullanıldıysa, yeni grid bonus alır.

8.5 Seçim
En yüksek puanlı şablon seçilir.

Eşitlik durumunda: Rastgele (tohum ile deterministik).

Kullanıcı tercihi varsa: Ona uygun olanlar öncelikli.

8.6 Modlara göre seçim
Mod	Strateji
Hızlı	En yüksek puan, hızlı
Dengeli	Çeşitlilik ağırlığı yüksek
Kontrollü	Kullanıcı seçtiği şablonlar
9. Aşama 4 — Yuva Eşleştirme
9.1 Amaç
Gruptaki her fotoğrafı, şablondaki en uygun yuvaya ata.

9.2 Problem
N fotoğraf, M yuva (N = M genelde).

Her fotoğrafın yönü, oranı farklı.

Her yuvanın oranı, önceliği farklı.

Amaç: En iyi eşleşme (maliyet minimize).

9.3 Maliyet fonksiyonu
text
cost(photo, slot) = 
    |photo.aspectRatio - slot.aspectRatio| * 0.5
  + orientationMismatch(photo, slot) * 0.3
  + (slot.priority == 1 ? -0.1 : 0)  // öncelikli yuvaya iyi fotoğraf
  + cropLoss(photo, slot) * 0.2
9.4 Algoritma seçenekleri
MVP: Aç gözlü (greedy) yaklaşım.

Her fotoğraf için en düşük maliyetli yuvayı seç.

Basit, hızlı.

Optimal değil ama yeterli.

İleride: Macar algoritması (Hungarian).

Optimal eşleşme.

Karmaşık, yavaş.

Büyük gruplarda (N > 6) yavaş.

Karar: MVP'de aç gözlü. İleride Macar.

9.5 Aç gözlü algoritma
text
for each photo in sortedByPriority(photos):
    bestSlot = null
    bestCost = infinity
    for each slot in availableSlots:
        c = cost(photo, slot)
        if c < bestCost:
            bestCost = c
            bestSlot = slot
    assign(photo, bestSlot)
    remove(bestSlot, availableSlots)
Sıralama: Öncelikli fotoğraflar (favori, büyük) önce atanır.

9.6 Kırpma modu
Mod	Davranış
cover	Yuvayı doldur, taşanı kırp (varsayılan)
contain	Fotoğrafı sığdır, boşluk bırak
fill	Esnet (kullanılmaz, oran bozar)
Kural: cover varsayılan; fill yasak.

9.7 Kırpma merkezi
MVP: Merkez kırpma.

v1.1: Yüz algılama ile akıllı kırpma.

Kullanıcı: Manuel kırpma her zaman mümkün.

9.8 Kırpma kaybı hesabı
text
cropLoss = 1 - (kullanılanAlan / toplamAlan)
cover modunda kırpılan alan oranı.

Yüksek kayıp → uyarı.

10. Aşama 5 — Çeşitlilik Kontrolü
10.1 Amaç
Sayfa düzenlerinin tekrarlanmasını önle.

10.2 Çeşitlilik boyutları
Boyut	Kural
Şablon tekrarı	Aynı şablon son 3 sayfada kullanılamaz
Kategori tekrarı	Aynı kategori son 2 sayfada kullanılamaz
Yoğunluk	Tam sayfa ve kolaj dengeli dağıtılır
Yön	Portre/manzara dengeli dağıtılır
Öne çıkan	Her 4-5 sayfada 1 tam sayfa
10.3 Şablon tekrarı engeli
csharp
public bool CanUseTemplate(Template t, List<Page> recentPages)
{
    var lastN = recentPages.TakeLast(3);
    return !lastN.Any(p => p.TemplateId == t.Id);
}
İstisna: Yeterli şablon yoksa (örn. tek şablon) kural gevşetilir.

10.4 Kategori ritmi
Örnek ritim: featured → grid → featured → fullBleed → grid → ...

Basit kural: Aynı kategori arka arkaya gelmez.

Gelişmiş: Ritim paterni (ileride).

10.5 Yoğunluk dengesi
Yoğun sayfa: 4+ fotoğraf.

Seyrek sayfa: 1-2 fotoğraf.

Oran: ~%30 yoğun, %70 seyrek.

Kullanıcı ayarlayabilir.

10.6 Yön dengesi
Portre fotoğraflar → portre uyumlu şablonlar.

Manzara fotoğraflar → manzara uyumlu şablonlar.

Karışık → karışık şablonlar.

10.7 Öne çıkan sayfa ritmi
Her 4-5 sayfada 1 tam sayfa (single veya fullBleed).

Otomatik veya kullanıcı ayarı.

11. Aşama 6 — Kilit Entegrasyonu
11.1 Kilit nedir?
Kullanıcı bir sayfayı kilitler.

Kilitli sayfa otomatik yerleşimde değişmez.

Kilitli sayfanın fotoğrafları "kullanılmış" sayılır.

11.2 Kilit türleri
Tür	Açıklama
Sayfa kilidi	Tüm sayfa korunur
Yuva kilidi	Sadece o yuva korunur
Fotoğraf kilidi	Fotoğraf başka sayfada kullanılamaz
11.3 Kilit entegrasyonu
text
1. Kilitli sayfaları ayır
2. Kilitli sayfalardaki fotoğrafları "kullanılmış" işaretle
3. Kalan fotoğraflarla otomatik yerleşim yap
4. Kilitli sayfaları sonuç listesine geri ekle (sıralarını koru)
11.4 Yeniden oluşturma
Kullanıcı "Yeniden Oluştur" derse:

Kilitli sayfalar korunur.

Kilitli olmayanlar yeniden üretilir.

Kullanıcı "Sıfırdan Oluştur" derse:

Tüm sayfalar yeniden üretilir (kilitler yok sayılır, onay istenir).

11.5 Fotoğraf tekrarı
Bir fotoğraf başka sayfada kullanılıyorsa:

Varsayılan: Tekrar kullanılmaz.

Kullanıcı izin verirse: Tekrar kullanılabilir.

Kilitli sayfadaki fotoğraf: "kullanılmış" sayılır.

12. Aşama 7 — Doğrulama
12.1 Doğrulama kontrolleri
Kontrol	Kural
Esnetme	Hiçbir fotoğraf esnetilmemiş
Çakışma	Yuvalar üst üste binmemiş
Sınırlar	Yuvalar sayfa dışına çıkmamış
Gutter	Yuvalar gutter'a girmemiş
Safe area	Önemli içerik safe area içinde
Boş yuva	Boş yuvalar bildirilmiş
Fotoğraf tekrarı	Tekrar yok (veya izinli)
Yön uyumu	Uyumsuzluk uyarısı
12.2 Doğrulama sonucu
csharp
public class LayoutValidationResult
{
    public bool IsValid { get; }
    public List<LayoutWarning> Warnings { get; }
    public List<LayoutError> Errors { get; }
}
12.3 Otomatik düzeltme
Esnetme: Otomatik düzelt (oran koru).

Çakışma: Uyarı; kullanıcı düzeltir.

Sınır: Otomatik düzelt (içeri al).

Gutter: Otomatik düzelt (kaydır).

Diğer: Uyarı.

12.4 Başarısız doğrulama
Motor sonucu geçersizse:

Hata mesajı.

Kullanıcıya "yeniden dene" seçeneği.

Alternatif şablon öner.

13. Üretim Modları
13.1 Hızlı Mod
Amaç: En kısa yoldan albüm.

Özellikler:

Varsayılan parametreler.

Aç gözlü algoritma.

Minimum çeşitlilik kontrolü.

Hızlı sonuç.

Hedef süre: 1000 fotoğraf < 3 saniye.

13.2 Dengeli Mod (varsayılan)
Amaç: Çeşitlilik ve kalite.

Özellikler:

Çeşitlilik ağırlığı yüksek.

Kategori ritmi.

Yön dengesi.

Öne çıkan sayfa ritmi.

Hedef süre: 1000 fotoğraf < 10 saniye.

13.3 Kontrollü Mod
Amaç: Kullanıcı tam kontrol.

Özellikler:

Kullanıcı parametreleri kesin.

Sayfa başına fotoğraf sayısı sabit.

Şablon seçimi kullanıcı tercihli.

Minimum otomatik karar.

Hedef süre: Değişken (kullanıcı parametrelerine bağlı).

13.4 Mod karşılaştırması
Özellik	Hızlı	Dengeli	Kontrollü
Hız	⭐⭐⭐	⭐⭐	⭐
Çeşitlilik	⭐	⭐⭐⭐	⭐⭐
Kullanıcı kontrolü	⭐	⭐⭐	⭐⭐⭐
Kalite	⭐⭐	⭐⭐⭐	⭐⭐⭐
Basitlik	⭐⭐⭐	⭐⭐	⭐
14. Açıklanabilirlik (Opsiyonel)
14.1 Amaç
Kullanıcı "neden bu düzen?" sorusuna cevap alabilsin.

14.2 Açıklama örneği
text
Sayfa 5: 4'lü Izgara
- Sebep: 4 fotoğraf grubu, kare oranlı
- Önceki sayfa "Öne Çıkan" olduğu için çeşitlilik bonusu
- Fotoğraflar: 2 portre, 2 manzara → ızgara uygun
- Kırpma kaybı: %8 (düşük)
14.3 Gösterim
Sağ panelde "Neden?" düğmesi.

Tıklanınca açıklama paneli.

Ana arayüzü kalabalıklaştırmaz.

14.4 MVP'de yok
Açıklanabilirlik v1.1'de.

MVP'de motor kararları log'a yazar (geliştirici için).

15. Determinizm ve Tohum
15.1 Neden deterministik?
Test edilebilirlik: Aynı girdi → aynı çıktı.

Tekrarlanabilirlik: Kullanıcı aynı sonucu alır.

Hata ayıklama: Sorun tekrarlanabilir.

15.2 Tohum (seed)
Motor bir seed alır.

Rastgelelik gerektiğinde bu tohum kullanılır.

Varsayılan: Proje ID'sinden türetilir.

Kullanıcı: "Farklı sonuç" için tohumu değiştirebilir.

15.3 Deterministik rastgele
csharp
var rng = new Random(seed);
Aynı seed → aynı dizi.

Test: sabit seed kullan.

15.4 Eşzamanlılık
Paralel işlem varsa, her thread kendi RNG'sini alır.

Sonuç birleştirme deterministik.

16. Performans
16.1 Hedefler
Senaryo	Hedef
100 fotoğraf	< 1 saniye
500 fotoğraf	< 3 saniye
1000 fotoğraf	< 10 saniye
5000 fotoğraf	< 60 saniye
16.2 Optimizasyonlar
Paralel işleme: Fotoğraf sınıflandırma paralel.

Lazy evaluation: Şablon filtreleme talep üzerine.

Önbellek: Şablon puanları önbelleğe alınır.

Erken çıkış: Uygun şablon bulununca dur.

İlerleme: Kullanıcıya progress bar.

16.3 Bellek
Fotoğraf metadata'sı hafif (birkaç KB).

Şablon tanımları küçük (JSON).

Sonuç: Sadece yerleşimler (küçük).

Bellek kullanımı: < 100 MB (1000 fotoğraf için).

16.4 İptal
Uzun süren işlemler iptal edilebilir.

CancellationToken kullanılır.

İptal sonrası kısmi sonuç gösterilmez.

17. Test Stratejisi
17.1 Birim testleri
Test	Amaç
Fotoğraf sınıflandırma	Yön, oran doğru
Sayfa gruplama	Doğru dağıtım
Şablon filtreleme	Uygun şablonlar
Şablon puanlama	En iyi seçim
Yuva eşleştirme	Doğru atama
Çeşitlilik kontrolü	Tekrar yok
Kilit entegrasyonu	Kilitli sayfa korunur
Doğrulama	Hatalar tespit
Determinizm	Aynı seed → aynı sonuç
17.2 Entegrasyon testleri
Uçtan uca: 100 fotoğraf → 25 sayfa.

Kilitli sayfa senaryosu.

Yetersiz fotoğraf senaryosu.

Farklı profiller.

Farklı modlar.

17.3 Performans testleri
100, 500, 1000, 5000 fotoğraf.

Farklı şablon sayıları.

Farklı modlar.

Bellek profili.

17.4 Görsel testler
Üretilen sayfaların görsel çıktısı.

Snapshot karşılaştırma.

Manuel gözden geçirme.

17.5 Örnek test senaryoları
csharp
[Fact]
public void AutoLayout_100Photos_ShouldNotStretchAny()
{
    var photos = GeneratePhotos(100);
    var result = engine.Layout(photos, profile);
    Assert.All(result.Pages.SelectMany(p => p.Placements),
        pl => Assert.False(pl.IsStretched));
}

[Fact]
public void AutoLayout_LockedPage_ShouldNotChange()
{
    var page = CreateLockedPage();
    var result = engine.Layout(photos, profile, locked: new[] { page });
    Assert.Contains(result.Pages, p => p.Id == page.Id && p.Equivalent(page));
}

[Fact]
public void AutoLayout_SameSeed_ShouldProduceSameResult()
{
    var r1 = engine.Layout(photos, profile, seed: 42);
    var r2 = engine.Layout(photos, profile, seed: 42);
    Assert.Equal(r1, r2);
}

[Fact]
public void AutoLayout_ConsecutivePages_ShouldNotRepeatTemplate()
{
    var result = engine.Layout(photos, profile);
    for (int i = 0; i < result.Pages.Count - 3; i++)
    {
        var window = result.Pages.Skip(i).Take(3);
        var ids = window.Select(p => p.TemplateId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
18. Genişletilebilirlik
18.1 Yeni algoritma ekleme
ILayoutEngine arayüzü.

Yeni algoritma bu arayüzü uygular.

Kullanıcı seçebilir (ileride).

18.2 Yeni şablon kaynağı
ITemplateProvider arayüzü.

Yerleşik, kullanıcı, topluluk sağlayıcıları.

Öncelik sırası: Kullanıcı > Topluluk > Yerleşik.

18.3 Yeni sınıflandırıcı
IPhotoClassifier arayüzü.

Farklı algılama yöntemleri (yüz, sahne, renk).

İleride.

18.4 Eklenti (ileride)
Harici algoritma/şablon.

v2.0 sonrası.

19. Şablon Motoru ve UI İlişkisi
19.1 Ayrım
Motor: UI'dan bağımsız, saf C#.

UI: Motoru çağırır, sonucu gösterir.

Ara: ILayoutService (async wrapper).

19.2 UI'nın sorumlulukları
Parametre toplama.

İlerleme gösterme.

İptal.

Sonucu gösterme.

Kullanıcı düzeltmeleri.

19.3 Motorun sorumlulukları
Sınıflandırma.

Gruplama.

Seçim.

Eşleştirme.

Doğrulama.

19.4 Örnek çağrı
csharp
var result = await layoutService.LayoutAsync(new LayoutRequest
{
    Photos = selectedPhotos,
    Profile = currentProfile,
    Mode = LayoutMode.Balanced,
    MinPhotosPerPage = 1,
    MaxPhotosPerPage = 4,
    LockedPages = lockedPages,
    Seed = projectId.GetHashCode()
}, cancellationToken);
20. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
Kalitesiz yerleşim	Orta	Yüksek	Erken kullanıcı testi, mod seçenekleri
Yavaş performans	Orta	Orta	Paralel, lazy, önbellek
Fotoğraf esnetme	Düşük	Yüksek	Doğrulama, test
Şablon tekrarı	Orta	Orta	Çeşitlilik kontrolü
Kilitli sayfa değişir	Düşük	Yüksek	Kilit entegrasyonu, test
Bellek taşması	Düşük	Orta	Streaming, dispose
Determinizm bozulur	Orta	Orta	Seed, test
Yetersiz şablon	Orta	Orta	Minimum 10, jeneratör (ileri)
Yetersiz fotoğraf	Orta	Orta	Açık bildirim, boş yuva
UI bağımlılığı	Orta	Orta	ILayoutEngine arayüzü
Aşırı mühendislik	Orta	Orta	Basit başla, geliştir
Test edilemezlik	Düşük	Yüksek	Saf fonksiyonlar, determinizm
21. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Motor ücretsiz; ticari algoritma yok
02 — Açık Kaynak	Şablon formatı açık; topluluk katkısı
03 — Kullanıcı Profilleri	Şablonlar profil ölçüsünden bağımsız
04 — Tek Kişi	Basit başla, test edilebilir, sürdürülebilir
05 — Zaman	Aşama 3'ün çıktısı
06 — Bütçe	Ücretsiz kütüphanelerle
07 — Rakip Özellikleri	Otomatik yerleşim kalitesi hedefi
08 — Teknoloji	C#, SkiaSharp, xUnit
09 — Veri Modeli	TemplateDefinition, Slot, PhotoPlacement
22. Açık Sorular (Sonraki Kararlara Bırakılan)
Macar algoritması ne zaman? → v1.2 (performans yeterliyse)

Yüz algılama? → v1.1 (opsiyonel)

Benzerlik gruplama? → v1.2

Açıklanabilirlik? → v1.1

Şablon jeneratörü? → v1.2

Kullanıcı şablon kaydetme? → v1.1

Eklenti altyapısı? → v2.0

ML tabanlı seçim? → v2.0 (opsiyonel)

23. Özet
NexAlbum'in şablon motoru ve otomatik yerleşim algoritması:

Şablon motoru: Yüzde tabanlı, ölçüden bağımsız, JSON formatında.

Kategoriler: 16 kategori; MVP'de en az 8 kategori, 10+ şablon.

Algoritma: 7 aşamalı katmanlı yapı.

Sınıflandırma: Yön, oran, çözünürlük, EXIF.

Gruplama: Sayfa başına dengeli dağıtım.

Şablon seçimi: Puanlama + çeşitlilik bonusu.

Yuva eşleştirme: Aç gözlü algoritma (MVP), Macar (ileride).

Çeşitlilik: Şablon tekrarı engeli, kategori ritmi, yön dengesi.

Kilit: Kilitli sayfalar korunur.

Doğrulama: Esnetme, çakışma, sınır, gutter.

3 mod: Hızlı, Dengeli, Kontrollü.

Determinizm: Seed ile tekrarlanabilir.

Performans: 1000 fotoğraf < 10 saniye.

Ayrım: Motor UI'dan bağımsız; ILayoutEngine.

Genişletilebilir: Arayüzler ile yeni algoritma/şablon.

Bu karar, ürünün kalbidir. Kaliteli otomatik yerleşim, kullanıcının zamanını kurtarır ve ürünün değerini kanıtlar. Motor, tek geliştirici için sürdürülebilir, test edilebilir ve deterministik olacak şekilde tasarlanmıştır.

Onay: Bu karar, ürünün şablon ve otomatik yerleşim temelinin belgesidir ve Aşama 3'ün çıktısıdır.