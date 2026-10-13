Karar Dokümanı 04 — Tek Kişi Geliştirme Gerçeği ve Sürdürülebilirlik
Proje: NexAlbum
Karar no: 04
Konu: Tek geliştiriciyle sürdürülebilir geliştirme modeli, kapsam disiplini ve zaman yönetimi
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 01 (Ücretsiz), 02 (Açık Kaynak), 03 (Kullanıcı Profilleri), 05 (Zaman), 06 (Bütçe)

1. Karar Özeti
NexAlbum tek bir geliştirici tarafından, gönüllü olarak, belirsiz bir zaman çizelgesinde geliştirilecektir.

Bu gerçek, ürünün tüm kararlarını etkiler:

Kapsam disiplini zorunludur. Her özellik "gerçekten gerekli mi?" sorusundan geçer.

MVP küçük ve tamamlanabilir olmalıdır. Bitmeyen proje, ölü projedir.

Mükemmeliyetçilik bir tuzaktır. "İyi" bitmiş, "mükemmel" bitmemişten iyidir.

Topluluk katkısı teşvik edilir. Tek kişi yükünü paylaşmak için açık kaynak şart.

Bakım kolaylığı önceliklidir. Karmaşık kod, yarın seni değil, 6 ay sonraki seni yorar.

Dokümantasyon birinci sınıf vatandaştır. Yarın unutacağını bugün yaz.

Otomasyon (CI, test, yayın) yatırımdır. Manuel iş, zaman hırsızıdır.

Yol haritası esnektir ama yönsüzdür değildir. Öncelik sırası nettir.

Bu karar, "mükemmel bir şey yapmak istiyorum, zaman alabilir" ifadesini, gerçekçi ve sürdürülebilir bir çerçeveye oturtur.

2. Tek Geliştirici Gerçeği
2.1 Neden bu bir "karar"dır?
Tek geliştirici olmak bir durum değil, bir kısıttır. Bu kısıt:

Kapsamı sınırlar.

Hızı belirler.

Kaliteyi şekillendirir.

Öncelikleri zorunlu kılar.

Riskleri tanımlar.

Bu kısıtı görmezden gelmek, projenin en büyük riskidir: tükenme (burnout) ve yarım kalma.

2.2 Tek geliştiricinin avantajları
Karar hızı: Kimseyle tartışmadan karar verilir.

Tutarlılık: Tek vizyon, tek stil, tek ses.

Esneklik: İstediğin zaman yön değiştirebilirsin.

Öğrenme: Her katmanı sen yazarsın; derin öğrenme.

Sahiplik: Kodun her satırına hâkimsin.

Maliyet: Maaş, ofis, toplantı yok.

Odak: Küçük ama net bir hedef.

2.3 Tek geliştiricinin dezavantajları
Zaman: 1 kişi = 1x hız. Ekip 5 kişi olsa 5x olmaz ama 2-3x olur.

Kör nokta: Kendi hatanı görmek zor.

Yorgunluk: Sürekli tek başına karar vermek yorucu.

Bilgi sınırı: Her alanda uzman olamazsın.

Test yükü: Her şeyi kendin test edersin.

Destek yükü: Kullanıcı sorularını kendin yanıtlarsın.

Motivasyon: Uzun süreli projelerde motivasyon düşer.

Bus factor 1: Sen durursan proje durur.

2.4 Gerçekçi beklenti
Bu proje:

Bitmeyecek. Yazılım hiç bitmez; sürümler biter.

Hızlı olmayacak. Aylar sürecek.

Mükemmel olmayacak. İlk sürümde eksikler olacak.

Herkesi memnun etmeyecek. Bazı özellikler "hayır" olacak.

Bazen duracak. Hayat, iş, aile öncelikli.

Ama ilerleyecek. Küçük adımlarla, sürekli.

Bu gerçekleri baştan kabul etmek, hayal kırıklığını önler.

3. Sürdürülebilirlik İlkeleri
3.1 İlke 1: Küçük başla, çalışan teslim et
Her hafta çalışan bir şey olmalı.

"Yarım özellik" yerine "tam küçük özellik".

Büyük özellikleri küçük parçalara böl.

"Dikey dilim" yaklaşımı: her özellik uçtan uca (UI + domain + test).

3.2 İlke 2: Kapsamı koru
"Bunu da ekleyelim" cümlesi yasak.

Yeni özellik = başka bir özelliğin ertelenmesi.

Yol haritası dışına çıkmak için yazılı gerekçe gerekir.

"İyi fikir" ≠ "şimdi yapılacak iş".

3.3 İlke 3: Otomatikleştir
Manuel test → otomatik test.

Manuel yayın → CI/CD.

Manuel biçimlendirme → formatter.

Manuel lisans kontrolü → script.

Manuel sürüm notu → changelog aracı.

Kural: Bir işi 3 kez manuel yaptıysan, otomatikleştir.

3.4 İlke 4: Dokümante et
Yarın unutacağını bugün yaz.

"Neden" yaz, "ne" zaten kodda.

Karar dokümanları (bu seri gibi).

README, CONTRIBUTING, mimari notları.

Kod içi yorum: sadece "neden", "ne" değil.

3.5 İlke 5: Basit tut
Karmaşıklık borçtur; faizi yüksektir.

"İleride lazım olur" = şimdi yazma.

Erken soyutlama = kötü soyutlama.

3 kez tekrar edene kadar soyutlama.

Basit çözüm, akıllı çözümden iyidir.

3.6 İlke 6: Sağlığını koru
Kod yazmak maraton değil, sürat koşusu değil, düzenli yürüyüş.

Günde 1-4 saat yeterli; 10 saat tükeniş getirir.

Haftada en az 1 gün kod yok.

Tatile çık; proje bekleyebilir.

Tükenme, en büyük proje riskidir.

3.7 İlke 7: Topluluğa aç
Tek kişi yükünü paylaşmak için topluluk şart.

Erken aç (kod çirkin olsa bile).

"Yardım eder misiniz?" diye sor.

Katkı rehberi net olsun.

Küçük görevler etiketle (good first issue).

3.8 İlke 8: Yönü koru
Her ay yol haritasını gözden geçir.

"Ne yaptım? Ne öğrendim? Ne değişti?"

Gerekirse yön değiştir; ama yazılı gerekçeyle.

Vizyon sabit, yol esnek.

3.9 İlke 9: Ölç, ama takıntı yapma
İlerlemeyi ölç (tamamlanan özellik, kapanan issue).

Ama sayı için sayı kovalamayın.

Kullanıcı memnuniyeti asıl metriktir.

3.10 İlke 10: Keyif al
Bu proje gönüllü. Maaş yok.

Keyif almıyorsan, bir şey yanlış.

Sıkıldığın özelliği ertele; ilginç olanı yap.

Ama disiplini bırakma; keyif + disiplin.

4. Kapsam Disiplini
4.1 "Özellik" nasıl kabul edilir?
Her yeni özellik şu sorulardan geçer:

Ürün vizyonuna uygun mu? (Albüm tasarım atölyesi)

MVP'de gerekli mi? (Yoksa sonraki sürüm)

Kullanıcı için gerçek değer mi? (Yoksa "havalı" mı?)

Bakım maliyeti nedir? (Kod, test, dokümantasyon)

Bağımlılık ekler mi? (Lisans, güvenlik, sürüm)

Tek geliştirici sürdürebilir mi?

Alternatifi var mı? (Daha basit çözüm)

Şimdi mi, sonra mı?

Cevaplar net değilse: hayır.

4.2 "Hayır" listesi (bilinçli reddedilenler)
Bu listedeki şeyler şimdilik yapılmayacak:

Bulut senkronizasyonu

Kullanıcı hesapları

Sosyal paylaşım

Mobil uygulama

Web sürümü

macOS/Linux (öncelik Windows; ileride Avalonia ile mümkün)

Yapay zekâ ile rötuş

Otomatik yüz güzelleştirme

Gelişmiş RAW işleme

Video desteği — Geçersiz (Karar 21: hikâye filmi ve sosyal medya klipleri kapsama alındı)

Baskı evi API entegrasyonu

Doğrudan PSD üretimi (harici kütüphane olmadan)

Doğrudan CDR üretimi

Çok kullanıcılı iş birliği

Şablon mağazası (paralı)

Abonelik modeli

Not: "Hayır" demek "asla" değil, "şimdi değil" demek. Yol haritasında yer alabilirler.

4.3 "Evet" listesi (MVP'de olacaklar)
Doküman 12. bölümdeki MVP listesi geçerlidir:

Yeni proje ve profil seçimi

Özel ölçü girişi

Fotoğraf içe aktarma ve thumbnail

En az 10 şablon

Otomatik sayfa oluşturma

Fotoğraf değiştirme, kırpma, konumlandırma

Sayfa sıralama, ekleme, silme, kilitleme

Temel metin ve arka plan

Proje kaydet/aç

JPG önizleme

Baskı PDF

SVG dışa aktarma

Baskı öncesi kontrol

Türkçe arayüz

Temel testler ve örnek proje

4.4 "Belki" listesi (v1.1+)
MVP sonrası, talep ve zaman durumuna göre:

Yüz algılama ile kırpma önerisi

Kullanıcı şablon kaydetme

Profil içe aktarma (topluluk)

Gelişmiş metin düzenleme

Soft proof

Çoklu sayfa seçimi

RAW (LibRaw)

Harici editör entegrasyonu (varsa)

Topluluk şablon/profil deposu

4.5 Kapsam genişleme kuralları
Bir özellik "belki" listesinden "şimdi" listesine geçmek istiyorsa:

Yazılı gerekçe (neden şimdi?)

Başka neyin erteleneceği (trade-off)

Tahmini süre (gerçekçi)

Bakım maliyeti (1 yıl sonra)

Test planı

Kabul kriteri

Bu 6 maddeyi doldurmayan özellik, kapsama girmez.

5. Zaman Yönetimi
5.1 Gerçekçi zaman tahmini
"1 günde biter" → gerçekte 3 gün.

"1 hafta sürer" → gerçekte 3 hafta.

"1 ayda MVP" → gerçekte 3-6 ay.

Kural: Tahminini 2.5 ile çarp.

Neden?

Hatalar

Test süresi

Dokümantasyon

Yeniden düzenleme

Hayat (iş, aile, sağlık)

Motivasyon dalgalanması

5.2 Çalışma ritmi
Öneri:

Haftada 10-20 saat (düzenli).

Haftada 5-6 gün, günde 2-3 saat.

Sabah veya akşam (kendi ritmine göre).

Haftada 1 gün kod yok.

Yasak:

Gece 3'e kadar kod yazmak (bir kez olur, alışkanlık olmaz).

Hafta sonu tamamen projeye gömmek.

Tatilsiz aylarca çalışmak.

Sonuç:

Düzenli ritim, yoğun patlamalardan iyidir.

Tükenme, hedefi kaçırmaktan beter.

5.3 Zaman blokları
Örnek bir hafta:

Gün	Süre	İş
Pazartesi	2 saat	Kod (yeni özellik)
Salı	2 saat	Kod (yeni özellik)
Çarşamba	2 saat	Test + düzeltme
Perşembe	2 saat	Kod (yeni özellik)
Cuma	2 saat	Dokümantasyon + topluluk
Cumartesi	2 saat	Serbest (ilginç iş)
Pazar	0	Dinlenme
Toplam: ~12 saat/hafta.

5.4 Zaman kaybı önleme
Toplantı yok. (Tek kişisin)

Sosyal medya yok (kod yazarken).

Bildirim kapalı. (Discord, e-posta)

Tek görev. Multitasking yok.

Ortam hazır. Editör, kısayol, tema.

Hızlı geri bildirim. Testler saniyeler içinde çalışsın.

5.5 Erteleme ile mücadele
En küçük adımı yaz. "Albüm oluştur" yerine "Yeni proje düğmesi".

2 dakika kuralı. 2 dakikada yapılacak işi hemen yap.

Zor işi sabaha al. İrade sabah daha güçlü.

İlerlemeyi gör. Commit grafiği, tamamlanan görevler.

Mola ver. Tıkanınca kalk, yürü.

6. Kod Kalitesi ve Bakım
6.1 Bakım kolaylığı ilkeleri
Okunabilirlik > zeka. Kısa kod, okunabilir koddan iyi değil.

Açık isimler. CalcTmp yerine CalculateBleedMargin.

Tek sorumluluk. Her sınıf tek iş.

Az bağımlılık. Her bağımlılık bir yüktür.

Test kapsamı. Kritik yol %80+.

Küçük dosyalar. 500 satırdan uzun dosya şüpheli.

Az yorum, çok niyet. Kod niyeti anlatsın.

6.2 Teknik borç yönetimi
Kaydet. Teknik borcu TODO ve issue olarak yaz.

Sınırla. Borç almadan önce "geri ödeyebilir miyim?" sor.

Görünür kıl. Borç listesi her ay gözden geçir.

Önceliklendir. Kritik borç önce.

Fırsatçı ödeme. Bir dosyayı düzenlerken borcunu da düzelt.

6.3 Refactor disiplini
Önce çalıştır, sonra güzelleştir.

Test varsa refactor güvenli.

Küçük adımlarla refactor.

Refactor + özellik aynı commit'te olmasın.

Refactor commit'i ayrı olsun.

6.4 Ölü kod temizliği
Kullanılmayan kod silinir.

Yorum satırına alınmış kod silinir (Git zaten saklıyor).

Kullanılmayan bağımlılık kaldırılır.

Ölü kod, yanıltıcı dokümantasyondur.

7. Test Stratejisi (Tek Kişi İçin)
7.1 Test öncelikleri
Kritik iş mantığı (layout engine, preflight, ölçü hesaplama)

Veri modeli (şema doğrulama, migration)

Dosya işlemleri (kaydet/aç, dışa aktarma)

UI etkileşimleri (sınırlı, çünkü kırılgan)

Uçtan uca (az sayıda, kritik akışlar)

7.2 Test piramidi
text
        /\
       /  \      E2E (5-10 test)
      /____\
     /      \    Entegrasyon (20-50 test)
    /________\
   /          \  Birim (100-500 test)
  /____________\
7.3 Test yazma kuralları
Yeni özellik = yeni test. İstisna yok.

Hata düzeltmesi = regresyon testi. Hata tekrar etmesin.

Test isimleri açıklayıcı. WhenBleedIsZero_ShouldFail.

Bir test = bir davranış.

Test hızlı olsun. Saniyeler içinde.

Flaky test = kaldır ya da düzelt.

7.4 Manuel test
Otomatik test yoksa, kontrol listesi hazırla.

Her sürüm öncesi manuel "smoke test".

Örnek proje ile uçtan uca.

Farklı Windows sürümleri (10, 11).

7.5 Test verisi
samples/ altında örnek proje.

Test fotoğrafları (lisansı uygun).

Örnek profiller.

Örnek şablonlar.

8. Dokümantasyon Stratejisi (Tek Kişi İçin)
8.1 Dokümantasyon önceliği
Karar dokümanları (bu seri) — "neden"

README — "ne, nasıl kurulur"

Mimari notları — "nasıl çalışır"

Kod içi yorum — "neden böyle"

Kullanıcı kılavuzu — "nasıl kullanılır"

API dokümantasyonu — "nasıl genişletilir"

8.2 "Yarın unutacağın" testi
Bir kod parçası yazarken sor:

"6 ay sonra bunu okuyan ben, neden böyle yaptığımı anlar mıyım?"

Anlamazsa: yorum yaz veya dokümante et.

8.3 Dokümantasyon bakımı
Kod değişince doküman da değişir.

Her PR'da dokümantasyon kontrolü.

Yıllık dokümantasyon temizliği.

Kullanılmayan doküman silinir.

8.4 Görsel dokümantasyon
Diyagramlar (Mermaid, çizim).

Ekran görüntüleri (kullanıcı kılavuzu).

GIF'ler (kısa gösterimler).

Bir görsel, 1000 kelime.

9. Topluluk Yönetimi (Tek Kişi İçin)
9.1 Gerçekçi beklenti
İlk 6 ay kimse gelmeyebilir.

İlk katkı 6-12 ay sonra gelebilir.

İlk 100 yıldız 1-2 yıl sürebilir.

Bu normaldir.

9.2 Topluluk zaman bütçesi
Haftada 1-2 saat topluluk.

Issue yanıtlama: 24-72 saat içinde ilk yanıt (mümkünse).

PR inceleme: haftada 1 kez toplu.

Tartışmalar: haftada 1 kez göz at.

9.3 Sınır koyma
"Hayır" demeyi öğren. Her özellik talebine evet deme.

Şablon yanıtlar. Sık sorulan sorulara hazır cevap.

Yönlendir. SSS, dokümantasyon, tartışmalar.

Kapat. Çözülen issue'ları kapat.

Görmezden gel. Kötü niyetli yorumlara girme.

9.4 Katkı teşviki
good first issue etiketi.

help wanted etiketi.

Katkı rehberi net.

Katkıcıyı teşvik et (teşekkür, AUTHORS.md).

Küçük görevleri kolayca ver.

9.5 Topluluk kanalları
GitHub Issues: Birincil.

GitHub Discussions: Sorular.

Discord/Matrix: İleride, topluluk büyürse.

E-posta: Sadece güvenlik için.

10. Motivasyon ve Tükenme Yönetimi
10.1 Motivasyon kaynakları
İlerleme: Commit grafiği, tamamlanan özellikler.

Geri bildirim: Kullanıcı yorumları, teşekkürler.

Öğrenme: Yeni teknoloji, yeni problem.

Topluluk: Katkıcılar, tartışmalar.

Amaç: Birinin işine yarıyor olması.

10.2 Tükenme belirtileri
Kod yazmak istemiyorsun.

Küçük bir hatayı günlerce erteliyorsun.

Projeyi düşünmek bile yoruyor.

"Neden yapıyorum?" sorusu sık geliyor.

Uyku bozuluyor, huzursuzluk artıyor.

10.3 Tükenme önleme
Mola ver. 1 hafta kod yok.

Tatil yap. 2 hafta proje kapalı.

Küçült. Bir sonraki hedefi yarıya indir.

Kolay iş seç. Zor işi ertele.

Paylaş. Topluluğa "yoruldum" de.

Danış. Bir arkadaşla konuş.

Sağlık önce. Uyku, yemek, egzersiz.

10.4 "Bırakma" senaryosu
Proje bırakılırsa ne olur?

Kod ortada kalır. Açık kaynak.

Topluluk devralabilir. Fork, maintainer.

Dokümantasyon kalır. Yarın devam edilebilir.

Kimse ölmez. Bu bir hobi, hayat değil.

Sonuç: Bırakma korkusu, tükenmeyi besler. Bırakma serbesttir; bu özgürlük, devam etmeyi kolaylaştırır.

11. Sürüm ve Yayın Disiplini
11.1 Sürüm sıklığı
Ana sürüm: 6-12 ayda bir (büyük özellikler).

Küçük sürüm: 1-3 ayda bir (yeni özellik).

Yama: Gerektiğinde (hata düzeltme).

Nightly: Otomatik (main branch).

11.2 Yayın kontrol listesi
Her sürümde:

□ Testler geçti
□ CHANGELOG güncellendi
□ Sürüm numarası artırıldı
□ README güncellendi
□ Örnek proje test edildi
□ Kurulum test edildi
□ Git tag atıldı
□ Yayın notları yazıldı
□ Sosyal duyuru (varsa)
11.3 Yayın sonrası
Kullanıcı geri bildirimi topla.

Hataları önceliklendir.

Sıcak yama (hotfix) gerekirse hızlı.

Sonra dinlen.

11.4 Yayın baskısı yok
Kimse beklemiyor.

Deadline yok.

"Bu hafta çıkarmalıyım" yok.

Kalite > hız.

Ama bitmemiş > sonsuz mükemmeliyetçilik.

12. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
Tükenme (burnout)	Yüksek	Yüksek	Düzenli mola, sağlık, gerçekçi hedef
Kapsam genişlemesi	Yüksek	Yüksek	Net MVP, "hayır" listesi, yazılı gerekçe
Motivasyon kaybı	Yüksek	Orta	Küçük hedefler, topluluk, mola
Proje yarım kalır	Orta	Yüksek	Küçük MVP, dikey dilim, çalışan teslim
Zaman tahmini şaşar	Yüksek	Orta	2.5x çarpanı, esnek plan
Bilgi eksikliği	Orta	Orta	Öğrenmeye zaman ayır, topluluğa sor
Hata birikimi	Orta	Orta	Düzenli test, teknik borç yönetimi
Topluluk ilgisizliği	Yüksek	Düşük	Beklenti yönetimi, sabır
Yalnızlık	Orta	Orta	Topluluk, arkadaş, toplantı (opsiyonel)
Ölü kod	Orta	Düşük	Düzenli temizlik
Bağımlılık krizi	Orta	Orta	Lisans kontrolü, minimal bağımlılık
Windows sürüm farkı	Orta	Orta	Test matrisi
Proje ölür	Düşük	Düşük	Açık kaynak; topluluk devralır
13. Başarı Metrikleri (Tek Kişi İçin)
13.1 Kişisel metrikler
Haftalık commit sayısı (hedef: 3-10)

Haftalık kod saati (hedef: 10-20)

Tamamlanan özellik (aylık)

Kapanan issue (aylık)

Mutluluk düzeyi (1-10, subjektif)

13.2 Proje metrikleri
Test kapsamı (hedef: kritik yol %80+)

Açık issue sayısı (trend aşağı)

Sürüm sıklığı (hedef: 1-3 ay)

Katkıcı sayısı (hedef: ilk yıl 1-5)

Yıldız sayısı (gösterge, hedef değil)

13.3 Uyarı metrikleri
2 hafta kod yok → mola mı, tükenme mi?

1 ay özellik yok → kapsam mı, tıkanma mı?

Sürekli aynı hata → test eksik mi?

Sürekli yorgunluk → tempo düşür.

14. Araçlar ve Otomasyon
14.1 Zorunlu araçlar
Git + GitHub (versiyon, iş birliği)

GitHub Actions (CI/CD)

EditorConfig + formatter (stil)

Test framework (xUnit, NUnit)

Paket yöneticisi (NuGet)

Issue/PR şablonları

Dependabot (bağımlılık güncelleme)

14.2 Yardımcı araçlar
Conventional Commits (commit mesajı)

Semantic Release (otomatik sürüm)

CodeQL (güvenlik analizi)

Coverage raporu (test kapsamı)

Mermaid (diyagram)

Docusaurus/MkDocs (dokümantasyon sitesi, ileride)

14.3 Kişisel üretkenlik
Todo listesi (basit; Notion, Obsidian, kağıt)

Zaman takibi (Toggl, WakaTime — opsiyonel)

Not alma (Obsidian, OneNote)

Odak modu (bildirim kapat, Pomodoro)

14.4 Otomasyon fırsatları
Sürüm notu üretimi (changelog)

Paketleme (build script)

Lisans kontrolü

Test çalıştırma

Yayın (release)

Dokümantasyon yayını

15. Öğrenme ve Gelişim
15.1 Öğrenme yaklaşımı
Öğrenerek yap. Mükemmel öğrenip başlama.

Küçük dene. Prototip yap.

Hata yap. Hata öğretir.

Belgele. Öğrendiğini yaz.

15.2 Öğrenme alanları
PDF üretimi (kütüphane, ICC)

Görüntü işleme (metadata, thumbnail)

Masaüstü UI (WPF/Avalonia)

Renk yönetimi (temel)

Baskı bilgisi (bleed, gutter)

15.3 Zaman bütçesi
Haftada 1-2 saat öğrenme.

Yeni teknoloji öğrenirken proje yavaşlar; kabul et.

Öğrenmeyi projeye entegre et; ayrı öğrenme projesi açma.

15.4 Topluluk öğrenmesi
Açık kaynak projelere bak (nasıl yapıyorlar?).

GitHub'da örnek kod incele.

Blog yaz, öğrendiğini paylaş.

16. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Gelir baskısı yok; zaman baskısı da azalır.
02 — Açık Kaynak	Tek kişi yükünü toplulukla paylaşmanın yolu.
03 — Kullanıcı Profilleri	Baskı evi entegrasyonu tek kişi için sürdürülemez; doğru karar.
05 — Zaman	Bu karar, zamanın gerçekçi planlanmasını sağlar.
06 — Bütçe	Ticari araç/kütüphane kullanmama kararını güçlendirir.
07 — Rakip Özellikleri	Kapsam disiplini, rakip özelliklerini seçerek almayı gerektirir.
17. Açık Sorular (Sonraki Kararlara Bırakılan)
Kesin çalışma ritmi ne olacak? → Kişisel tercih

Topluluk kanalları ne zaman açılacak? → Karar 02

Ne zaman ilk yayın? → Karar 05 (zaman)

Bağış altyapısı ne zaman? → Karar 06 (bütçe)

Kod imzalama sertifikası? → Karar 06

18. Özet
NexAlbum tek bir geliştirici tarafından, gönüllü, sürdürülebilir bir şekilde geliştirilecektir. Bu karar:

Kapsamı sınırlar. ("Hayır" listesi, MVP, kabul kuralları)

Zamanı gerçekçi planlar. (2.5x çarpan, düzenli ritim)

Kod kalitesini korur. (Basitlik, test, dokümantasyon)

Topluluğu teşvik eder. (Açık kaynak, katkı, paylaşım)

Tükenmeyi önler. (Mola, sağlık, keyif)

Otomasyonu zorunlu kılar. (CI/CD, test, lisans)

Bırakmayı serbest bırakır. (Topluluk devralır)

Bu karar, "mükemmel bir şey yapmak istiyorum, zaman alabilir" ifadesini gerçekçi bir çerçeveye oturtur: Mükemmel değil, sürdürülebilir; bitmemiş değil, çalışan; hızlı değil, düzenli. En önemlisi: keyif alarak.

Onay: Bu karar, ürünün sürdürülebilirlik ilkesinin temelidir.