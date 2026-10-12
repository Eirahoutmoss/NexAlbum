# Karar Dokümanı 01 — Ücretsiz Ürün Modeli

**Proje:** NexAlbum
**Karar no:** 01
**Konu:** Ürünün ücretsiz olması ve bunun tüm katmanlara etkisi
**Durum:** Kesinleşti
**Tarih:** (doldurulacak)
**İlgili kararlar:** 02 (Açık Kaynak), 04 (Tek Kişi Geliştirme), 06 (Bütçe)

---

## 1. Karar Özeti

NexAlbum **tamamen ücretsiz** bir masaüstü uygulaması olacaktır.

- Ücretli sürüm yok.
- Deneme sürümü yok.
- Özellik kilidi yok.
- Reklam yok.
- Zorunlu hesap yok.
- Abonelik yok.
- Uygulama içi satın alma yok.
- Filigran yok.

Tüm özellikler, ilk günden itibaren, her kullanıcı için açıktır.

Bu karar; 02 numaralı "Açık Kaynak" kararıyla birlikte alınmıştır ve
ikisi birbirini destekler. Ücretsiz olmak, açık kaynak olmanın doğal
sonucudur; ayrıca ürünün benimsenmesini hızlandırır.

---

## 2. "Ücretsiz" Ne Anlama Geliyor?

### 2.1 Kullanıcı açısından

- İndirme ve kurulum için ödeme yok.
- Kayıt/üyelik zorunluluğu yok.
- Tüm özellikler ilk günden açık.
- Reklam görmez.
- Verisi cihazından çıkmaz (yerel çalışma).
- İstediği zaman siler, istediği zaman yeniden kurar.
- Ticari işlerinde de kullanabilir (ticari kullanım serbest).
- Müşteri albümleri üretip para kazanabilir; uygulama buna engel olmaz.

### 2.2 Geliştirici açısından

- Gelir beklentisi yok.
- Lisans sunucusu, ödeme altyapısı, aktivasyon kodu yok.
- Korsanlıkla mücadele derdi yok.
- Kullanıcı sayısı bir başarı metriği değil; **kullanıcı memnuniyeti** ve
  **ürünün işe yaraması** esas.
- Kaynak kodu açık olduğu için isteyen çatallayabilir (fork).

### 2.3 Mimari açısından

- **Lisans katmanı yok.** Kod tabanında `LicenseManager`, `Activation`,
  `Trial`, `FeatureGate` gibi sınıflar bulunmayacak.
- **Telemetri zorunlu değil.** Varsa bile opt-in olacak ve anonim olacak.
- **Güncelleme kontrolü** isteğe bağlı; kullanıcı istemezse hiç ağ
  bağlantısı kurulmaz.
- **Çevrimdışı tam çalışma.** İnternet olmadan da tüm özellikler
  kullanılabilir.
- **Taşınabilir sürüm (portable)** kolayca mümkün: ZIP'i aç, çalıştır.

### 2.4 Dağıtım açısından

- GitHub Releases üzerinden yayın.
- Kurulum dosyası (MSIX veya Inno Setup) + taşınabilir ZIP.
- Kaynak koddan derleme talimatı README'de.
- Paket yöneticileri (winget, chocolatey) hedeflenebilir ama zorunlu
  değil.

---

## 3. "Ücretsiz" Ne Anlama Gelmiyor?

Bu kararın sınırlarını net çizmek, yanlış beklentileri önler.

- **Sınırsız destek anlamına gelmez.** Hata bildirimleri topluluk
  kanallarında ele alınır; birebir özel destek vaadi yoktur.
- **Sınırsız özellik anlamına gelmez.** Yol haritası kapsamı, tek
  geliştiricinin sürdürebileceği hızda ilerler (bkz. Karar 04).
- **Baskı evi entegrasyonu anlamına gelmez.** Kullanıcı kendi baskı
  profilini girer (bkz. Karar 03).
- **Bulut hizmeti anlamına gelmez.** Senkronizasyon, hesap, çevrimiçi
  şablon mağazası yoktur.
- **Profesyonel SLA anlamına gelmez.** "Yarın düzelteceğim" garantisi
  verilmez.
- **Reklamsız olması**, "hiçbir maliyeti yok" anlamına gelmez;
  geliştiricinin zamanı bir maliyettir ve bu gönüllü bir katkıdır.

---

## 4. Neden Ücretsiz?

### 4.1 Ürünün doğası

Fotoğraf albümü tasarımı, niş bir ihtiyaçtır. Bu nişte:
- Mevcut ticari çözümler pahalıdır (SmartAlbums, Fundy).
- Türkçe arayüz ve yerel baskı profilleri eksiktir.
- Windows'ta çalışan, hafif ve hızlı bir araç yoktur.

Ücretsiz + açık kaynak, bu boşluğu en hızlı doldurma yoludur.

### 4.2 Açık kaynakla uyum

Açık kaynak bir ürünün ücretli olması teknik olarak mümkündür ama:
- Kullanıcı ikili dosyayı kendisi derleyebilir.
- Lisans kontrolü kırılabilir.
- Topluluk güveni zedelenir.

Ücretsiz olmak, açık kaynak kararını tutarlı kılar.

### 4.3 Benimsenme hızı

Ücretsiz ürünler:
- Daha hızlı yayılır.
- Daha çok geri bildirim alır.
- Daha çok katkı çeker.
- Daha çok test edilir.

Tek geliştirici için bu, kaliteyi artırmanın en gerçekçi yoludur.

### 4.4 Etik ve erişilebilirlik

Fotoğrafçılıkla uğraşan herkesin pahalı yazılımlara bütçesi yoktur.
Ücretsiz olmak, ürünü:
- Öğrencilere,
- Yeni başlayanlara,
- Küçük stüdyolara,
- Gelişmekte olan pazarlara

açar.

---

## 5. Sürdürülebilirlik Planı

Ücretsiz olmak, "kaynak yok" anlamına gelmez. Aşağıdaki kanallar
**opsiyonel** olarak açık tutulur; hiçbiri zorunlu değildir.

### 5.1 Bağış (Donation)

- GitHub Sponsors
- Ko-fi / Buy Me a Coffee
- Liberapay
- Uygulama içinde "Bağış" düğmesi (açık, baskıcı olmayan, bir kez
  gösterilir, kapatılabilir)

**İlke:** Bağış, özellik kilidini açmaz. Bağış yapanla yapmayan aynı
ürüne sahiptir.

### 5.2 Katkı (Contribution)

- Kod katkısı (PR)
- Şablon katkısı (kullanıcı şablonları topluluk deposuna)
- Baskı profili katkısı (baskı evlerinden onaylı profiller)
- Çeviri katkısı
- Dokümantasyon katkısı
- Hata bildirimi ve test

### 5.3 Sponsorluk (İleride)

- Baskı evleri sponsor olabilir (ama ürüne müdahale edemez).
- Fotoğrafçılık toplulukları destekleyebilir.
- Eğitim kurumları kullanabilir.

**İlke:** Sponsor, ürün kararlarına karışmaz. Reklam yerleştirilmez.

### 5.4 Kendi kendine yeterlilik

- Sunucu maliyeti yok (yerel uygulama).
- Alan adı maliyeti minimal (GitHub Pages yeterli).
- Kod imzalama sertifikası opsiyonel (ileride topluluk bağışıyla).
- Tek geliştirici olduğu için maaş/maliyet baskısı yok.

---

## 6. Mimariye Yansımaları

Aşağıdaki kararlar, "ücretsiz" ilkesinden doğrudan türetilmiştir.

### 6.1 Yapılmayacaklar

- ❌ Lisans anahtarı doğrulama
- ❌ Çevrimiçi aktivasyon
- ❌ Özellik bayrakları (feature flags) ile kilit açma
- ❌ Kullanım limiti (sayfa sayısı, proje sayısı vb.)
- ❌ Filigran
- ❌ Zorunlu güncelleme
- ❌ Zorunlu hesap
- ❌ Zorunlu telemetri
- ❌ Reklam SDK'sı

### 6.2 Yapılacaklar

- ✅ Tüm özellikler açık
- ✅ Çevrimdışı tam çalışma
- ✅ Taşınabilir sürüm
- ✅ Opt-in anonim hata raporu (isteğe bağlı)
- ✅ Opt-in güncelleme kontrolü (isteğe bağlı)
- ✅ Açık kaynak lisansı (bkz. Karar 02)
- ✅ README'de net "ücretsiz ve açık kaynak" ibaresi
- ✅ Katkı rehberi (CONTRIBUTING.md)
- ✅ Davranış kuralları (CODE_OF_CONDUCT.md)

### 6.3 Kod tabanında isimlendirme

- `LicenseService` yerine → yok
- `TrialManager` yerine → yok
- `FeatureGate` yerine → yok
- `TelemetryService` → `OptInTelemetryService` (varsa)
- `UpdateChecker` → `OptionalUpdateChecker`

---

## 7. Kullanıcıya Verilen Sözler

Bu sözler, ürünün kimliğini oluşturur ve README'de açıkça yazılır:

1. **Her zaman ücretsiz kalacak.** Gelecekte ücretli sürüme geçilmeyecek.
2. **Özellikler kilitlenmeyecek.** Yeni özellikler herkese açık olacak.
3. **Reklam gösterilmeyecek.**
4. **Hesap zorunlu olmayacak.**
5. **Veriler cihazda kalacak.**
6. **Kaynak kodu açık olacak.**
7. **Çatallamak serbest olacak.**
8. **Ticari kullanım serbest olacak.**
9. **Bağış opsiyonel olacak, özellik açmayacak.**
10. **Geliştirici tek kişi olsa bile, ürün topluluğa ait olacak.**

---

## 8. Riskler ve Önlemler

| Risk | Olasılık | Etki | Önlem |
|---|---|---|---|
| Geliştirici motivasyonunu kaybeder | Orta | Yüksek | Küçük ve tamamlanabilir MVP; topluluk katkısı; bağış |
| Ürün "desteklenmiyor" algısı | Orta | Orta | README'de net durum; topluluk kanalları |
| Beklenti yönetimi zorluğu | Yüksek | Orta | "Ne değildir" bölümü; SSS; net yol haritası |
| Kötü niyetli çatal (fork) | Düşük | Düşük | Lisans açık; sorun değil |
| Bağış geliri yetersiz | Yüksek | Düşük | Zaten gelir hedefi yok |
| Kullanıcı "ücretsiz = kalitesiz" sanır | Orta | Orta | Kaliteye yatırım; referans projeler; ekran görüntüleri |
| Özellik talepleri kontrolsüz artar | Yüksek | Orta | Net yol haritası; "hayır" deme disiplini |

---

## 9. Başarı Metrikleri

Ücretsiz bir üründe başarı, gelirle ölçülmez. Ölçütler:

- **Aktif kullanıcı sayısı** (opt-in telemetri veya GitHub indirme)
- **Tamamlanan albüm sayısı** (kullanıcı geri bildirimi)
- **Hata bildirim hızı** (topluluk sağlığı göstergesi)
- **Katkı sayısı** (PR, şablon, çeviri)
- **Kullanıcı memnuniyeti** (anketler, yorumlar)
- **Ortalama albüm oluşturma süresi** (ürün verimliliği)
- **PDF çıktısı başarı oranı** (teknik kalite)

---

## 10. Kararın Diğer Kararlarla İlişkisi

| Karar | İlişki |
|---|---|
| 02 — Açık Kaynak | Ücretsiz olmak, açık kaynağı zorunlu kılar. Birlikte alınır. |
| 03 — Kullanıcı Profilleri | Ücretsiz olduğu için baskı evi entegrasyonu yok; kullanıcı kendi profili girer. |
| 04 — Tek Kişi | Ücretsiz + tek kişi = sürdürülebilirlik için MVP disiplini şart. |
| 05 — Zaman | Ücretsiz olduğu için ticari baskı yok; kaliteye odaklanılabilir. |
| 06 — Bütçe | Ücretsiz olduğu için bütçe = geliştiricinin zamanı. Ticari kütüphaneler tercih edilmez. |
| 07 — Rakip Özellikleri | Ücretsiz olduğu için "en iyi özellikleri bir araya getirme" stratejisi mantıklı. |

---

## 11. Açık Sorular (Sonraki Kararlara Bırakılan)

- Lisans türü ne olacak? (MIT, GPL, Apache?) → Karar 02
- Bağış altyapısı MVP'de olacak mı, sonra mı? → Karar 04/06
- Telemetri hiç olacak mı, opt-in mi? → Karar 04
- Kod imzalama sertifikası alınacak mı? → Karar 06
- Güncelleme kontrolü nasıl yapılacak? → Karar 04

---

## 12. Özet

NexAlbum **ücretsiz** bir üründür. Bu karar:
- Ürünün kimliğini belirler.
- Mimariyi sadeleştirir (lisans katmanı yok).
- Kullanıcı güvenini kazanır.
- Açık kaynak kararıyla tutarlıdır.
- Tek geliştirici için sürdürülebilir tek modeldir.

Bu karar, diğer tüm kararların temelini oluşturur. Değiştirilmesi
durumunda (örneğin ileride ücretli sürüm eklenmesi) mimari ve ürün
kimliği baştan gözden geçirilmelidir. Ancak mevcut niyet, bu kararın
kalıcı olduğudur.

---

**Onay:** Bu karar, ürünün kurucu ilkelerinden biridir ve README'de
kullanıcıya açıkça duyurulur.