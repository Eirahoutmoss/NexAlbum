Karar Dokümanı 00 — Giriş, Manifesto ve İndeks
Proje: NexAlbum
Karar no: 00
Konu: Projenin amacı, felsefesi, karar dokümanı sistemi ve tüm kararların indeksi
Durum: Kesinleşti
Tarih: (12/10/2026)
İlgili kararlar: 01-14 (tümü)

1. Bu Doküman Nedir?
Bu doküman, NexAlbum projesinin giriş kapısıdır. Şunları içerir:

Projenin neden var olduğu (manifesto).

Nasıl karar aldığımız (karar sistemi).

Hangi kararların alındığı (indeks).

Nasıl katkı verileceği (yönlendirme).

Eğer bu projeye yeni geldiysen, ilk bu dosyayı oku. Sonra ilgini çeken karara geç.

2. Manifesto
NexAlbum, fotoğrafçıların albüm tasarım işini hızlandıran, ücretsiz ve açık kaynak bir masaüstü uygulamasıdır.

Amacımız: Bir fotoğrafçının yüzlerce fotoğrafı dakikalar içinde anlamlı bir albüme dönüştürebilmesi; baskıya hazır PDF'i doğru ölçüde, doğru bleed'le, doğru renkle alabilmesi; istediği an tasarımı elle değiştirebilmesi.

İnancımız:

İyi yazılım pahalı olmak zorunda değildir.

Doğru ölçü, güzel tasarımdan önce gelir.

Kullanıcı verisi kullanıcının cihazında kalır.

Şeffaflık, kapalı kutudan iyidir.

Topluluk, tek kişiden güçlüdür.

Yapmadıklarımız:

Bulut zorunluluğu yok.

Hesap zorunluluğu yok.

Abonelik yok.

Reklam yok.

Telemetri zorunlu değil.

Kullanıcıyı kilitleyen format yok.

Sözümüz:

Her zaman ücretsiz kalacak.

Kaynak kodu açık kalacak.

Özellikler kilitlenmeyecek.

Değer uydurmayacağız (ne bleed, ne DPI, ne ICC).

Kullanıcıyı yanıltmayacağız.

3. Ürünün Tek Cümlelik Tanımı
Windows öncelikli, yerel çalışan, ücretsiz, açık kaynak, Türkçe albüm tasarım atölyesi.

4. Ürünün Beş Temel Vaadi
#	Vaad	Anlam
1	Doğru ölçü	Profil tabanlı; bleed, safe area, gutter uydurulmaz
2	Hızlı yerleşim	1000 fotoğraf < 10 saniye otomatik albüm
3	Elle kontrol	Otomatik sonuç her zaman düzenlenebilir
4	Açık çıktı	PDF, SVG, JPG — kilitli değil
5	Yerel + gizli	Veri cihazdan çıkmaz; hesap yok
5. Karar Dokümanı Sistemi
5.1 Neden karar dokümanı?
Yazılım projelerinde en büyük sorun: "Bunu neden böyle yaptık?" sorusunun cevapsız kalması.

Karar dokümanları:

Neyi yaptığımızı yazılı hale getirir.

Neden yaptığımızı kayıt altına alır.

Alternatifleri ve reddedilme gerekçelerini saklar.

Gelecekteki geliştiriciye (belki 6 ay sonraki sana) yol gösterir.

Topluluğa şeffaflık sağlar.

Tartışmaları yapıcı kılar.

5.2 Karar dokümanı kuralları
Numaralı. Her karar bir numara alır (00, 01, 02...).

Durumlu. Taslak, Kesinleşti, Geçersiz, Yerine yenisi geldi.

Bağlantılı. İlgili kararlara referans verir.

Gerekçeli. Her kararın "neden"i yazılıdır.

Değiştirilebilir. Ama eski karar silinmez; "geçersiz" işaretlenir.

Kamuya açık. Depoda docs/kararlar/ altında.

5.3 Karar nasıl alınır?
text
1. İhtiyaç ortaya çıkar
2. Alternatifler araştırılır
3. Artı/eksi karşılaştırılır
4. Karar verilir (gerekçeli)
5. Karar dokümanı yazılır
6. Depoya eklenir
7. Diğer kararlarla ilişkisi belirtilir
5.4 Karar nasıl değiştirilir?
text
1. Değişiklik ihtiyacı doğar
2. Eski karar "geçersiz" işaretlenir
3. Yeni karar dokümanı yazılır
4. İlişki belirtilir ("X kararının yerine geçti")
5. Topluluk bilgilendirilir
Kural: Eski karar silinmez. Tarih korunur. Geçmiş kaybolmaz.

6. Karar İndeksi
Aşağıda tüm karar dokümanları listelenmiştir. Her biri bağımsız okunabilir; ama birlikte bir bütün oluşturur.

6.1 Temel Kararlar
#	Karar	Özet	Dosya
00	Giriş ve Manifesto	Bu dosya	00_giris_ve_indeks_karari.md
01	Ücretsiz Ürün	Her zaman ücretsiz; lisans katmanı yok	01_ucretsiz_urun_karari.md
02	Açık Kaynak	GPL-3.0-or-later; topluluğa ait	02_acik_kaynak_karari.md
03	Kullanıcı Profilleri	Baskı evi entegrasyonu yok; kullanıcı kendi profilini girer	03_kullanici_profilleri_karari.md
04	Tek Kişi Geliştirme	Kapsam disiplini; sürdürülebilirlik	04_tek_kisi_gelistirme_karari.md
05	Zaman ve Yol Haritası	6 aşama; MVP ~4-7 ay; takvim değil yön	05_zaman_ve_yol_haritasi_karari.md
06	Bütçe ve Kaynak	Sıfır parasal bütçe; ücretsiz araçlar	06_butce_ve_kaynak_karari.md
07	Rakip Özellikleri	En iyilerini sentezle; kötülerini reddet	07_rakip_ozellikleri_sentezi_karari.md
6.2 Teknik Kararlar
#	Karar	Özet	Dosya
08	Teknoloji Yığını	C# 14 / .NET 10 (Rev. 1) / WPF / SkiaSharp / PDFsharp / SQLite	08_teknoloji_yigini_karari.md
09	Veri Modeli ve Proje Formatı	.albumproj (ZIP + JSON); şema sürümlü	09_veri_modeli_ve_proje_dosyasi_karari.md
10	Şablon Motoru ve Otomatik Yerleşim	7 aşamalı algoritma; yüzde tabanlı şablonlar; deterministik	10_sablon_motoru_ve_otomatik_yerlesim_karari.md
11	Çıktı Formatları ve PSD	PDF/JPG/SVG; PSD/CDR ileride	11_cikti_formatlari_ve_psd_karari.md
12	UI/UX Tasarım	5 bölge; Türkçe öncelikli; klavye dostu	12_ui_ux_tasarim_karari.md
6.3 Süreç Kararları
#	Karar	Özet	Dosya
13	Test ve Kalite	Test piramidi; CI; kalite kapıları	13_test_ve_kalite_karari.md
14	Topluluk ve Katkı	Açık katkı; DCO; Contributor Covenant	14_topluluk_ve_katki_karari.md
6.4 Gelecek Kararlar (Henüz yazılmadı)
#	Karar	Durum
15	Güvenlik ve Gizlilik	Bekliyor
16	Sürüm ve Yayın	Bekliyor
17	Marka ve Kimlik	Bekliyor
18	Bakım ve Uzun Vadeli Sürdürülebilirlik	Bekliyor
19	KVKK ve Yasal Uyumluluk	Bekliyor
7. Karar Özetleri (Tek Bakışta)
7.1 Ürün Kararları
Ücretsiz — Her zaman, herkes için.

Açık kaynak — GPL-3.0-or-later.

Yerel — Bulut yok, hesap yok.

Türkçe öncelikli — i18n altyapısı hazır.

Windows öncelikli — Cross-platform ileride.

Kullanıcı tanımlı profil — Baskı evi entegrasyonu yok.

7.2 Teknik Kararlar
.NET 10 LTS + WPF — Windows'ta en iyi performans.

SkiaSharp — Hızlı görüntü işleme.

PDFsharp — MIT lisanslı, ICC destekli PDF.

.albumproj — ZIP + JSON, şema sürümlü.

Şablonlar — Yüzde tabanlı, JSON, ölçüden bağımsız.

Otomatik yerleşim — 7 aşamalı, deterministik.

7.3 Süreç Kararları
MVP ~4-7 ay — Düzenli 10-15 saat/hafta.

Test zorunlu — Kritik yol %80+ kapsam.

CI/CD — GitHub Actions, her PR'da.

Topluluk — Açık katkı; DCO.

Karar dokümanları — Yazılı, gerekçeli, sürümlenebilir.

7.4 Sınır Kararları (Ne Yapmıyoruz?)
❌ Bulut senkronizasyonu

❌ Kullanıcı hesabı

❌ Abonelik

❌ Reklam

❌ Telemetri zorunluluğu

❌ Baskı evi API entegrasyonu

❌ Layered PSD

❌ CDR doğrudan çıktı

❌ AI rötuş

❌ Video

❌ Mobil / Web

8. Okuma Rehberi
8.1 Yeni kullanıcıysan
00 (bu dosya) — Genel bakış.

01 (Ücretsiz) — Neden ücretsiz?

12 (UI/UX) — Nasıl kullanılır?

8.2 Geliştirici olmak istiyorsan
00 (bu dosya) — Genel bakış.

02 (Açık Kaynak) — Lisans ve katkı.

08 (Teknoloji) — Ne kullanıyoruz?

09 (Veri Modeli) — Nasıl saklıyoruz?

10 (Şablon Motoru) — Kalp.

13 (Test) — Nasıl test ediyoruz?

14 (Topluluk) — Nasıl katkı verilir?

8.3 Karar vericiysen
00 (bu dosya) — Genel bakış.

01-07 — Ürün ve strateji.

08-11 — Teknik.

12-14 — Süreç.

8.4 Sadece "ne yapıyor bu?" diyorsan
§2 Manifesto ve §4 Beş Vaad yeterli.

9. Sıkça Sorulan Sorular
9.1 "Neden ücretsiz?"
Çünkü iyi yazılım pahalı olmak zorunda değil. Çünkü ücretsiz + açık kaynak, benimsenmeyi hızlandırır. Çünkü tek geliştirici için sürdürülebilir tek model. Detay: Karar 01.

9.2 "Neden açık kaynak?"
Çünkü topluluk, tek kişiden güçlüdür. Çünkü şeffaflık güven verir. Çünkü kod ortada kalırsa proje ölse bile yaşar. Detay: Karar 02.

9.3 "Neden PSD yok?"
Layered PSD, C# için 3-6 ay geliştirme demek. Hazır kütüphane yok. Aspose.PSD bütçe dışı. Bunun yerine PDF (Photoshop'ta açılır) ve SVG var. Detay: Karar 11.

9.4 "Neden Windows öncelikli?"
En yaygın fotoğrafçı platformu. WPF en iyi performans. Cross-platform ileride (Avalonia). Detay: Karar 08.

9.5 "Neden bulut yok?"
Çünkü veri kullanıcının cihazında kalmalı. Çünkü bulut zorunluluğu kullanıcıyı kilitler. Çünkü tek geliştirici bulut altyapısını sürdüremez. Detay: Karar 01.

9.6 "Neden baskı evi entegrasyonu yok?"
Her baskı eviyle ayrı anlaşma gerekir. Tek geliştirici bunu sürdüremez. Kullanıcı kendi profilini girer. Detay: Karar 03.

9.7 "Ne zaman biter?"
Bilinmiyor. Ve bu soru yanlış. Doğru soru: "Şu an hangi aşamadayım?" Detay: Karar 05.

9.8 "Katkı verebilir miyim?"
Evet! Kod, şablon, profil, çeviri, doküman, test. Detay: Karar 14.

9.9 "Lisans?"
GPL-3.0-or-later. Ticari kullanım serbest. Kapalı türev yasak. Detay: Karar 02.

9.10 "Verim nerede saklanıyor?"
Cihazında. %APPDATA%/NexAlbum/. Buluta gitmez. Detay: Karar 09.

10. Katkı Vermek İçin İlk Adım
Oku: Bu dosya + CONTRIBUTING.md.

Seç: good first issue etiketli bir issue.

Sor: Emin değilsen GitHub Discussions'ta sor.

Yap: Fork → branch → kod + test → PR.

Bekle: İnceleme (1 hafta hedefi).

Birleştir: Onay sonrası.

Detay: Karar 14 ve CONTRIBUTING.md.

11. İlkelerin Özeti (Kısa Liste)
Doğruluk önce. Ölçü, bleed, renk uydurulmaz.

Basitlik. Karmaşıklık borçtur.

Şeffaflık. Kod açık, karar yazılı.

Yerellik. Veri cihazda kalır.

Sürdürülebilirlik. Tek kişi sürdürebilir.

Topluluk. Katkıya açık.

Saygı. Kullanıcıya, katkıcıya.

Keyif. Bu bir hobi.

12. Sürüm Bilgisi
Bu doküman sürümü: 1.0

Karar sayısı: 00-14 (15 karar)

Toplam dosya: 15 markdown

Son güncelleme: (doldurulacak)

Değişiklik geçmişi: CHANGELOG.md

13. Teşekkür
Bu karar dokümanları, bir fikrin yazılı bir ürüne dönüşme sürecidir. Her biri, "acaba?" sorusunun "evet, çünkü..." cevabıdır.

Bu süreçte:

Orijinal fikir: Sen.

Karar dokümanları: Birlikte.

Uygulama: Gelecekte, birlikte.

Okuduğun için teşekkürler. Bir sonraki adımda görüşürüz.

14. Bağlantılar
Kaynak	Yer
Kararlar	docs/kararlar/
README	Depo kökü
Katkı rehberi	CONTRIBUTING.md
Davranış kuralları	CODE_OF_CONDUCT.md
Güvenlik	SECURITY.md
Lisans	LICENSE (GPL-3.0+)
Yol haritası	ROADMAP.md
Değişiklikler	CHANGELOG.md
Onay: Bu doküman, tüm kararların giriş kapısıdır ve projenin kimliğini tanımlar. Projenin adı NexAlbum'dur. İlk taslaklarda kullanılan çalışma adı "Album Atelier" tüm dokümanlarda NexAlbum olarak güncellenmiştir.
