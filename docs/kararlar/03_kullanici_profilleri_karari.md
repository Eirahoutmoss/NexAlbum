Karar Dokümanı 03 — Kullanıcı Tanımlı Baskı Profilleri
Proje: NexAlbum
Karar no: 03
Konu: Baskı profillerinin kullanıcı tarafından tanımlanması, saklanması ve doğrulanması
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 01 (Ücretsiz), 02 (Açık Kaynak), 04 (Tek Kişi Geliştirme), 06 (Bütçe)

1. Karar Özeti
NexAlbum, baskı evi entegrasyonu yapmayacaktır. Bunun yerine:

Kullanıcı kendi baskı profilini oluşturur.

Profiller açık ve okunabilir JSON formatındadır.

Uygulama, örnek/başlangıç profilleri sunar; bunların "örnek olduğu ve baskı evi tarafından doğrulanması gerektiği" açıkça belirtilir.

Uygulama, kullanıcının baskı evinden aldığı değerleri girmesi için rehberlik eder.

Profil değerleri (bleed, safe area, gutter vb.) asla uygulama tarafından uydurulmaz.

Bilinmeyen değerler için kullanıcıdan istenir veya "doğrulanmamış" olarak işaretlenir.

Bu karar, 01 (Ücretsiz) ve 02 (Açık Kaynak) kararlarıyla tutarlıdır: baskı evi anlaşması olmadığı için entegrasyon yok; topluluk katkısına açık profil formatı var.

2. Neden Baskı Evi Entegrasyonu Yok?
2.1 Tek geliştirici gerçeği
Her baskı eviyle ayrı anlaşma, ayrı API, ayrı doğrulama süreci gerekir.

Baskı evleri küçük, dağınık, çoğu zaman API sunmaz.

Tek geliştirici bunu sürdüremez.

2.2 Ücretsiz ürün gerçeği
Baskı evi entegrasyonu genellikle ticari anlaşma gerektirir.

Ücretsiz üründe bu tür bir ortaklık modeli yok.

Baskı evleri "aracı yazılım" istemeyebilir.

2.3 Açık kaynak gerçeği
Entegrasyon kodu, baskı evi API'lerine bağımlı olur.

API değişince kod kırılır.

Topluluk katkısı zorlaşır.

2.4 Kullanıcı özgürlüğü
Kullanıcı zaten kendi baskı eviyle çalışıyor.

Baskı evinden aldığı değerleri bilir veya öğrenebilir.

Uygulama bu değerleri girmesine yardımcı olur; onun yerine karar vermez.

2.5 Doğruluk ilkesi
Yanlış bir baskı profili = mahvolmuş baskı = para ve zaman kaybı. Bu nedenle:

Uygulama kesinlikle değer uydurmaz.

Örnek değerler "örnek" olarak işaretlenir.

Kullanıcı, profilini kaydetmeden önce "bu değerlerin doğruluğunu baskı evinizle teyit ettiniz mi?" onayı verir.

3. Profil Veri Modeli
3.1 Tam şema (JSON)
json
{
  "$schema": "https://nexalbum.dev/schemas/profile-v1.json",
  "schemaVersion": 1,
  "id": "kullanici-profil-01",
  "name": "Fotoğrafçı Ahmet — 30×30 Layflat",
  "description": "Kendi stüdyom için kullandığım layflat albüm profili",
  "source": "user",
  "sourceNote": "Baskı evim XYZ Matbaa'dan alınan değerler",
  "verified": true,
  "verifiedAt": "2025-01-15T10:30:00Z",
  "verifiedBy": "Kullanıcı beyanı",
  "createdAt": "2025-01-10T09:00:00Z",
  "updatedAt": "2025-01-15T10:30:00Z",
  "version": "1.0.0",

  "page": {
    "width": 300,
    "height": 300,
    "unit": "mm",
    "orientation": "square"
  },

  "spread": {
    "enabled": false,
    "note": "Tek sayfa albüm"
  },

  "bleed": {
    "top": 3,
    "right": 3,
    "bottom": 3,
    "left": 3,
    "unit": "mm",
    "verified": true
  },

  "safeArea": {
    "top": 5,
    "right": 5,
    "bottom": 5,
    "left": 5,
    "unit": "mm",
    "verified": true
  },

  "gutter": {
    "width": 0,
    "unit": "mm",
    "note": "Layflat olduğu için orta kayıp yok",
    "verified": true
  },

  "resolution": {
    "targetDPI": 300,
    "minDPI": 200,
    "note": "Baskı evi 300 DPI öneriyor, alt sınır 200"
  },

  "color": {
    "mode": "CMYK",
    "profile": "CoatedFOGRA39.icc",
    "profileEmbedded": false,
    "note": "Profil dosyası baskı evinden alındı"
  },

  "output": {
    "cropMarks": true,
    "cropMarkOffset": 3,
    "cropMarkLength": 5,
    "registrationMarks": false,
    "colorBars": false,
    "pageInfo": true
  },

  "cover": {
    "type": "hardcover",
    "front": {
      "width": 300,
      "height": 300,
      "unit": "mm"
    },
    "back": {
      "width": 300,
      "height": 300,
      "unit": "mm"
    },
    "spine": {
      "width": 12,
      "unit": "mm",
      "calculationMethod": "manual",
      "note": "Baskı evi sayfa sayısına göre söyledi",
      "verified": true
    }
  },

  "paper": {
    "type": "matte",
    "weight": 250,
    "weightUnit": "gsm",
    "note": "Baskı evi standart kağıdı"
  },

  "binding": {
    "type": "layflat",
    "note": "Layflat cilt"
  },

  "notes": "Bu profil kişisel kullanım içindir. Başka baskı evinde kullanılmamalıdır.",

  "tags": ["layflat", "30x30", "kişisel"],

  "warnings": [
    {
      "level": "info",
      "message": "Bu profil kullanıcı tarafından tanımlanmıştır. Değerlerin doğruluğundan kullanıcı sorumludur."
    }
  ]
}
3.2 Alan açıklamaları
Alan	Zorunlu	Açıklama
schemaVersion	✅	Şema sürümü (şu an: 1)
id	✅	Benzersiz kimlik (UUID veya kısa ad)
name	✅	Kullanıcıya görünen ad
description	❌	Serbest açıklama
source	✅	user, builtin, imported, community
verified	✅	Kullanıcı doğruladı mı?
page.width/height	✅	Bitmiş sayfa ölçüsü
page.unit	✅	mm, cm, inch, px
page.orientation	✅	portrait, landscape, square
spread.enabled	✅	Açık çift sayfa mı?
bleed	✅	Her kenar ayrı (üst/sağ/alt/sol)
safeArea	✅	Her kenar ayrı
gutter.width	✅	Orta birleşim payı
resolution.targetDPI	✅	Hedef çözünürlük
color.mode	✅	RGB, CMYK, Grayscale
color.profile	❌	ICC dosya adı
output.*	❌	Kesim/registration/renk çubuğu
cover.*	❌	Kapak/sırt (varsa)
paper.*	❌	Kağıt bilgisi (bilgi amaçlı)
binding.*	❌	Cilt tipi
notes	❌	Serbest not
tags	❌	Arama/filtre için etiketler
warnings	❌	Uygulama tarafından eklenen uyarılar
3.3 Şema dosyası
JSON Schema (Draft 2020-12)

Depoda: schemas/profile-v1.json

Uygulama bu şemayla doğrulama yapar.

Şema sürümlenir; eski profiller yeni sürüme migrate edilir.

4. Profil Türleri
4.1 Kaynak bazlı sınıflandırma
Tür	source	Açıklama
Yerleşik (Built-in)	builtin	Uygulamayla gelir, "örnek" olarak işaretli
Kullanıcı (User)	user	Kullanıcı kendi oluşturdu
İçe aktarılmış (Imported)	imported	Dış kaynaktan geldi (dosya, URL)
Topluluk (Community)	community	Topluluk deposundan indirildi
4.2 Güven düzeyi
Düzey	İşaret	Anlamı
Doğrulanmamış	⚠️	Değerler örnek; baskı eviyle teyit edilmedi
Kullanıcı doğruladı	✅	Kullanıcı "doğru" dedi
Baskı evi doğruladı	🏭	Baskı evinden resmi onay (opsiyonel)
Uygulama, doğrulanmamış profille çıktı alındığında uyarı gösterir.

4.3 Kullanım bazlı sınıflandırma
Kişisel: Sadece kullanıcı kullanır.

Stüdyo: Stüdyoda paylaşılır.

Müşteri: Müşteriye özel.

Şablon: Başka profillere temel olur.

5. Yerleşik (Built-in) Profiller
5.1 İlke
Yerleşik profiller örnektir. Gerçek baskı değerleri değildir. Amaç:

Kullanıcıya başlangıç noktası sunmak.

Uygulamayı ilk açtığında boş ekranla karşılaşmaması.

Bir değerin nasıl girileceğini göstermek.

Yaygın ölçüleri tanıtmak.

5.2 Yerleşik profil listesi
Ad	Ölçü	Yön	Not
15×13 cm	150×130 mm	Landscape	Küçük albüm
15×21 cm	150×210 mm	Portrait	Klasik oran
20×20 cm	200×200 mm	Square	Kare albüm
20×30 cm	200×300 mm	Portrait	Yaygın
25×25 cm	250×250 mm	Square	Orta boy
30×30 cm	300×300 mm	Square	Büyük kare
30×20 cm	300×200 mm	Landscape	Yatay
30×30 cm layflat	300×300 mm	Square	Çift sayfa
5.3 Yerleşik profillerde değerler
Yerleşik profillerde:

source: "builtin"

verified: false

warnings alanında: "Bu örnek bir profildir. Baskı evinizden aldığınız değerlerle güncelleyin."

Bleed/safe area gibi değerler boş veya "0" olarak bırakılır.

Kullanıcı bunları doldurmadan çıktı alamaz (preflight hatası).

Kritik: Yerleşik profillerde hiçbir sayısal değer uydurulmaz. Sadece sayfa ölçüsü vardır; gerisi kullanıcıya bırakılır.

5.4 "Özel ölçü" profili
Kullanıcı boş bir profil açar.

Sayfa ölçüsünü kendisi girer.

Bleed, safe area, gutter vb. kendisi girer.

Kaydetmeden önce "Bu değerleri baskı evinizden aldınız mı?" onayı.

6. Kullanıcı Deneyimi
6.1 Yeni profil oluşturma akışı
Menü: Profiller → Yeni Profil

Başlangıç seçimi:

Boş profil

Yerleşik profilden kopyala

Mevcut profilden kopyala

JSON dosyasından içe aktar

Temel bilgiler:

Profil adı (zorunlu)

Açıklama (opsiyonel)

Etiketler (opsiyonel)

Sayfa ölçüsü:

Genişlik × Yükseklik

Birim (mm, cm, inch)

Yön (portre, yatay, kare)

Tek sayfa mı, çift sayfa mı?

Bleed (taşma payı):

Üst / Sağ / Alt / Sol (ayrı ayrı)

"Tüm kenarlara uygula" kolaylığı

"Baskı evinizden aldığınız değerleri girin" uyarısı

Safe area (güvenli alan):

Üst / Sağ / Alt / Sol

Aynı kolaylık

Gutter (orta birleşim):

Genişlik

Çift sayfa ise zorunlu

Çözünürlük:

Hedef DPI (varsayılan 300, ama uyarı: "Baskı eviniz farklı bir değer isteyebilir")

Minimum DPI (uyarı eşiği)

Renk:

Mod: RGB / CMYK / Grayscale

ICC profil dosyası (opsiyonel, dosya seçici)

Çıktı tercihleri:

Kesim işaretleri (crop marks)

Registration marks

Renk çubukları

Sayfa bilgisi

Kapak (opsiyonel):

Kapak tipi (yok, yumuşak, sert, layflat)

Ön/arka/sırt ölçüleri

Sırt hesaplama (manuel veya formül)

Kağıt ve cilt (opsiyonel, bilgi amaçlı):

Kağıt tipi, gramaj

Cilt tipi

Özet ve doğrulama:

Tüm değerler gösterilir.

"Bu değerlerin doğruluğunu baskı evinizle teyit ettiniz mi?" onay kutusu.

Kaydet.

6.2 Profil düzenleme
Mevcut profili aç → değerleri değiştir → kaydet.

Kaydederken "Bu değişiklik mevcut projeleri etkiler" uyarısı.

Sürüm numarası otomatik artar.

İsteğe bağlı: "Farklı kaydet" ile yeni profil oluştur.

6.3 Profil silme
Kullanıcı profili silebilir.

Yerleşik profil silinemez (ama gizlenebilir).

Bir profil kullanımdaysa silmeden önce uyarı.

Silme geri alınabilir (çöp kutusu, 30 gün).

6.4 Profil içe/dışa aktarma
Dışa aktar: JSON dosyası olarak kaydet.

İçe aktar: JSON dosyası seç, doğrula, kaydet.

Toplu içe aktar: ZIP içindeki birden çok profili al.

Çakışma: Aynı id varsa "üzerine yaz / farklı kaydet / iptal".

6.5 Profil kopyalama
"Yerleşikten kopyala" → yeni kullanıcı profili.

"Mevcut profilden kopyala" → yeni kullanıcı profili.

Kopyalanan profil source: "user" olur.

6.6 Profil doğrulama sihirbazı
Basit bir rehber:

"Baskı evinizden şu değerleri isteyin:" listesi

"Aldığınız değerleri buraya girin."

"Bu değerleri doğruladınız mı?" onayı

Profil kaydedilir, verified: true olur.

7. Profil Doğrulama ve Preflight İlişkisi
7.1 Preflight kontrolleri
Bir proje bir profile bağlıysa, dışa aktarma öncesi şu kontroller yapılır:

Kontrol	Önem
Sayfa ölçüsü girilmiş mi?	Hata
Bleed girilmiş mi?	Hata
Safe area girilmiş mi?	Hata
Gutter girilmiş mi (çift sayfa ise)?	Hata
Hedef DPI girilmiş mi?	Uyarı
Renk modu girilmiş mi?	Uyarı
Kapak ölçüleri girilmiş mi (kapak varsa)?	Uyarı
Sırt genişliği girilmiş mi (sert kapak ise)?	Uyarı
Profil doğrulanmış mı?	Uyarı
ICC profil dosyası mevcut mu?	Uyarı
7.2 Hata vs uyarı
Hata: Dışa aktarmayı engeller. Örneğin bleed girilmemişse PDF üretilemez.

Uyarı: Kullanıcı onayıyla devam edilebilir. Örneğin profil doğrulanmamışsa "Yine de devam et?" sorulur.

Bilgi: Sadece bilgilendirme. Örneğin "Bu profil 300 DPI öneriyor."

7.3 Profil "eksik" durumu
Bir profil eksikse:

Uygulama kullanıcıyı profili tamamlamaya yönlendirir.

Otomatik değer ataması yapmaz.

"Bilinmiyor" olarak işaretler.

Çıktı alınamaz.

8. Sırt Genişliği Hesaplama
8.1 İlke
Sırt genişliği baskı evine, kağıda ve sayfa sayısına bağlıdır. Uygulama formül uydurmaz.

8.2 Seçenekler
Manuel giriş: Kullanıcı baskı evinden aldığı değeri girer.

Formül (kullanıcı tanımlı): Kullanıcı kendi formülünü girer (ileride).

Tablo: Baskı evi tablo verirse, kullanıcı sayfa sayısına göre seçer (ileride).

8.3 Uygulama davranışı
Varsayılan: Manuel giriş.

Kullanıcı sırt genişliğini girmezse: "Baskı evinizden sırt genişliğini isteyin" uyarısı.

Sayfa sayısı değişince: "Sırt genişliği değişmiş olabilir, kontrol edin" uyarısı.

Otomatik hesaplama yok.

8.4 Not
Sırt genişliği, albüm sayfa sayısına göre değişir. Örneğin 20 sayfa ile 100 sayfa aynı sırt genişliğine sahip olmaz. Bu yüzden:

Profil sırt genişliğini tek bir sayı olarak saklar.

Sayfa sayısı değişince kullanıcı uyarılır.

İleride "sırt tablosu" özelliği eklenebilir (sayfa sayısı → sırt genişliği).

9. Profil Deposu ve Organizasyon
9.1 Dosya sistemi
text
%APPDATA%/NexAlbum/
├── profiles/
│   ├── user/
│   │   ├── kullanici-profil-01.json
│   │   ├── kullanici-profil-02.json
│   │   └── ...
│   ├── imported/
│   │   └── ...
│   └── community/
│       └── ...
├── icc/
│   ├── CoatedFOGRA39.icc
│   └── ...
└── settings.json
9.2 Yerleşik profiller
Uygulama klasöründe: profiles/builtin/*.json

Salt okunur.

Kullanıcı kopyalayıp düzenleyebilir.

9.3 Profil arama ve filtreleme
Ada göre ara

Etikete göre filtrele

Kaynağa göre filtrele (yerleşik, kullanıcı, topluluk)

Doğrulama durumuna göre filtrele

Ölçüye göre filtrele

Son kullanılanlar

9.4 Profil favorileri
Kullanıcı sık kullandığı profilleri yıldızlar.

Favoriler üstte görünür.

10. Proje ve Profil İlişkisi
10.1 Bağlama
Her proje bir baskı profiline bağlıdır.

Profil id ile referanslanır.

Profil id yoksa (kullanıcı profili silmişse) proje uyarır.

Profil değişirse proje etkilenir.

10.2 Profil gömme (embedding)
Varsayılan: Profil projede referans olarak tutulur.

Taşınabilir paket: Profil JSON'u projeye gömülür.

Böylece başka bilgisayarda açıldığında profil bulunur.

10.3 Profil güncelleme
Profil güncellenirse, ona bağlı projeler etkilenir.

Uygulama "Profil güncellendi, projeyi yeniden kontrol edin" uyarısı verir.

Kullanıcı isterse eski profil sürümünü korur (snapshot).

Proje, profilin hangi sürümüyle oluşturulduğunu kaydeder.

10.4 Profil özeti
Proje ayarlarında profil özeti gösterilir:

text
Profil: 30×30 Layflat (Kullanıcı)
Doğrulama: ✅ Kullanıcı doğruladı
Sayfa: 300×300 mm
Bleed: 3 mm (her kenar)
Safe Area: 5 mm (her kenar)
Gutter: 0 mm (layflat)
DPI: 300
Renk: CMYK / CoatedFOGRA39
11. Topluluk Profilleri (İleride)
11.1 Ayrı depo
nexalbum-profiles (GitHub)

Her profil JSON dosyası

PR ile katkı

Şema doğrulaması CI'da

Doğrulama durumu: topluluk katkısı, resmi değil

11.2 İçe aktarma
Uygulama içinden "Topluluk profillerini gözat" (opsiyonel, çevrimiçi)

URL'den içe aktar

Dosyadan içe aktar

11.3 Sorumluluk reddi
Topluluk profilleri için:

"Bu profil topluluk tarafından katkı olarak sağlanmıştır. NexAlbum ekibi doğruluğunu garanti etmez. Kullanmadan önce baskı evinizle teyit edin."

11.4 Doğrulama rozeti
🏭 Baskı evi onaylı (opsiyonel, ileride)

✅ Topluluk tarafından doğrulandı

⚠️ Doğrulanmamış

12. Profil Sürümleme
12.1 Profil sürümü
Her profil version alanı taşır (SemVer).

Kullanıcı profili düzenlerse version artar.

id aynı kalır.

12.2 Proje snapshot
Proje oluşturulduğunda kullanılan profil sürümü kaydedilir.

Profil sonradan değişse bile proje eski sürümü bilir.

İsteğe bağlı: "Eski sürüme dön" özelliği.

12.3 Geriye uyumluluk
Şema sürümü artarsa (v1 → v2), migration kodu yazılır.

Eski profiller otomatik güncellenir.

Kullanıcıya "Profil güncellendi" bildirimi.

13. Doğrulama ve Güvenlik
13.1 Şema doğrulama
Her profil JSON Schema ile doğrulanır.

Geçersiz profil yüklenmez.

Hata mesajı net olur: "Sayfa genişliği eksik" gibi.

13.2 Mantıksal doğrulama
Şema dışında mantık kontrolleri:

Bleed, sayfa ölçüsünden küçük mü?

Safe area, bleed'den büyük mü?

Gutter, sayfa genişliğinden küçük mü?

DPI pozitif mi?

Ölçü birimleri tutarlı mı?

Geçmezse: Hata mesajı, kaydetmeye izin verilmez.

13.3 Uyarılar
Bleed çok küçük (< 2 mm): "Çok küçük, kontrol edin."

Safe area çok büyük (> 20 mm): "Çok büyük, kontrol edin."

DPI çok düşük (< 150): "Baskı kalitesi düşük olabilir."

DPI çok yüksek (> 600): "Gereksiz büyük dosya."

13.4 Kötü niyetli profil
Profil JSON'u sadece veri içerir; kod çalıştırmaz.

Dosya yolları doğrulanır (path traversal engellenir).

ICC profil dosyası okunurken güvenlik kontrolü.

Topluluk profilinde source alanı zorunlu.

14. Kullanıcı Arayüzü
14.1 Profil yöneticisi ekranı
Sol: Profil listesi (arama, filtre, favoriler)

Orta: Seçili profil detayları (salt okunur özet)

Sağ: Eylemler (düzenle, kopyala, sil, dışa aktar)

Üst: Yeni profil, içe aktar

14.2 Profil düzenleme ekranı
Sekmeli yapı:

Genel (ad, açıklama, etiketler)

Sayfa (ölçü, yön, tek/çift)

Bleed & Safe Area

Gutter

Çözünürlük

Renk

Çıktı

Kapak

Kağıt & Cilt

Notlar

Her sekmede: "Bu değeri baskı evinizden alın" ipucu

Alt: Kaydet / İptal / Sıfırla

14.3 Profil önizlemesi
Sayfa ölçüsü görsel olarak gösterilir.

Bleed, safe area, gutter çizgilerle işaretlenir.

Ölçüler mm/cm/inch olarak etiketlenir.

Zoom ve kaydırma.

14.4 Doğrulama sihirbazı
Adım adım rehber.

Her adımda "Nereden alınır?" ipucu.

Örnek baskı evi metni (örnek olduğu belirtilir).

14.5 Hata gösterimi
Alan bazlı hata mesajları.

Üstte özet hata listesi.

Düzeltilene kadar kaydetme engeli (hata varsa).

15. Örnek Kullanıcı Senaryoları
15.1 Senaryo A: Yeni başlayan
Kullanıcı uygulamayı açar.

"Yeni Proje" → profil seçimi ekranı.

Yerleşik "30×30" profilini seçer.

Uygulama uyarır: "Bu örnek bir profildir. Baskı evinizden aldığınız değerlerle güncelleyin."

Kullanıcı "Düzenle" der.

Bleed, safe area, DPI değerlerini baskı evinden öğrenip girer.

"Doğruladım" onayını işaretler.

Kaydeder.

Projeye devam eder.

15.2 Senaryo B: Deneyimli fotoğrafçı
Kullanıcı kendi profilini oluşturur.

"JSON'dan içe aktar" ile önceden hazırladığı profili yükler.

Uygulama şemayı doğrular.

Profil kullanıma hazır.

Kullanıcı favorilere ekler.

Sonraki projelerde hızlıca seçer.

15.3 Senaryo C: Stüdyo
Stüdyo sahibi bir profil oluşturur.

JSON olarak dışa aktarır.

Ekibindeki diğer fotoğrafçılara gönderir.

Onlar içe aktarır.

Tüm ekip aynı profille çalışır.

15.4 Senaryo D: Baskı evi değişikliği
Kullanıcı yeni bir baskı eviyle çalışmaya başlar.

Yeni profil oluşturur.

Eski profil durur (silinmez).

Yeni projeler yeni profille.

Eski projeler eski profille kalır.

15.5 Senaryo E: Eksik değer
Kullanıcı sırt genişliğini bilmiyor.

Profili kaydetmeye çalışır.

Uygulama: "Sırt genişliği boş. Baskı evinizden isteyin. Bilmiyorsanız 'bilinmiyor' işaretleyin."

Kullanıcı "bilinmiyor" işaretler.

Profil kaydedilir ama çıktı alırken preflight uyarı verir.

16. Klavye Kısayolları (Profil Ekranı)
Kısayol	İşlev
Ctrl+N	Yeni profil
Ctrl+O	İçe aktar
Ctrl+S	Kaydet
Ctrl+Shift+S	Farklı kaydet
Ctrl+D	Kopyala
Del	Sil
Ctrl+E	Dışa aktar
Ctrl+F	Ara
F2	Yeniden adlandır
Esc	İptal
17. Performans
Profil listesi 1000+ profil olsa bile hızlı yüklenir (lazy loading).

Profil doğrulama anında yapılır (küçük JSON).

ICC profil dosyası büyükse arka planda yüklenir.

Arama ve filtreleme anlık.

18. Test Stratejisi
18.1 Birim testleri
JSON şema doğrulama (geçerli/geçersiz örnekler)

Mantıksal doğrulama (bleed > sayfa vb.)

Sürüm migration (v1 → v2)

İçe/dışa aktarma

Profil kopyalama

18.2 Entegrasyon testleri
Proje + profil ilişkisi

Profil değişince proje uyarısı

Preflight kontrolleri

Taşınabilir pakete profil gömme

18.3 Kullanıcı senaryoları
§15'teki 5 senaryo

Türkçe karakterli profil adı

Çok büyük ICC dosyası

Bozuk JSON

Şema uyumsuz JSON

18.4 Örnek profil seti
Depoda samples/profiles/ altında:

10+ örnek profil

Her biri farklı senaryoyu kapsar

Test verisi olarak kullanılır

19. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
Kullanıcı yanlış değer girer	Yüksek	Yüksek	Doğrulama uyarıları, "baskı evinden alın" ipuçları
Kullanıcı değer uydurur	Orta	Yüksek	"Doğruladım" onayı, sorumluluk kullanıcıda
Yerleşik profil "gerçek" sanılır	Yüksek	Yüksek	Açık "örnek" etiketi, uyarı
Profil şeması karmaşık gelir	Orta	Orta	Basit başlangıç, sihirbaz, ipuçları
ICC profil dosyası bulunamaz	Orta	Orta	Uyarı, "profilsiz devam et" seçeneği
Sırt genişliği yanlış	Yüksek	Yüksek	Manuel giriş zorunlu, otomatik hesaplama yok
Topluluk profili kötü niyetli	Düşük	Orta	Şema doğrulama, kod çalıştırma yok
Profil sürümü karışıklığı	Orta	Orta	Proje snapshot, açık sürüm gösterimi
Kullanıcı profili siler, proje kırılır	Orta	Orta	Uyarı, "profil gömülü" seçeneği
20. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Baskı evi entegrasyonu ticari anlaşma gerektirir; ücretsiz modelde yok.
02 — Açık Kaynak	Profil formatı JSON; topluluk katkısına açık; ayrı depo mümkün.
04 — Tek Kişi	Tek geliştirici baskı evi entegrasyonunu sürdüremez; kullanıcı tanımlı profil doğru çözüm.
05 — Zaman	Entegrasyon geliştirme süresi uzun; kullanıcı tanımlı profil MVP'ye uygun.
06 — Bütçe	Ticari API maliyeti yok; kullanıcı kendi profilini girer.
07 — Rakip Özellikleri	Rakiplerin baskı evi entegrasyonu bizde yok; bunun yerine esneklik ve şeffaflık.
21. Açık Sorular (Sonraki Kararlara Bırakılan)
Topluluk profilleri ne zaman eklenecek? → Karar 04 (zaman)

Sırt tablosu özelliği olacak mı? → Karar 07 (özellikler)

ICC profil yönetimi (sürüm, güncelleme) nasıl olacak? → Karar 04

Profil şeması v2'de ne değişecek? → Gelecek kararlar

"Doğrulanmış" rozeti nasıl verilecek? → Karar 07

22. Özet
NexAlbum'de baskı profilleri kullanıcı tarafından tanımlanır. Bu karar:

Baskı evi entegrasyonunu reddeder (tek geliştirici, ücretsiz, açık kaynak gerçeği).

Kullanıcıya tam kontrol verir.

Hiçbir değer uydurulmaz.

Yerleşik profiller örnektir, "gerçek" değildir.

Profil formatı JSON, açık, topluluk katkısına uygun.

Doğrulama kullanıcının sorumluluğundadır; uygulama rehberlik eder.

Yanlış değer girme riskine karşı uyarılar ve preflight vardır.

Sırt genişliği gibi kritik değerler manuel girilir; otomatik hesaplama yoktur.

Bu karar, ürünün "doğruluk ve şeffaflık" ilkesini yansıtır. Kullanıcı yanlış baskı almaz; çünkü yanlış değeri girmekten kendisi sorumludur ve uygulama bunu ona açıkça söyler.

Onay: Bu karar, ürünün baskı doğruluğu ilkesinin temelidir.