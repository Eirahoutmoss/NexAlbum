# NexAlbum --- Fotoğraf Düzenleme ve Hikâye Efektleri

## Amaç ve kapsam

Bu belge, NexAlbum içinde fotoğrafları düzenlerken ve bir albüm/hikâye
anlatısı oluştururken kullanılacak **yerleşik efekt araçlarını**
tanımlar.

Bu efektler web sitesi, dış servis veya uzaktaki görsel kaynağı olarak
tasarlanmamıştır. Kullanıcı efektleri NexAlbum arayüzünden seçer,
ayarlarını değiştirir, sonucu önizler ve proje içinde saklar. İşlem
yerel olarak yapılır. Efektlerin çalışması internet bağlantısı
gerektirmez.

## Temel ürün ilkeleri

1.  **Yerel işlem:** Fotoğraf, ayar ve önizlemeler harici servise
    gönderilmez.
2.  **Tahribatsız düzenleme:** Orijinal fotoğraf korunur. Efektler ve
    ayarlar proje içinde düzenlenebilir parametreler olarak saklanır.
3.  **Canlı önizleme:** Kullanıcı ayarları değiştirirken sonuç mümkün
    olduğunca anlık gösterilir.
4.  **Geri alınabilirlik:** Her efekt açılıp kapatılabilir,
    sıfırlanabilir ve geri alınabilir.
5.  **Katmanlı efekt zinciri:** Birden fazla efekt belirlenmiş sırayla
    uygulanabilir; kullanıcı sıralamayı değiştirebilir.
6.  **Baskı ayrımı:** Ekran geçiş animasyonları baskıya aktarılmaz.
    Fotoğrafa uygulanan gerçek görsel düzenlemeler ise kullanıcı
    seçimine göre dışa aktarıma dahil edilir.
7.  **Performans:** Büyük fotoğraflarda düşük çözünürlüklü önizleme
    kullanılabilir; nihai çıktı yüksek çözünürlüklü kaynak üzerinden
    oluşturulur.
8.  **Erişilebilirlik:** Klavye ile kullanım, görünür odak, anlaşılır
    kontroller ve hareket azaltma desteği sağlanır.

------------------------------------------------------------------------

# 1. Fotoğrafa doğrudan uygulanan efektler

Bunlar yalnızca sunum animasyonu değildir; fotoğrafın görünümünü
değiştirir ve kullanıcı isterse nihai albüm çıktısına dahil edilir.

## 1.1 Parlaklık ve kontrast

**Öncelik: P0**

-   Parlaklık, kontrast ve pozlama benzeri temel ayarlar.
-   Sıfırlama ve varsayılan değerlere dönme.
-   Orijinal fotoğrafı değiştirmeden parametre olarak saklama.

## 1.2 Renk sıcaklığı ve ton

**Öncelik: P0**

-   Sıcak/soğuk renk dengesi.
-   Renk tonu ve doygunluk ayarları.
-   Öncesi/sonrası karşılaştırması.

## 1.3 Siyah-beyaz

**Öncelik: P0**

-   Siyah-beyaz dönüşümü.
-   Kontrast ve ton yoğunluğu ayarı.
-   Tek tıkla varsayılan siyah-beyaz görünüm.

## 1.4 Sepya ve nostaljik ton

**Öncelik: P1**

-   Sepya ve sıcak nostaljik renk paleti.
-   Efekt yoğunluğunu ayarlama.
-   Hikâyenin zaman/hatıra hissini destekleme.

## 1.5 Vinyet

**Öncelik: P1**

-   Fotoğrafın kenarlarını kontrollü biçimde karartma veya açma.
-   Merkez ve yoğunluk ayarı.
-   Önizleme ve dışa aktarımda tutarlılık.

## 1.6 Film greni

**Öncelik: P2**

-   İnce veya belirgin film dokusu.
-   Yoğunluk ve tane boyutu.
-   Yalnızca kullanıcı açtığında uygulanır; varsayılan olarak kapalıdır.

## 1.7 Yumuşak odak ve bulanıklık

**Öncelik: P1**

-   Kontrollü yumuşatma veya bulanıklık.
-   Yoğunluk ayarı.
-   Mümkünse seçili alanı koruyarak arka planı yumuşatma için ileride
    maske desteği.

## 1.8 Keskinlik

**Öncelik: P1**

-   Hafif keskinleştirme.
-   Aşırı keskinleştirmeyi önlemek için sınırlandırılmış ayar aralığı.
-   Önizleme ve çıktı tutarlılığı.

## 1.9 Işık sızıntısı ve ışık halesi

**Öncelik: P2**

-   Sıcak ışık halesi, yumuşak parıltı ve kontrollü ışık sızıntısı.
-   Konum, yoğunluk ve renk ayarı.
-   Fotoğrafın önemli detaylarını örtmemeli.

## 1.10 Renkli vurgu

**Öncelik: P2**

-   Seçilen bir rengin korunup diğer renklerin azaltılması.
-   İlk sürümde tüm nesneleri otomatik ve kusursuz tanıdığı iddia
    edilmemeli.
-   Basit sürüm renk aralığına, gelişmiş sürüm maske/seçim aracına
    dayanabilir.

## 1.11 Renk geçişi / Color grading

**Öncelik: P2**

-   Gölgeler, orta tonlar ve parlak alanlar için ayrı renk ayarları.
-   Hikâyenin genel renk atmosferini tutarlı hâle getirme.
-   Kullanıcı tarafından kaydedilebilir ön ayarlar.

------------------------------------------------------------------------

# 2. Hikâye anlatımı için fotoğraf efektleri

## 2.1 Ken Burns hareketi

**Öncelik: P1**

-   Fotoğraf üzerinde yavaş yakınlaşma veya kayma.
-   Başlangıç ve bitiş ölçeği/konumu.
-   Yalnızca dijital hikâye veya sunum oynatımında çalışır; statik baskı
    dosyasına animasyon olarak aktarılmaz.

## 2.2 Derinlik / Parallax

**Öncelik: P2**

-   Ön plan ve arka planı farklı hızlarda hareket ettirme.
-   İlk sürümde manuel katman veya maske gerektirebilir.
-   Otomatik derinlik ayrımının her fotoğrafta doğru olduğu
    varsayılmamalı.

## 2.3 Odak kaydırma

**Öncelik: P1**

-   Bir fotoğraftan diğerine geçerken odak hissi oluşturma.
-   Galeri seçimi veya hikâye oynatımı sırasında kullanılabilir.
-   Fotoğrafın kendisini bulanıklaştıran düzenleme ile yalnızca geçiş
    animasyonu birbirinden ayrılmalı.

## 2.4 Dairesel iris geçişi

**Öncelik: P2**

-   Yeni fotoğraf, seçilen veya varsayılan bir noktadan dairesel biçimde
    açılır.
-   Geçiş süresi ve başlangıç noktası ayarlanabilir.
-   Yalnızca dijital sunumda kullanılır.

## 2.5 Çapraz geçiş

**Öncelik: P0**

-   Bir fotoğraf yavaşça kaybolurken sonraki fotoğraf görünür.
-   Hikâye akışında güvenilir varsayılan geçiş.
-   Süre ayarı ve hareket azaltma desteği.

## 2.6 Sayfa çevirme

**Öncelik: P1**

-   Dijital albüm önizlemesinde sayfa çevirme hissi.
-   Kullanıcı isterse kapatabilir.
-   Animasyon baskı geometrisini değiştirmez.

## 2.7 Hikâye ritmi ve duraklama

**Öncelik: P1**

-   Fotoğraf başına gösterim süresi.
-   Önemli fotoğraflarda daha uzun duraklama.
-   Otomatik ritim önerisi verilebilir; son kontrol kullanıcıda kalır.

## 2.8 Fotoğraflar arası görsel süreklilik

**Öncelik: P2**

-   Bir hikâyedeki fotoğrafların renk tonlarını birbirine yaklaştırmaya
    yardımcı olur.
-   Otomatik eşleştirme yalnızca öneri olarak sunulur.
-   Kullanıcı onayı olmadan bütün fotoğrafların renkleri topluca
    değiştirilmez.

------------------------------------------------------------------------

# 3. Yerleşim ve kompozisyon yardımcıları

## 3.1 Akıllı hizalama çizgileri

**Öncelik: P0**

-   Merkez, kenar ve eşit boşluk hizalarını göster.
-   Fotoğraf veya metin sürüklenirken etkinleş.
-   Kılavuzlar yalnızca düzenleme arayüzünde görünür; kullanıcı ayrıca
    seçmedikçe baskıya dahil edilmez.

## 3.2 Otomatik kırpma önerisi

**Öncelik: P0**

-   En-boy oranını koruyarak kırpma alanı öner.
-   Kullanıcı kırpma alanını elle değiştirebilsin.
-   Yüz veya önemli nesne algılama varsa bunun bir öneri olduğu açıkça
    belirtilsin; garanti verilmesin.

## 3.3 Kenarlık ve çerçeve

**Öncelik: P1**

-   Renk, kalınlık, köşe biçimi ve iç boşluk ayarı.
-   Fotoğrafa veya yerleşim yuvasına uygulanabilir.
-   Baskı çıktısına dahil edilip edilmeyeceği açıkça belirlenir.

## 3.4 Gölge ve derinlik

**Öncelik: P1**

-   Fotoğraf kartı veya çerçevesine yumuşak gölge.
-   Ekran dekoru ile baskıya girecek gölge ayrı seçenekler olmalı.

## 3.5 Kolaj ve fotoğraf maskeleri

**Öncelik: P1**

-   Daire, oval, yumuşak köşe ve yaratıcı çerçeveler.
-   Fotoğrafı esnetmeden maske içine yerleştirme.
-   Kırpma ve maske parametreleri düzenlenebilir kalmalı.

## 3.6 Metin ve başlık efektleri

**Öncelik: P1**

-   Başlık, tarih, kısa hikâye metni ve açıklama.
-   Renk, boyut, hizalama, harf aralığı ve kontrollü gölge.
-   Metin okunabilirliği ve baskı güvenli alanı kontrol edilmeli.

## 3.7 Fotoğraf sıralama ve ritim

**Öncelik: P1**

-   Hikâye içindeki fotoğrafları sürükleyerek yeniden sırala.
-   Açılış, gelişme, vurgu ve kapanış fotoğrafları için kullanıcı
    tarafından belirlenebilir roller.
-   Otomatik öneriler kullanıcının sıralamasını izinsiz değiştirmemeli.

------------------------------------------------------------------------

# 4. Efekt paneli ve kullanıcı deneyimi

## 4.1 Önerilen panel yapısı

Efektler NexAlbum içinde bağlama duyarlı bir **Efektler** panelinden
seçilmelidir:

-   **Fotoğraf:** Işık, renk, siyah-beyaz, sepya, keskinlik, bulanıklık,
    vinyet, gren.
-   **Kompozisyon:** Kırpma, çerçeve, maske, gölge, hizalama.
-   **Hikâye:** Ken Burns, odak kaydırma, iris, çapraz geçiş, sayfa
    çevirme, süre.
-   **Metin:** Başlık, açıklama, tarih ve metin biçimi.
-   **Ön ayarlar:** Kullanıcının kaydettiği efekt kombinasyonları.

## 4.2 Her efektte bulunması gereken kontroller

-   Aç/kapat.
-   Yoğunluk veya ilgili parametreler.
-   Öncesi/sonrası karşılaştırması.
-   Sıfırla.
-   Geri al/yinele ile uyumluluk.
-   Efekti kopyala ve başka fotoğrafa uygula.
-   Birden fazla fotoğrafa toplu uygulama için açık kullanıcı onayı.
-   Ön ayar olarak kaydet.

## 4.3 Efekt zinciri

Efektler deterministik ve sıralı bir zincir şeklinde uygulanmalıdır.
Örnek:

1.  Kırpma ve döndürme.
2.  Pozlama, parlaklık ve kontrast.
3.  Beyaz dengesi ve renk.
4.  Siyah-beyaz veya renk derecelendirme.
5.  Keskinlik veya bulanıklık.
6.  Vinyet, gren ve diğer yaratıcı efektler.

Bu sıra bir başlangıç önerisidir; gerçek render mimarisi ve renk
yönetimiyle doğrulanmalıdır. Kullanıcı efektleri açıp kapatabilmeli ve
sıralamayı değiştirebilmelidir.

------------------------------------------------------------------------

# 5. Teknik mimari ve çevrimdışı çalışma

-   Fotoğraf dosyaları, efekt parametreleri ve önizlemeler yerel olarak
    işlenir.
-   Harici API, CDN, uzaktaki görsel kaynağı veya internet bağlantısı
    zorunluluğu yoktur.
-   Demo görselleri gerekiyorsa uygulamayla birlikte gelen yerel örnek
    varlıklar kullanılır.
-   Orijinal fotoğraf asla efekt uygulanmış dosyayla otomatik olarak
    değiştirilmez.
-   Proje dosyasında efekt parametreleri ve sürüm bilgileri saklanır;
    yüksek çözünürlüklü sonuç gerektiğinde yeniden oluşturulur.
-   Önizleme için düşük çözünürlüklü kopya/cache kullanılabilir; nihai
    çıktı kaynak fotoğraftan üretilir.
-   Uzun işlemler iptal edilebilir olmalı ve ilerleme göstergesi
    sunulmalıdır.
-   GPU hızlandırma varsa isteğe bağlı optimizasyon olarak ele alınmalı;
    CPU ile çalışan bir temel yol bulunmalıdır.
-   Ekran geçişleri ile fotoğrafın gerçek görünümünü değiştiren efektler
    ayrı API ve veri modellerinde tutulmalıdır.
-   Efekt ayarları geri alma/yineleme geçmişine dahil edilmelidir.
-   Büyük görsellerde bellek kullanımı kontrol edilmeli ve ara tamponlar
    gerektiğinde serbest bırakılmalıdır.

------------------------------------------------------------------------

# 6. Öncelik sırası

## P0 --- Temel düzenleme

1.  Parlaklık ve kontrast.
2.  Renk sıcaklığı ve doygunluk.
3.  Siyah-beyaz.
4.  Çapraz geçiş.
5.  Kırpma ve döndürme.
6.  Akıllı hizalama çizgileri.
7.  Otomatik kırpma önerisi.
8.  Öncesi/sonrası görünümü.
9.  Aç/kapat, sıfırla, geri al/yinele.
10. Yerel çalışma ve tahribatsız düzenleme.

## P1 --- Hikâye ve kompozisyon

1.  Ken Burns.
2.  Sepya.
3.  Vinyet.
4.  Yumuşak odak/bulanıklık.
5.  Keskinlik.
6.  Sayfa çevirme.
7.  Hikâye ritmi ve fotoğraf süresi.
8.  Çerçeve, maske ve gölge.
9.  Metin ve başlık efektleri.
10. Fotoğraf sıralama ve efekt kopyalama.

## P2 --- Yaratıcı görünüm

1.  Film greni.
2.  Işık sızıntısı ve ışık halesi.
3.  Renkli vurgu.
4.  Color grading.
5.  Parallax.
6.  Dairesel iris.
7.  Fotoğraflar arası renk sürekliliği önerileri.

------------------------------------------------------------------------

# 7. Kabul kriterleri

-   Bütün temel efektler internet olmadan çalışır.
-   Efektler orijinal fotoğrafı tahrip etmez.
-   Kullanıcı efektleri ayarlayabilir, kapatabilir, sıfırlayabilir ve
    geri alabilir.
-   Dijital hikâye animasyonları baskı çıktısına yanlışlıkla aktarılmaz.
-   Baskıya dahil edilen gerçek fotoğraf efektleri önizleme ve nihai
    çıktıda tutarlı görünür.
-   Aynı proje ve aynı parametrelerle aynı sonuç elde edilir.
-   Büyük fotoğraflar işlenirken uygulama bellek sınırlarını gözetir.
-   Her efekt için temel test ve çıktı karşılaştırması yapılır.
-   Harici ağ isteği olmadan temel iş akışı çalışır.
-   Test edilmemiş özellikler tamamlandı olarak raporlanmaz.

# 8. Claude Code için uygulama talimatı

Bu belgeyi NexAlbum'in mevcut mimari ve karar belgeleriyle birlikte oku.
Önce mevcut fotoğraf render motorunu, proje dosyası modelini, geri
alma/yineleme altyapısını ve çıktı sistemini incele.

Ardından: 1. Mevcut yapıyla çelişen noktaları ve eksik altyapıyı
raporla. 2. Fotoğraf efektleri ile yalnızca dijital sunumda kullanılan
animasyonları ayrı tut. 3. P0 kapsamını küçük, test edilebilir görevlere
böl. 4. İlk olarak tahribatsız fotoğraf efektleri ve kırpma/hizalama
araçlarını uygula. 5. Efekt parametrelerini proje dosyasına kaydet ve
geri alma/yineleme akışına bağla. 6. Önizleme ile nihai render sonucunu
doğrula. 7. P0 testleri geçmeden P1 ve P2 efektlerine geçme.

Mevcut mimariyi gerekçesiz yeniden yazma. İnternet veya dış servis
bağımlılığı ekleme. Çalıştırılmayan testleri başarılı gösterme. Her
aşamanın sonunda değişen dosyaları ve gerçek test sonuçlarını kısa
biçimde raporla.
