Karar Dokümanı 06 — Bütçe ve Kaynak Yönetimi
Proje: NexAlbum
Karar no: 06
Konu: Bütçe felsefesi, kaynak kullanımı, maliyet kalemleri ve "sıfır bütçe" ilkesi
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 01 (Ücretsiz), 02 (Açık Kaynak), 03 (Kullanıcı Profilleri), 04 (Tek Kişi), 05 (Zaman)

1. Karar Özeti
NexAlbum sıfır parasal bütçe ilkesiyle geliştirilecektir. Bu, "para yok" değil; "para harcamadan sürdürülebilir" anlamına gelir.

Geliştirici zamanı tek gerçek yatırımdır.

Parasal gider: 0 TL (hedef).

Ticari kütüphane: Kullanılmaz (GPL uyumu + maliyet).

Ücretli servis: Kullanılmaz (barındırma, CI, depolama).

Donanım: Mevcut bilgisayar yeterli.

Yazılım araçları: Yalnızca ücretsiz/açık kaynak.

Belirsizlik: "Bilmiyorum" cevabı, "sıfır bütçe ile çöz" olarak yorumlanır.

Beklenmedik gider: Ortaya çıkarsa ayrı karar dokümanı gerekir.

Bu karar, 01 (Ücretsiz), 02 (Açık Kaynak) ve 04 (Tek Kişi) kararlarıyla doğrudan tutarlıdır.

2. "Bütçe" Ne Anlama Geliyor?
2.1 Geniş tanım
Bütçe = kaynakların planlanması. Kaynaklar:

Para: Nakit harcama.

Zaman: Geliştirici saati.

Dikkat: Bilişsel yük.

Enerji: Fiziksel/duygusal.

İtibar: Topluluk güveni.

Fırsat: Yapılmayan diğer işler.

Bu doküman para ve zaman üzerine odaklanır; diğerleri Karar 04 ve 05'te ele alınır.

2.2 Dar tanım
Bu dokümanda "bütçe" = parasal giderler.

2.3 Sıfır bütçe ilkesi
Hedef: 0 TL parasal gider.

İstisna: Zorunlu ve kaçınılmaz giderler (çok nadir).

İstisna süreci: Yazılı gerekçe + ayrı karar dokümanı.

Kural: "Kolaylık için para harca" yasak.

3. Neden Sıfır Bütçe?
3.1 Ürün modeli gereği
Ürün ücretsiz (Karar 01).

Gelir yok.

Gider olursa, geliştirici cebinden çıkar.

Bu sürdürülemez.

3.2 Açık kaynak gereği
Açık kaynak projeler genellikle düşük bütçelidir.

Ticari kütüphaneler lisans uyumsuzluğu yaratır (Karar 02).

Ücretsiz araçlar zaten yeterli.

3.3 Tek kişi gereği
Tek geliştiricinin bütçesi = kişisel bütçesi.

Kişisel bütçeden proje finanse etmek sürdürülemez.

Bağış/sponsorluk opsiyonel ama varsayılmaz.

3.4 Doğruluk ve şeffaflık
Sıfır bütçe, kararları basitleştirir.

"Şunu alalım" tartışması olmaz.

Her karar "ücretsiz mi?" sorusundan geçer.

3.5 Felsefi temel
İyi yazılım, pahalı araçlarla değil, iyi düşünceyle yapılır.

Kısıt, yaratıcılığı besler.

"Ücretsiz" ürünün ücretsiz araçlarla yapılması tutarlıdır.

4. Bütçe Kalemleri — Detaylı Analiz
4.1 Geliştirme araçları
Kalem	Ticari Seçenek	Ücretsiz Seçenek	Karar
IDE	Visual Studio Professional	Visual Studio Community / VS Code / JetBrains Rider (ücretsiz lisans)	✅ Ücretsiz
Git	—	Git + GitHub	✅ Ücretsiz
CI/CD	GitHub Actions (ücretli dakika)	GitHub Actions (public repo ücretsiz)	✅ Ücretsiz
Tasarım	Adobe XD / Figma Pro	Figma Free / Penpot / Inkscape / GIMP	✅ Ücretsiz
Diyagram	Lucidchart Pro	draw.io / Mermaid	✅ Ücretsiz
Dokümantasyon	Confluence	Markdown + MkDocs / Docusaurus	✅ Ücretsiz
Proje yönetimi	Jira / Asana	GitHub Projects / Kanban	✅ Ücretsiz
Not alma	Notion Pro / Obsidian Sync	Obsidian / Logseq / kağıt	✅ Ücretsiz
Mockup	—	Figma / Penpot	✅ Ücretsiz
Ekran kaydı	Camtasia	OBS Studio / ShareX	✅ Ücretsiz
Sonuç: Tüm geliştirme araçları ücretsiz alternatiflerle karşılanır.

4.2 Kütüphaneler ve bağımlılıklar
Kalem	Ticari Seçenek	Ücretsiz Seçenek	Karar
PDF üretimi	iText (AGPL/ticari) / Aspose.PDF	QuestPDF (topluluk) / PDFsharp / SkiaSharp	⚠️ Kontrol
Görüntü işleme	LeadTools	ImageSharp / SkiaSharp / Magick.NET	✅ Ücretsiz
UI framework	DevExpress / Telerik	WPF / Avalonia / WinUI	✅ Ücretsiz
Veritabanı	SQL Server	SQLite / LiteDB	✅ Ücretsiz
Log	—	Serilog / NLog	✅ Ücretsiz
Test	—	xUnit / NUnit / MSTest	✅ Ücretsiz
Mock	—	Moq / NSubstitute	✅ Ücretsiz
DI	—	Microsoft.Extensions.DependencyInjection	✅ Ücretsiz
Not: QuestPDF topluluk lisansı, yıllık geliri 1M$ altındaki projeler için ücretsizdir. Bizim için uygun; ancak kullanıcılar için de aynı koşul geçerli mi netleştirilmelidir. Alternatif: PDFsharp.

Sonuç: Tüm bağımlılıklar ücretsiz ve GPL uyumlu.

4.3 Dağıtım ve barındırma
Kalem	Ticari Seçenek	Ücretsiz Seçenek	Karar
Kod barındırma	GitHub Team	GitHub Free (public repo)	✅ Ücretsiz
Sürüm yayını	AWS S3	GitHub Releases	✅ Ücretsiz
Web sitesi	Hosting	GitHub Pages	✅ Ücretsiz
Alan adı	—	nexalbum.github.io	✅ Ücretsiz (alt alan adı)
E-posta	Google Workspace	GitHub Issues	✅ Ücretsiz
Topluluk	Discord Nitro	GitHub Discussions / Discord Free	✅ Ücretsiz
Sonuç: Tüm dağıtım ve barındırma GitHub üzerinde ücretsiz.

4.4 Kod imzalama
Kalem	Ticari Seçenek	Ücretsiz Seçenek	Karar
Windows kod imzalama	DigiCert / Sectigo (yıllık 200-500$)	Yok (öz-imzalı, uyarı verir)	⚠️ Ertelendi
Durum: Kod imzalama sertifikası yok. Kullanıcı "bilinmeyen yayıncı" uyarısı görecek. Bu kabul edilebilir; belge açıklama ile geçilir.

Alternatif: Ücretsiz kod imzalama çözümleri (SignPath, Azure Trusted Signing — kısmi ücretsiz) ileride araştırılabilir.

Karar: MVP'de kod imzalama yok. v2.0'da bağış/sponsorluk ile değerlendirilir.

4.5 Donanım
Kalem	Gerekli mi?	Mevcut mu?	Karar
Geliştirme bilgisayarı	Evet	Evet (varsayım)	✅ Mevcut
Test bilgisayarı (Windows 10)	Önerilen	Belki	⚠️ Sanal makine
Test bilgisayarı (Windows 11)	Önerilen	Belki	⚠️ Sanal makine
Yüksek DPI monitör	Önerilen	Belki	⚠️ Opsiyonel
Sonuç: Donanım için para harcanmaz. Test için sanal makine (Hyper-V / VirtualBox) kullanılır.

4.6 Eğitim ve öğrenme
Kalem	Ticari Seçenek	Ücretsiz Seçenek	Karar
Online kurs	Udemy / Pluralsight	YouTube / Microsoft Learn / freeCodeCamp	✅ Ücretsiz
Kitap	Teknik kitap (50-100$)	Kütüphane / açık kaynak dokümantasyon	⚠️ Opsiyonel
Konferans	Ücretli biletler	YouTube kayıtları	✅ Ücretsiz
Sonuç: Öğrenme için para harcanmaz; kişisel istek varsa ayrı değerlendirilir (bu proje bütçesinden değil).

4.7 Yasal ve idari
Kalem	Gerekli mi?	Ücret	Karar
Şirket kurulumu	Hayır	—	❌ Gerekmez
Marka tescili	Hayır (şimdilik)	~1000 TL	⚠️ Ertelendi
Hukuki danışmanlık	Hayır (GPL net)	—	❌ Gerekmez
Vergi	Hayır (gelir yok)	—	❌ Gerekmez
Sözleşme	Hayır (katkı DCO)	—	❌ Gerekmez
Sonuç: Yasal/idari gider yok. Marka tescili ileride topluluk bağışıyla düşünülebilir.

4.8 Toplam yıllık bütçe
Kategori	Ticari	Ücretsiz (bizim)
Geliştirme araçları	~2000$	0$
Kütüphaneler	~5000$	0$
Barındırma	~500$	0$
Kod imzalama	~300$	0$ (ertelendi)
Donanım	~2000$	0$ (mevcut)
Eğitim	~500$	0$
Yasal	~1000$	0$
Toplam	~11.300$/yıl	0$/yıl
Sonuç: Ücretsiz araçlarla tüm ihtiyaçlar karşılanır.

5. Gizli Maliyetler ve Bilinmeyenler
Sıfır bütçe hedef olsa da bazı gizli maliyetler vardır:

5.1 Zaman maliyeti
En büyük maliyet: geliştirici zamanı (Karar 04, 05).

Parasal değeri: 0 (gönüllü).

Gerçek değeri: yüksek (alternatif iş yapılabilirdi).

5.2 Fırsat maliyeti
Projeye harcanan zaman, başka şeylere harcanamaz.

Bu kabul edilir; keyif alınıyor.

5.3 Öğrenme maliyeti
Yeni teknoloji öğrenmek zaman alır.

Parasal değil ama zaman değeri var.

5.4 Topluluk maliyeti
Issue yanıtlama, PR inceleme zaman alır.

Parasal değil ama zaman.

5.5 Bakım maliyeti
Bağımlılık güncelleme, hata düzeltme, sürüm sürekli.

1 yıl sonra hâlâ bakım gerekiyor.

5.6 Bilinmeyenler
PDF kütüphanesi yetmezse? → Ücretsiz alternatifler var.

Windows API değişirse? → Adaptasyon.

Topluluk beklenmedik talep getirirse? → "Hayır" deme disiplini (Karar 04).

İlke: Bilinmeyen, sıfır bütçeyle çözülür. Para harcamak son çaredir.

6. Gider Ortaya Çıkarsa Süreç
6.1 İstisna kriterleri
Bir gider sadece şu koşullarda kabul edilir:

Zorunlu: Alternatifi yok.

Küçük: < 500 TL / yıl.

Tek seferlik: Yinelenen gider değil.

Yazılı gerekçe: Neden gerekli, neyi çözer.

Ayrı karar dokümanı: Bu doküman güncellenir.

6.2 Kabul edilebilir istisna örnekleri
Alan adı (nexalbum.com gibi) — ~100 TL/yıl, opsiyonel.

Marka tescili — topluluk bağışıyla ileride.

Kod imzalama sertifikası — topluluk bağışıyla ileride.

6.3 Kabul edilemeyecek giderler
Ticari kütüphane lisansı (GPL uyumsuz).

Ücretli barındırma (ücretsiz alternatif varken).

Reklam (ürün ilkesine aykırı).

Ücretli tema/tasarım (ücretsiz var).

Ekip toplantısı/tatil (tek kişi zaten).

6.4 Karar süreci
İhtiyaç ortaya çıkar.

Ücretsiz alternatif araştırılır (zorunlu).

Alternatif yoksa gerekçe yazılır.

Bu dokümana ek karar eklenir.

Onaylanırsa gider kabul edilir.

Yıllık gözden geçirmede değerlendirilir.

7. Bağış ve Sponsorluk (Opsiyonel)
7.1 İlke
Bağış beklenmez; olursa sevinçle kabul edilir.

Bağış, projenin olması için değil, daha iyi olması için.

Bağış altyapısı varsa da proje onsuz çalışır.

7.2 Kabul edilen bağış türleri
GitHub Sponsors: Aylık/tek seferlik.

Ko-fi / Buy Me a Coffee: Tek seferlik.

Liberapay: Yinelenen.

Açık kaynak fonları: NLNet, Mozilla, vb. (başvuru).

Kurumsal sponsorluk: Baskı evleri, eğitim kurumları.

7.3 Bağış kullanımı
Öncelik sırası:

Hiçbir şey (proje zaten ücretsiz araçlarla yürür).

Alan adı (opsiyonel).

Kod imzalama sertifikası (kullanıcı güveni).

Topluluk etkinlikleri (opsiyonel).

Geliştiriciye teşekkür (kahve, kitap).

Yasak: Bağış, özellik kilidi açmaz; kararları etkilemez; reklam yerleştirmez.

7.4 Şeffaflık
Bağış alınırsa, harcama raporu yayınlanır (yıllık).

Ama bağış alınmazsa da proje yürür.

"Bağış yoksa proje durur" algısı yaratılmaz.

7.5 Sponsorluk ilkeleri
Sponsor ürün kararlarına karışmaz.

Sponsor reklam yerleştirmez.

Sponsor adı README'de listelenir (isteğe bağlı).

Sponsor çekilirse proje devam eder.

8. Zaman = Bütçe
8.1 Zamanın parasal karşılığı
Geliştirici saati 0 TL (gönüllü), ama fırsat maliyeti yüksektir. Örneğin:

400 saat × 200 TL/saat = 80.000 TL (piyasa değeri).

Bu "görünmez" yatırım, projenin gerçek değeridir.

8.2 Zaman tasarrufu = para tasarrufu
Otomasyon (Karar 04): zaman kazandırır.

Basit çözüm: zaman kazandırır.

"Şimdi değil" listesi: zaman kazandırır.

Ücretsiz araç: para kazandırır + zaman kazandırır.

8.3 Zaman israfı = en büyük maliyet
Gereksiz özellik: onlarca saat.

Karmaşık kod: bakım saatleri.

Yanlış teknoloji: baştan yazma.

Tükenme: haftalar/aylar kaybı.

Sonuç: Zamanı para gibi harca — dikkatli.

9. Karar Matrisi (Bütçe Perspektifi)
Her karar şu sorulardan geçer:

Soru	Cevap "Hayır" ise
Ücretsiz mi?	Reddedilir
Açık kaynak mı?	Reddedilir
GPL uyumlu mu?	Reddedilir
Tek kişi sürdürebilir mi?	Ertelenir
Zaman bütçesine uygun mu?	Ertelenir
Gerçek değer mi?	Reddedilir
10. Uzun Vadeli Finansal Senaryolar
10.1 Senaryo A: Sıfır bütçe (mevcut plan)
0 TL/yıl gider.

Bağış yok.

Sürdürülebilir (gönüllü).

Olasılık: Yüksek.

10.2 Senaryo B: Küçük bağış
500-2000 TL/yıl bağış.

Alan adı + kod imzalama.

Proje davranışı değişmez.

Olasılık: Orta.

10.3 Senaryo C: Kurumsal sponsorluk
5000-20000 TL/yıl.

Topluluk etkinlikleri, ödüller.

Karar bağımsızlığı korunur.

Olasılık: Düşük.

10.4 Senaryo D: Ticari teklif
Bir şirket satın almak/tamamlamak ister.

Karar: Reddedilir (Karar 01, 02).

Çatal açabilir (GPL izin verir).

Olasılık: Düşük.

10.5 Senaryo E: Fon başvurusu
NLNet, Mozilla, vb.

Başvuru zaman alır; sonuç belirsiz.

Alınırsa iyi; alınmazsa plan değişmez.

Olasılık: Düşük.

Sonuç: Plan A'ya göre yaşa; B/C/E olursa bonus.

11. Maliyet-Fayda Analizi (Örnekler)
11.1 Kod imzalama sertifikası
Maliyet: ~3000 TL/yıl.

Fayda: Kullanıcı "bilinmeyen yayıncı" uyarısı görmez.

Alternatif: README'de açıklama, hash doğrulama.

Karar: Ertelendi (bağış olursa değerlendirilir).

11.2 Alan adı
Maliyet: ~100-300 TL/yıl.

Fayda: Profesyonel görünüm, kolay paylaşım.

Alternatif: nexalbum.github.io.

Karar: Opsiyonel; bağış olursa alınabilir.

11.3 Ticari PDF kütüphanesi
Maliyet: ~1000-5000 $/yıl.

Fayda: Daha kolay PDF üretimi.

Alternatif: QuestPDF / PDFsharp (ücretsiz).

Karar: Reddedildi (bütçe + GPL).

11.4 Ücretli tasarım teması
Maliyet: ~200-1000 TL.

Fayda: Daha güzel arayüz.

Alternatif: Kendi tasarım + ücretsiz ikonlar (Feather, Tabler).

Karar: Reddedildi.

11.5 Yüksek DPI monitör
Maliyet: ~10.000 TL.

Fayda: Daha iyi tasarım deneyimi.

Alternatif: Mevcut monitör.

Karar: Kişisel tercih (proje bütçesinden değil).

12. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Gelir yok → gider de olmamalı.
02 — Açık Kaynak	Ticari kütüphane yasak; ücretsiz alternatif zorunlu.
03 — Kullanıcı Profilleri	Baskı evi entegrasyonu parasal anlaşma gerektirir; reddedildi.
04 — Tek Kişi	Tek kişinin bütçesi = kişisel bütçe; sıfır hedef.
05 — Zaman	Zaman = en büyük bütçe kalemi; dikkatli kullan.
07 — Rakip Özellikleri	Rakiplerin ticari özellikleri (bulut, hesap) reddedildi.
13. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
Beklenmedik gider	Orta	Orta	Sıfır bütçe ilkesi; istisna süreci
Ticari kütüphane cazip görünür	Orta	Yüksek	GPL uyumu hatırlatması; ücretsiz alternatif listesi
Bağış gelmez	Yüksek	Düşük	Zaten bağışa bağlı değil
Bağış gelir, beklenti yaratır	Düşük	Orta	Bağış ilkeleri net; karar bağımsızlığı
Sponsor müdahale eder	Düşük	Yüksek	Sponsorluk ilkeleri; reddetme hakkı
Kod imzalama eksikliği güven zedeler	Orta	Orta	Açık kaynak + hash + README açıklaması
Zaman bütçesi aşılır	Yüksek	Orta	Karar 04, 05 disiplini
Tükenme → proje durur	Orta	Yüksek	Mola, sağlık, topluluk
Fırsat maliyeti göz ardı edilir	Orta	Orta	Zaman takibi; keyif kontrolü
Bağış altyapısı MVP'yi geciktirir	Düşük	Düşük	Bağış altyapısı opsiyonel; sonraya
14. Başarı Metrikleri
14.1 Bütçe metrikleri
Parasal gider: 0 TL (hedef).

İstisna sayısı: 0 (ilk yıl ideal).

Bağış: 0 (beklenmiyor; olursa bonus).

Ticari kütüphane: 0.

14.2 Kaynak metrikleri
Zaman kullanımı: Haftada 10-15 saat.

Fırsat maliyeti farkındalığı: Aylık değerlendirme.

Keyif düzeyi: 7+/10 (subjektif).

14.3 Sürdürülebilirlik metrikleri
Proje yaşı: 1+ yıl.

Bakım devamlılığı: Aylık commit var mı?

Topluluk katkısı: İlk PR geldi mi?

Tükenme işareti: Yok.

15. Araç ve Hizmet Listesi (Onaylanmış)
15.1 Zorunlu (ücretsiz)
GitHub (kod, issue, CI, release, pages)

Visual Studio Community / VS Code

.NET SDK

Git

GitHub Actions

Figma Free / Penpot (tasarım)

draw.io / Mermaid (diyagram)

Obsidian / Logseq (not)

15.2 Opsiyonel (ücretsiz)
JetBrains Rider (açık kaynak lisansı)

GitHub Sponsors (bağış)

Ko-fi (bağış)

Discord Free (topluluk)

MkDocs / Docusaurus (dokümantasyon sitesi)

15.3 Kullanılmayacaklar (ticari)
Visual Studio Professional

Adobe Creative Cloud

Figma Pro

Jira / Confluence

Notion Pro

iText / Aspose (ticari)

DevExpress / Telerik

Herhangi bir abonelik

15.4 Kararsız (ileride)
QuestPDF (topluluk lisansı — koşul kontrol edilecek)

Kod imzalama (bağış olursa)

Alan adı (bağış olursa)

Marka tescili (bağış olursa)

16. Yıllık Gözden Geçirme
Her yıl başında:

□ Gider var mı? (Hedef: 0)
□ İstisna kullanıldı mı? (Gerekçe yazılı mı?)
□ Bağış geldi mi? (Nasıl kullanıldı?)
□ Yeni ticari kütüphane ihtiyacı var mı? (Ücretsiz alternatif?)
□ Zaman bütçesi sağlıklı mı?
□ Tükenme işareti var mı?
□ Gelecek yıl için değişiklik var mı?
Sonuç: Bu doküman güncellenir veya teyit edilir.

17. Açık Sorular (Sonraki Kararlara Bırakılan)
Bağış altyapısı ne zaman kurulacak? → v1.1 sonrası

Alan adı alınacak mı? → Bağış olursa

Kod imzalama? → Bağış olursa / v2.0

Marka tescili? → Bağış olursa / v2.0

QuestPDF uygun mu? → Aşama 4 öncesi test

Sponsorluk kabul edilecek mi? → v1.0 sonrası

Fon başvurusu? → Topluluk büyürse

18. Özet
NexAlbum sıfır parasal bütçe ilkesiyle geliştirilir. Bu karar:

Ücretsiz araçlar kullanır (GitHub, VS Community, Figma Free...).

Ücretsiz kütüphaneler tercih eder (GPL uyumlu).

Ticari hizmet kullanmaz (barındırma, CI, depolama).

Bağış/sponsorluk opsiyoneldir; proje onlara bağlı değildir.

Kod imzalama ertelendi (bağış olursa).

Beklenmedik gider yazılı gerekçe + karar dokümanı gerektirir.

En büyük bütçe: geliştiricinin zamanı (Karar 04, 05).

Fırsat maliyeti farkındalığı yüksek tutulur.

"Bilmiyorum" cevabı, "sıfır bütçe ile çöz" olarak yorumlanır. Bu, ürünün ücretsiz ve açık kaynak kimliğiyle tutarlıdır ve tek geliştirici için sürdürülebilir tek modeldir.

Onay: Bu karar, ürünün kaynak yönetimi ilkesinin temelidir.