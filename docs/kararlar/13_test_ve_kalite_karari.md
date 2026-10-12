Karar Dokümanı 13 — Test ve Kalite Kararı
Proje: NexAlbum
Karar no: 13
Konu: Test stratejisi, kalite standartları, CI/CD, kod inceleme ve kabul kriterleri
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 02 (Açık Kaynak), 04 (Tek Kişi), 05 (Zaman), 08 (Teknoloji), 09 (Veri Modeli), 10 (Şablon Motoru), 11 (Çıktı), 12 (UI/UX)

1. Karar Özeti
NexAlbum'de kalite, sonradan eklenen bir katman değil, sürecin parçasıdır.

Test piramidi: Bol birim, orta entegrasyon, az E2E.

Kapsam hedefi: Kritik yol %80+, genel %70+.

Otomasyon: Her PR'da derleme + test + lisans + stil.

Manuel test: Her sürüm öncesi kontrol listesi.

Kod inceleme: Tek kişi olsa bile kendi kodunu gözden geçirme ritüeli.

Kalite kapıları: Testler geçmeden main'e birleşme yok.

Determinizm: Test edilebilirlik için saf fonksiyonlar, tohum.

Ölçüm: Kapsam, hata sayısı, performans, erişilebilirlik.

Şeffaflık: Test sonuçları PR'da görünür.

Sürekli iyileştirme: Hata → regresyon testi → bir daha olmaz.

Bu karar, 04 (Tek Kişi) ve 08 (Teknoloji) kararlarının kalite boyutudur ve 05 (Zaman) yol haritasının her aşamasında uygulanır.

2. Kalite Felsefesi
2.1 İlkeler
Kalite herkesin işi. Test yazmak sadece "test aşaması" değil.

Hızlı geri bildirim. Testler saniyeler içinde çalışmalı.

Güven. Test varsa, refactor güvenli.

Regresyon önleme. Her hata bir test doğurur.

Basitlik. Test kodu, üretim kodu kadar önemli.

Gerçekçilik. Test, gerçek kullanımı yansıtmalı.

Ölçülebilirlik. "İyi" subjektif; metrik objektif.

Sürdürülebilirlik. Test bakımı kolay olmalı.

Şeffaflık. Sonuçlar açık.

İstikrar. "Flaky" test = kaldır ya da düzelt.

2.2 Anti-ilkeler
❌ "Test yok, sonra eklerim" (asla eklenmez)

❌ Kapsam için test yazmak (anlamsız)

❌ Test kırılınca görmezden gelmek

❌ Flaky test bırakmak

❌ Manuel testi otomasyonsuz bırakmak

❌ "Benim makinemde çalışıyor" bahanesi

❌ Kaliteyi sona bırakmak

2.3 Kalite tanımı
Kalite = ürünün vaat ettiğini doğru, hızlı, güvenilir şekilde yerine getirmesi.

Alt boyutlar:

Doğruluk: Baskı ölçüsü doğru mu?

Güvenilirlik: Çökme yok, veri kaybı yok.

Performans: Hızlı, akıcı.

Kullanılabilirlik: Anlaşılır, öğrenilebilir.

Sürdürülebilirlik: Kod bakımı kolay.

Erişilebilirlik: Herkes kullanabilir.

3. Test Piramidi
3.1 Piramit
text
              /\
             /  \        E2E (5-15 test)
            /____\       — Uçtan uca, yavaş, kırılgan
           /      \
          /        \     Entegrasyon (30-80 test)
         /__________\    — Modüller arası, orta hız
        /            \
       /              \  Birim (300-800 test)
      /________________\ — İzole, hızlı, çok
3.2 Oranlar
Katman	Sayı	Süre	Kapsam
Birim	300-800	< 5 sn	%60-70
Entegrasyon	30-80	< 30 sn	%20-30
E2E	5-15	< 2 dk	%5-10
Manuel	Kontrol listesi	Değişken	Kritik akışlar
3.3 Test süresi hedefleri
Tetikleyici	Süre
Yerel (birim)	< 5 sn
Yerel (entegrasyon)	< 30 sn
PR (tümü)	< 5 dk
Sürüm öncesi (tümü + E2E)	< 15 dk
Kural: Testler 5 dakikayı aşarsa, insanlar çalıştırmaz.

4. Birim Testleri
4.1 Amaç
Her sınıfın/metodun izole davranışını doğrula.

4.2 Kapsam alanları
Alan	Örnek
Domain	AlbumProject, AlbumPage, Slot
Layout Engine	Sınıflandırma, gruplama, eşleştirme
Preflight	Her kontrol
JSON	Serileştirme, şema doğrulama
Migration	v1 → v2
Checksum	SHA-256
Yol işleme	Normalizasyon, güvenlik
4.3 Birim testi ilkeleri
İzole. Dış bağımlılık mock'lanır.

Hızlı. < 10 ms/her test.

Deterministik. Aynı girdi → aynı çıktı.

Okunabilir. Test = dokümantasyon.

Bir davranış. Bir test = bir senaryo.

Bağımsız. Testler sırayla çalışabilir.

4.4 İsimlendirme
Format: MethodName_Scenario_ExpectedBehavior

Örnekler:

csharp
Classify_PortraitPhoto_ReturnsPortraitOrientation
Layout_LockedPage_DoesNotChangePage
Validate_ZeroBleed_ReturnsError
Migrate_V1ToV2_PreservesAllFields
4.5 Yapı: AAA
csharp
[Fact]
public void Layout_100Photos_ProducesCorrectPageCount()
{
    // Arrange
    var photos = GeneratePhotos(100);
    var profile = CreateTestProfile();
    var engine = new LayoutEngine();

    // Act
    var result = engine.Layout(photos, profile);

    // Assert
    Assert.Equal(25, result.Pages.Count);
}
4.6 Test verisi
Fabrika metotları: CreateTestProfile(), GeneratePhotos(n).

Builder pattern: Karmaşık nesneler için.

Sabit seed: Rastgelelik için.

Örnek dosyalar: samples/tests/.

4.7 Mock kullanımı
Moq (Karar 08).

Sadece dış bağımlılık: IO, zaman, rastgelelik.

Domain mock'lanmaz.

Aşırı mock = kötü tasarım işareti.

4.8 Örnek: Layout Engine testleri
csharp
[Theory]
[InlineData(1, PhotoOrientation.Landscape)]
[InlineData(1.5, PhotoOrientation.Landscape)]
[InlineData(0.5, PhotoOrientation.Portrait)]
[InlineData(1.0, PhotoOrientation.Square)]
public void Classify_ReturnsCorrectOrientation(double ratio, PhotoOrientation expected)
{
    var result = PhotoClassifier.Classify(ratio * 100, 100);
    Assert.Equal(expected, result);
}

[Fact]
public void Layout_PhotosWithDifferentOrientations_PreservesAspectRatio()
{
    var photos = new[]
    {
        CreatePhoto(6000, 4000),  // Landscape
        CreatePhoto(4000, 6000),  // Portrait
    };
    var result = new LayoutEngine().Layout(photos, profile);

    foreach (var placement in result.Pages.SelectMany(p => p.Placements))
    {
        var photo = photos.First(p => p.Id == placement.AssetId);
        var photoRatio = (double)photo.Width / photo.Height;
        var slotRatio = (double)placement.SlotWidth / placement.SlotHeight;
        Assert.Equal(photoRatio, slotRatio, 3);
    }
}
5. Entegrasyon Testleri
5.1 Amaç
Birden fazla modülün birlikte çalışmasını doğrula.

5.2 Kapsam alanları
Alan	Örnek
Proje kaydet/aç	Round-trip, veri kaybı yok
Profil + Layout	Profil değişince layout etkilenir
Layout + Çıktı	Layout → PDF
Fotoğraf + Metadata	EXIF okuma → yön
Migration + Şema	Eski proje → yeni şema
Kilit + Otomatik	Kilitli sayfa korunur
5.3 Entegrasyon testi ilkeleri
Gerçek bağımlılık (mümkünse).

Geçici dosya kullan (test klasörü).

Temizlik: Test sonrası dosya silinir.

İzolasyon: Testler birbirini etkilemez.

5.4 Örnek: Proje kaydet/aç
csharp
[Fact]
public async Task SaveAndLoad_ComplexProject_PreservesAllData()
{
    // Arrange
    var project = CreateComplexProject(); // 20 sayfa, 100 fotoğraf
    var path = Path.GetTempFileName() + ".albumproj";

    try
    {
        // Act
        await ProjectService.SaveAsync(project, path);
        var loaded = await ProjectService.LoadAsync(path);

        // Assert
        Assert.Equal(project.Id, loaded.Id);
        Assert.Equal(project.Pages.Count, loaded.Pages.Count);
        Assert.Equal(project.Assets.Count, loaded.Assets.Count);
        Assert.Equal(project.ProfileId, loaded.ProfileId);
        // ... her alan karşılaştırılır
    }
    finally
    {
        File.Delete(path);
    }
}
5.5 Örnek: PDF çıktısı
csharp
[Fact]
public async Task ExportPdf_30x30Profile_ProducesCorrectPageSize()
{
    // Arrange
    var project = CreateProjectWithProfile("30x30");
    var outputPath = Path.GetTempFileName() + ".pdf";

    try
    {
        // Act
        await PdfExporter.ExportAsync(project, outputPath);

        // Assert
        using var pdf = PdfReader.Open(outputPath);
        var page = pdf.Pages[0];
        Assert.Equal(850.4, page.Width.Point, 1);  // 300mm = 850.4pt
        Assert.Equal(850.4, page.Height.Point, 1);
    }
    finally
    {
        File.Delete(outputPath);
    }
}
6. Uçtan Uca (E2E) Testleri
6.1 Amaç
Gerçek kullanıcı akışını doğrula.

6.2 Kapsam alanları
Senaryo	Kritiklik
Yeni proje → 10 foto → otomatik → PDF	Yüksek
Proje aç → düzenle → kaydet → aç	Yüksek
100 fotoğraf → otomatik → JPG	Orta
Eksik fotoğraf → uyarı → düzelt	Orta
Kilitli sayfa → yeniden oluştur	Orta
Türkçe karakter → kaydet/aç	Düşük
6.3 E2E araçları
UI otomasyonu: FlaUI (WPF için, MIT lisansı).

Alternatif: TestStack.White.

Kısıt: Kırılgan; az tutulur.

Strateji: Sadece kritik akışlar.

6.4 E2E ilkeleri
Az sayıda. 5-15 yeterli.

Kritik akışlar. Sadece ana senaryolar.

Deterministik. Bekleme süreleri sabit.

Temiz ortam. Her test baştan.

Hata durumunda ekran görüntüsü.

6.5 Örnek senaryo
text
1. Uygulamayı başlat
2. "Yeni Proje" tıkla
3. Profil seç (30×30)
4. "Fotoğraf Ekle" tıkla
5. 10 fotoğraf seç
6. "Otomatik Oluştur" tıkla
7. Sonucu doğrula (10 fotoğraf yerleşmiş)
8. "PDF Olarak Dışa Aktar" tıkla
9. PDF dosyasının varlığını doğrula
10. PDF sayfa sayısını doğrula
6.6 Görsel regresyon
Snapshot karşılaştırma.

Araç: ApprovalTests.NET (Apache 2.0).

Kullanım: Kritik ekranlar.

Kısıt: Platform farklılıkları.

7. Manuel Test
7.1 Amaç
Otomasyonun yakalayamadığı durumları test et.

7.2 Ne zaman?
Her sürüm öncesi.

Yeni özellik sonrası.

Büyük refactor sonrası.

Kullanıcı geri bildirimi sonrası.

7.3 Kontrol listesi
Sürüm öncesi manuel test:

□ Uygulama açılıyor (< 3 sn)
□ Yeni proje oluşturulabiliyor
□ Profil seçilebiliyor
□ Fotoğraf içe aktarılabiliyor (100+)
□ Thumbnail'lar görünüyor
□ Otomatik oluştur çalışıyor
□ Elle düzenleme çalışıyor
□ Sayfa sıralama çalışıyor
□ Kaydet/aç çalışıyor
□ PDF çıktısı doğru ölçüde
□ JPG önizleme doğru
□ SVG açılıyor (Inkscape)
□ Preflight hataları gösteriyor
□ Türkçe karakterli dosya adları çalışıyor
□ Kısayollar çalışıyor
□ Tema değiştirilebiliyor
□ Hata mesajları anlaşılır
□ Çökme yok
7.4 Test ortamları
Ortam	Amaç
Geliştirici makinesi	Günlük
Windows 10 VM	Sürüm öncesi
Windows 11 VM	Sürüm öncesi
Temiz Windows	Kurulum testi
7.5 Test verileri
Küçük: 10 fotoğraf, 3 sayfa.

Orta: 100 fotoğraf, 25 sayfa.

Büyük: 1000 fotoğraf, 100 sayfa.

Kenar durumları: Bozuk JPEG, eksik dosya, Türkçe karakter.

7.6 Kayıt
Her manuel test oturumu kaydedilir.

Bulunan hatalar issue olarak açılır.

Tekrarlanabilir adımlar yazılır.

Ekran görüntüsü eklenir.

8. Kod Kapsamı (Code Coverage)
8.1 Hedefler
Alan	Hedef
Kritik yol (layout, preflight, kaydet/aç)	≥ %80
Domain	≥ %85
Genel	≥ %70
UI	≥ %40 (zor)
8.2 Araçlar
Coverlet (MIT).

ReportGenerator (Apache 2.0).

Rapor: HTML + PR yorumu.

8.3 Ne ölçülmez?
UI kod (XAML).

Otomatik üretilen kod.

Migration (bir kez çalışır).

Program.cs (giriş noktası).

8.4 Kapsam ≠ kalite
Yüksek kapsam, iyi test demek değil.

Düşük kapsam, kötü işarettir.

Kalite göstergesi, kapsam değil hata yakalama.

8.5 Kapsam kapısı
PR'da kapsam düşerse uyarı.

Düşüş > %2 ise gerekçe gerekli.

Artış teşvik edilir.

9. CI/CD (GitHub Actions)
9.1 Workflow'lar
Workflow	Tetikleyici	Süre	İçerik
build.yml	Push/PR	< 2 dk	Derleme
test.yml	Push/PR	< 5 dk	Birim + entegrasyon
e2e.yml	Push (main), manuel	< 15 dk	E2E
release.yml	Tag	< 10 dk	Paketleme + yayın
codeql.yml	Haftalık	Değişken	Güvenlik analizi
licenses.yml	Push/PR	< 1 dk	Lisans kontrolü
lint.yml	Push/PR	< 1 dk	Kod stili
9.2 Kalite kapıları
PR birleşmeden önce:

□ Derleme başarılı
□ Tüm birim testleri geçti
□ Tüm entegrasyon testleri geçti
□ Kod stili uyumlu
□ Lisans kontrolü geçti
□ Kod kapsamı düşmedi
□ Yeni bağımlılık GPL uyumlu
□ Dokümantasyon güncel (gerekiyorsa)
9.3 CI başarısızlığı
Kırmızı CI = dur. Önce düzelt.

Flaky test → geçici olarak [Skip], issue açılır, en kısa sürede düzeltilir.

Altyapı hatası → yeniden çalıştır.

9.4 Branch koruması
main branch:

Doğrudan push yasak.

PR zorunlu.

CI geçmeli.

En az 1 onay (tek kişi olsa bile, self-review ritüeli).

9.5 Sürüm otomasyonu
Tag atılınca (v0.1.0):

Derleme (Release).

Testler.

Paketleme (Inno Setup + ZIP).

GitHub Release oluşturma.

Sürüm notları (CHANGELOG'dan).

10. Kod İnceleme (Tek Kişi)
10.1 Neden?
Tek kişi olsa bile kod inceleme:

Hataları erken yakalar.

Kod kalitesini artırır.

Bilgi birikimini korur.

Gelecekteki katkıcılar için örnek olur.

10.2 Süreç
Yaz.

Bekle. En az 1 saat (ideali 1 gün).

İncele. Farklı gözle oku.

Düzelt.

Birleştir.

10.3 İnceleme kontrol listesi
□ Kod amacını yerine getiriyor mu?
□ Test var mı?
□ İsimler açıklayıcı mı?
□ Basitleştirilebilir mi?
□ Kenar durumları düşünülmüş mü?
□ Hata yönetimi var mı?
□ Performans sorunu var mı?
□ Güvenlik riski var mı?
□ Dokümantasyon gerekli mi?
□ Ölü kod var mı?
10.4 Otomatik kontrol
Roslyn Analyzer: C# kalite kuralları.

EditorConfig: Stil kuralları.

dotnet format: Otomatik biçimlendirme.

CodeQL: Güvenlik.

10.5 PR şablonu
markdown
## Ne yapar?
Kısa açıklama.

## Neden?
Motivasyon.

## Nasıl test edildi?
- [ ] Birim testleri
- [ ] Manuel test

## Kontrol listesi
- [ ] Testler geçti
- [ ] Dokümantasyon güncel
- [ ] CHANGELOG güncellendi (gerekiyorsa)
11. Kod Kalite Standartları
11.1 C# standartları
.NET 8 dil özellikleri:

Records (DTO'lar için)

Pattern matching

Nullable reference types (aktif)

File-scoped namespaces

Global using

Stil:

4 space indent

_camelCase private field

PascalCase public

camelCase local

I prefix interface

Uzunluk:

Metot: < 50 satır (ideal)

Sınıf: < 500 satır

Dosya: < 1000 satır

11.2 EditorConfig
Depoda .editorconfig:

ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true
11.3 Analyzer kuralları
Hata: Nullable, güvenlik.

Uyarı: Performans, okunabilirlik.

Öneri: Stil.

11.4 İsimlendirme örnekleri
csharp
// ✅ İyi
public class LayoutEngine { }
private readonly ILogger<LayoutEngine> _logger;
public async Task<LayoutResult> LayoutAsync(...) { }

// ❌ Kötü
public class LE { }
private ILogger log;
public async Task<LayoutResult> DoLayout(...) { }
11.5 Yorumlar
"Neden" yaz, "ne" değil.

Karmaşık algoritma: kısa açıklama.

Public API: XML doc.

TODO: issue numarası ile.

csharp
// ✅ İyi: Neden
// Greedy algoritma: O(n*m), Macar O(n^3). MVP için yeterli.
var result = GreedyMatch(photos, slots);

// ❌ Kötü: Ne
// Fotoğrafları eşleştir
var result = GreedyMatch(photos, slots);
11.6 Teknik borç
TODO: Geçici çözüm.

HACK: Bilinçli kötü çözüm.

FIXME: Düzeltilmeli.

Her biri issue numarası ile.

csharp
// TODO(#123): Macar algoritmasına geç
// FIXME(#456): Büyük albümde yavaş
12. Performans Testleri
12.1 Araçlar
BenchmarkDotNet (MIT).

Manuel ölçüm (kronometre).

dotnet-trace (profil).

12.2 Ölçüm alanları
Alan	Metrik	Hedef
Layout engine	1000 fotoğraf	< 10 sn
PDF üretimi	100 sayfa	< 5 dk
JPG önizleme	20 sayfa	< 20 sn
Proje kaydetme	100 fotoğraf	< 3 sn
Proje açma	100 fotoğraf	< 3 sn
Thumbnail	100 fotoğraf	< 5 sn
Uygulama açılış	Soğuk	< 3 sn
UI yanıt	Düğme	< 100 ms
12.3 Bellek
Senaryo	Hedef
Boşta	< 100 MB
100 fotoğraf açık	< 500 MB
1000 fotoğraf açık	< 2 GB
PDF üretimi	< 2 GB
12.4 Benchmark örneği
csharp
[MemoryDiagnoser]
public class LayoutEngineBenchmarks
{
    [Params(100, 500, 1000)]
    public int PhotoCount { get; set; }

    [Benchmark]
    public LayoutResult AutoLayout()
    {
        return _engine.Layout(_photos, _profile);
    }
}
12.5 Performans regresyonu
Baseline: Ölçüm kaydedilir.

PR: Yeni ölçüm.

Karşılaştırma: %20+ yavaşlama → uyarı.

Kabul: Gerekçe ile.

12.6 Optimizasyon önceliği
Algoritma: O(n²) → O(n log n).

Bellek: Gereksiz kopya.

IO: Batch, async.

Paralel: CPU yoğun işler.

Cache: Tekrar eden hesaplar.

Micro-optimization: En son.

13. Güvenlik Testleri
13.1 Araçlar
CodeQL (GitHub).

Dependabot (bağımlılık).

Secret scanning (GitHub).

13.2 Test alanları
Alan	Test
ZIP traversal	../ engeli
Zip bomb	Sıkıştırma oranı
JSON derinlik	Aşırı iç içe
Dosya yolu	.. temizleme
ICC dosyası	Kötü niyetli dosya
Büyük dosya	Boyut limiti
13.3 Örnek test
csharp
[Fact]
public void Unzip_PathTraversal_ThrowsException()
{
    var zip = CreateMaliciousZip("../../etc/passwd");
    Assert.Throws<SecurityException>(() => Unzip(zip));
}

[Fact]
public void Deserialize_DeeplyNestedJson_Rejects()
{
    var json = GenerateDeepJson(depth: 1000);
    Assert.Throws<JsonException>(() => Deserialize(json));
}
13.4 Bağımlılık güvenliği
Dependabot: Haftalık tarama.

Güncelleme: Kritik güvenlik → hemen.

Test: Güncelleme sonrası tüm testler.

Rollback: Sorun çıkarsa geri al.

14. Erişilebilirlik Testleri
14.1 Alanlar
Alan	Test
Klavye	Tüm eylemler klavyeyle
Ekran okuyucu	Narrator (Windows)
Yüksek kontrast	Windows ayarı
Renk körlüğü	Simülatör
Yazı boyutu	%200 ölçek
Fokus	Görünür gösterge
14.2 Kontrol listesi
□ Tüm düğmeler klavyeyle erişilebilir
□ Fokus sırası mantıklı
□ Fokus göstergesi net
□ Renk tek başına anlam taşımaz
□ Yüksek kontrast modunda okunur
□ Ekran okuyucu temel bilgi verir
□ Kısayollar çalışır
14.3 Otomasyon
Axe.Windows (erken aşama).

Manuel test (şu an için).

15. Yerelleştirme Testleri
15.1 Kontroller
Kontrol	Açıklama
Türkçe karakterler	İ, ı, ş, ğ, ü, ö, ç
Uzun metin	Almanca gibi uzun diller
RTL	Gelecekte
Tarih formatı	dd.MM.yyyy
Sayı formatı	1.234,56
Ölçü birimi	mm, cm
Kısayol çakışması	Farklı diller
15.2 Örnek test
csharp
[Fact]
public void SaveProject_TurkishCharacters_PreservesCorrectly()
{
    var project = new AlbumProject
    {
        Name = "Ahmet & Zeynep'in Düğünü — İstanbul",
        Description = "Çığlık atan sevinç..."
    };
    var path = SaveAndLoad(project);
    Assert.Equal(project.Name, path.Name);
    Assert.Equal(project.Description, path.Description);
}
16. Kabul Kriterleri (MVP)
16.1 Fonksiyonel
Doküman 14. bölümdeki 12 madde geçerli:

□ Özel ölçüde albüm oluşturulabiliyor
□ 100+ fotoğraf içe aktarılıyor, UI donmuyor
□ 10+ şablon çalışıyor
□ Otomatik yerleşim fotoğrafı esnetmiyor
□ Elle düzenleme + sıralama
□ Kaydet/aç düzeni koruyor
□ JPG önizleme doğru
□ PDF ölçüsü + bleed doğru
□ Preflight eksikleri raporluyor
□ Hata mesajları anlaşılır
□ Photoshop/CorelDRAW olmadan çalışıyor
□ PSD/CDR vaat edilmiyor
16.2 Kalite
□ Test kapsamı: Kritik yol %80+, genel %70+
□ CI yeşil
□ Manuel test kontrol listesi tamamlandı
□ Performans hedefleri karşılandı
□ Erişilebilirlik kontrol listesi tamamlandı
□ Türkçe karakter testleri geçti
□ Çökme yok
□ Veri kaybı yok
16.3 Dokümantasyon
□ README güncel
□ Kullanıcı kılavuzu mevcut
□ CHANGELOG güncel
□ Karar dokümanları tamam
□ Katkı rehberi (CONTRIBUTING) mevcut
□ Lisans dosyası mevcut
16.4 Paketleme
□ Kurulum dosyası (Inno Setup)
□ Taşınabilir ZIP
□ Temiz Windows'ta test
□ Kaldırma testi
□ Güncelleme testi
17. Sürüm Öncesi Kontrol Listesi
Her sürüm öncesi:

17.1 Kod
□ Tüm testler geçti
□ Kod kapsamı düştü mü?
□ Analyzer uyarısı yok
□ Ölü kod temizlendi
□ TODO/FIXME gözden geçirildi
□ Bağımlılıklar güncel
17.2 Dokümantasyon
□ CHANGELOG güncellendi
□ README güncellendi
□ Sürüm numarası artırıldı
□ Ekran görüntüleri güncel
17.3 Manuel test
□ Kontrol listesi tamamlandı
□ Windows 10 test
□ Windows 11 test
□ Türkçe karakter test
□ Büyük albüm test (100+)
□ Kurulum test
17.4 Paketleme
□ Inno Setup derlemesi
□ Taşınabilir ZIP
□ Hash doğrulama
□ İmza (varsa)
17.5 Yayın
□ Git tag atıldı
□ GitHub Release oluşturuldu
□ Sürüm notları yazıldı
□ Ekran görüntüleri eklendi
□ İndirme bağlantıları test edildi
18. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
Test yazmaya zaman yok	Yüksek	Yüksek	Test zorunlu; PR kapısı
Flaky test	Orta	Orta	Hemen düzelt veya kaldır
Kapsam düşer	Orta	Orta	PR'da uyarı; gerekçe
Manuel test atlanır	Orta	Yüksek	Kontrol listesi zorunlu
Performans regresyonu	Orta	Orta	Baseline karşılaştırma
Güvenlik açığı	Düşük	Yüksek	CodeQL, Dependabot
Erişilebilirlik eksik	Orta	Orta	Kontrol listesi
CI yavaşlar	Orta	Orta	Paralel, cache
Yanlış pozitif test	Orta	Düşük	Kalibrasyon
Test bakımı zor	Orta	Orta	Basit test, fabrika metodu
Kapsam için test	Orta	Orta	Kalite odaklı test
E2E kırılgan	Yüksek	Orta	Az tut, kritik akış
Sürüm stresi	Orta	Orta	Otomasyon, kontrol listesi
Kullanıcı hatası	Orta	Orta	Hata mesajları, geri al
19. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Test araçları ücretsiz
02 — Açık Kaynak	Testler kamuya açık; katkı
03 — Kullanıcı Profilleri	Profil şema testleri
04 — Tek Kişi	Test disiplini; otomasyon
05 — Zaman	Her aşamada test
06 — Bütçe	Ücretsiz test araçları
07 — Rakip Özellikleri	Kalite hedefi
08 — Teknoloji	xUnit, Moq, FluentAssertions
09 — Veri Modeli	Şema, migration testleri
10 — Şablon Motoru	Layout engine testleri
11 — Çıktı	PDF/JPG/SVG testleri
12 — UI/UX	UI testleri, erişilebilirlik
20. Açık Sorular (Sonraki Kararlara Bırakılan)
Axe.Windows ne zaman? → v1.1

Görsel regresyon ne zaman? → v1.1

Load test (1000+ kullanıcı)? → Bulut olmadığı için gereksiz

Fuzzing? → v1.2

Mutation testing? → v2.0

Performans CI'da ne zaman? → v1.1

21. Özet
NexAlbum'in test ve kalite yaklaşımı:

Test piramidi: Bol birim, orta entegrasyon, az E2E.

Kapsam: Kritik %80+, genel %70+.

Otomasyon: Her PR'da CI (derleme, test, lisans, stil).

Kalite kapıları: Testler geçmeden birleşme yok.

Manuel test: Her sürüm öncesi kontrol listesi.

Kod inceleme: Tek kişi bile self-review ritüeli.

Standartlar: C# 12, nullable, EditorConfig, Analyzer.

Performans: Benchmark, baseline, regresyon kontrolü.

Güvenlik: CodeQL, Dependabot, path traversal testleri.

Erişilebilirlik: Klavye, kontrast, ekran okuyucu.

Yerelleştirme: Türkçe karakter, tarih, sayı.

Kabul kriterleri: Fonksiyonel + kalite + dokümantasyon + paketleme.

Sürüm kontrol listesi: Kod, doküman, test, paket, yayın.

Bu karar, "kalite sonradan eklenmez, baştan inşa edilir" ilkesini uygular. Tek geliştirici için sürdürülebilir, otomasyona dayalı, ölçülebilir bir kalite sistemi kurar.

Onay: Bu karar, ürünün test ve kalite temelinin belgesidir.