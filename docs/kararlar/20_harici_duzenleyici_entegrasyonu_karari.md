Karar Dokümanı 20 — Harici Düzenleyici Entegrasyonu (Photoshop ve açık kaynak araçlar)
Proje: NexAlbum
Karar no: 20
Konu: Rötuş/renk işlerinin kullanıcının kendi araçlarıyla (Photoshop action'ları, darktable, RawTherapee, GIMP vb.) tahribatsız ve toplu yapılması
Durum: Kesinleşti
Tarih: 13/10/2026
İlgili kararlar: 01 (Ücretsiz), 02 (Açık kaynak), 07 (Rakip sentezi), 08 (Teknoloji, Revizyon 2), 09 (Veri modeli), 11 (Çıktı), 12 (UI/UX)

1. Karar Özeti
NexAlbum rötuş motoru yazmaz; fotoğrafçının zaten kullandığı araçları yönetir. Fotoğrafçının action seti onun
"imzası"dır; NexAlbum bu tarzı ezmez, taşır. Entegrasyon isteğe bağlıdır: araç yoksa özellik görünmez, uygulama
eksiksiz çalışmaya devam eder.

| Yetenek | Kapsam |
|---|---|
| Düzenleyicide aç | Seçili fotoğrafın çalışma kopyası harici düzenleyicide açılır; kaydedilince albüm kendiliğinden yenilenir |
| Toplu düzenleme | Photoshop action'ı ya da komut satırı aracı; seçili / sayfadaki / albümde kullanılan / tüm / henüz düzenlenmemiş fotoğraflar |
| Önce dene | Toplu işten önce tek fotoğrafta sonuç; önce/sonra karşılaştırması |
| Önce/sonra | Her fotoğrafta orijinal ↔ düzenlenmiş tek tıkla; düzenleme kaldırılabilir |
| Tarif | Kullanılan araç ve tarif (ör. "Stüdyo / Sıcak Ton") projede saklanır; pencere aynı tarifle açılır, "henüz düzenlenmemişler" kapsamıyla sonradan eklenen fotoğraflara da uygulanır |

2. Değişmez ilkeler
- **Orijinale dokunulmaz.** Düzenleyiciye her zaman bir kopya verilir; çıktı ayrı dosyadır (Karar 09 §13.1 referans modu).
- **Kırpma NexAlbum'da kalır.** Kırpma, kaydırma ve yakınlaştırma tahribatsızdır ve şablon değişince yeniden hesaplanır;
  harici araç yalnızca rötuş ve renk yapar. Düzenlenmiş dosyanın oranı değişirse yerleşim yeni orana göre çözülür.
- **Değer uydurma yok.** Çıktının boyutu ve yönü dosyadan okunur; okunamayan çıktı başarısız sayılır, kabul edilmez.
- **Renk profili korunur.** Photoshop çıktısı ICC profili gömülü kaydedilir. Varsayılan biçim JPEG kalite 12 (PDF'e yeniden sıkıştırmadan gömülür); kayıpsız isteyene PNG. TIFF seçilmez: PDF motoru (PDFsharp) TIFF gömemez.
- **Tek iş, sıralı kuyruk.** Photoshop tek örnek çalışır; dosyalar sırayla işlenir, iş iptal edilebilir, her dosyanın süresi sınırlıdır.
  Bir dosyanın hatası toplu işi durdurmaz; hatalar listelenir.
- **Tıklama otomasyonu yok.** Ekran tıklatarak (UI otomasyonu) çalışılmaz; yalnızca araçların resmi otomasyon kapıları kullanılır.

3. Araçlar ve yöntem
| Araç | Yöntem | Not |
|---|---|---|
| Adobe Photoshop (Windows) | COM (`Photoshop.Application`) + `DoJavaScript` ile ExtendScript | Action listesi okunur, `doAction`, diyaloglar kapalı (`DialogModes.NO`) |
| darktable | `darktable-cli {girdi} [stil.xmp] {çıktı}` | Komut satırı ön ayarı |
| RawTherapee | `rawtherapee-cli -o {çıktı} -p {profil.pp3} -c {girdi}` | Komut satırı ön ayarı |
| GIMP, Affinity, diğerleri | Yalnızca "Düzenleyicide aç" | Etkileşimli |
| Herhangi bir CLI | Kullanıcı tanımlı komut şablonu: `{input}`, `{output}` | Genel kapı |

Komut şablonları kabuk üzerinden değil, doğrudan süreç olarak ve bağımsız değişken listesiyle çalıştırılır (komut enjeksiyonu yok).
ExtendScript'e geçen dosya adları ve action adları JSON kaçışıyla gömülür.

4. Veri modeli (Karar 09'a ek, şema sürümü değişmez — §14.2 "yeni opsiyonel alan")
`PhotoAsset.edit` (opsiyonel): `path`, `pixelWidth`, `pixelHeight`, `exifOrientation?`, `tool`, `recipe?`, `createdAt`.
`PhotoAsset.useEdit` (varsayılan true). `missing` açılışta denetlenir.
Etkin kaynak kuralı: düzenleme varsa, kullanılıyorsa ve dosyası yerindeyse ekran, ön kontrol ve PDF düzenlenmiş dosyayı kullanır;
aksi halde orijinali. Düzenlenmiş dosya kayıpsa ön kontrol uyarı verir ve orijinal kullanılır.
`settings.editing.lastRecipe` (opsiyonel): son toplu tarif.

Düzenlenmiş dosyaların yeri: `Belgeler/NexAlbum/Düzenlenmiş/<proje adı>-<kimlik8>/` (uygulama ayarlarından değiştirilebilir).
Dosya adı deterministiktir: `<asset-id>-<tarif kısaltması>.<uzantı>`.

5. Katman
Yeni katman `NexAlbum.Integrations` → Core, Assets (Karar 08 Revizyon 2). Dış süreç, COM ve dosya izleme burada;
Core saf kalır. UI → Integrations.

6. Lisans
Photoshop'a dışarıdan, kullanıcının kendi kurulumu üzerinden erişilir; Adobe kodu veya SDK'sı dağıtılmaz, bağlanmaz.
GPL-3.0-or-later ile çelişki yoktur (Karar 02). Yeni NuGet bağımlılığı yoktur.

7. Rakiplerden farkı (Karar 07'ye ek)
Rakip albüm programları ya rötuşu hiç yönetmez ya da kendi sınırlı filtrelerini dayatır. NexAlbum:
fotoğrafçının kendi action'larını albüm bağlamında (yalnızca albümde kullanılan fotoğraflara) toplu uygular, ücretsiz
araçlarla da aynı akışı sunar, orijinali korur ve önce/sonra kanıtını müşteri görüşmesine taşır.

8. Riskler
| Risk | Önlem |
|---|---|
| Action içinde onay penceresi işi kilitler | `DialogModes.NO`, dosya başına zaman aşımı, atla ve devam et |
| Photoshop sürüm farkları | Erişilebilirlik denetimi; hata metni kullanıcıya aynen gösterilir |
| Büyük dosyalar, disk | Yalnızca seçilen kapsam işlenir; çıktı klasörü ayarlardan seçilir |
| Düzenlenmiş dosyanın silinmesi | Açılışta denetim, ön kontrol uyarısı, orijinale otomatik dönüş |
