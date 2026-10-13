Karar Dokümanı 17 — Marka ve Görsel Kimlik
Proje: NexAlbum
Karar no: 17
Konu: Logo, yazı markası, renk sistemi (Gece ve Fildişi temaları), tipografi, ikonlar, uygulama simgesi ve açılış ekranı
Durum: Kesinleşti
Tarih: 13/10/2026
İlgili kararlar: 01 (Ücretsiz), 02 (Açık kaynak), 08 (Teknoloji, Revizyon 4), 12 (UI/UX, Revizyon 1)
Kaynak: `docs/marka/nexalbum-marka-konsepti.png` ve `docs/NexAlbum_ Düğün Albümü Tasarım Stüdyosu.png` (Hasan Güler, 13/10/2026)

1. Karar Özeti
NexAlbum'ün görsel kimliği **koyu antrasit zemin + sıcak altın vurgu** üzerine kurulur. Varsayılan tema **Gece**'dir
(koyu); açık zemin isteyenler için **Fildişi** teması vardır. Bu karar Karar 12 §9'un yerine geçer (Karar 12 Revizyon 1).

| Öğe | Karar |
|---|---|
| Logo | Daire içinde altın, kurdele biçimli "N"; siyah disk, ince altın halka |
| Yazı markası | "Nex" kırık beyaz/koyu + "Album" altın; Inter SemiBold |
| Slogan | "Fotoğraflarınızı Hikayeye Dönüştürün" |
| Varsayılan tema | Gece (koyu-altın); seçenek: Fildişi (açık-altın); Sistem: ileride |
| Yazı tipi | Inter (SIL OFL 1.1), uygulamayla birlikte gömülü |
| İkonlar | Lucide (ISC), ince çizgi; WPF geometrisi olarak gömülü |
| Uygulama simgesi | `NexAlbum.ico`: 16, 20, 24, 32 (sadeleştirilmiş çizim), 40, 48, 64, 128, 256 px |
| Açılış ekranı | Logo + yazı markası + slogan; altın dalga çizgileri |

2. Neden koyu ve altın? (gerekçe)
- **Fotoğraf kahraman, arayüz sessiz (Karar 12 §2).** Nötr koyu zemin fotoğrafın renklerini ve kontrastını doğru algılatır;
  profesyonel fotoğraf araçlarının (ör. Lightroom, Capture One) koyu zemini bu yüzden seçmesi tesadüf değildir. Açık zemin göz bebeğini
  daraltır, sayfa beyazları "soluk" görünür.
- **Ekranı müşteri de görür.** Türkiye'de albüm seçimi çoğu zaman stüdyoda, çiftle birlikte aynı ekrana bakılarak yapılır. Arayüz bu
  anda fotoğrafçının vitrinidir: aracın görünüşü işin özenini anlatır. "Premium" görünüm bir süs değil, fotoğrafçının müşteri karşısındaki
  güvenidir.
- **Altın, düğünün kültürel rengidir.** Takı merasimi, davetiye yaldızı, deri albüm kapağındaki sıcak varak: altın kutlama ve
  kalıcılık çağrışımı yapar. Albüm, ailenin kuşaktan kuşağa geçecek nesnesidir; marka bu "değerli nesne" duygusunu taşır.
- **Tek vurgu rengi.** Altın yalnızca eylem ve seçimde kullanılır; geri kalan her şey nötrdür. Arayüz kalabalıklaşmaz.

3. Logo
3.1 Çizim
- Kaynak dosyalar: `docs/marka/nexalbum-logo.svg` (≥ 40 px) ve `docs/marka/nexalbum-logo-kucuk.svg` (≤ 32 px).
- "N" üç şeritten oluşur: iki dikey gövde (koyu altın, alttan aydınlanan) ve öne geçen çapraz şerit (açık altın, ince ışık çizgisi).
  Sol üst ve sağ alt köşelerdeki koyu üçgenler kurdelenin katlandığını gösterir.
- Küçük sürüm: katlanma üçgenleri ve ışık çizgisi kaldırılır, "N" %18 büyütülür, halka kalınlaşır. 16 px'te de "N" okunur.
- Uygulamada logo vektördür (`Themes/Brand.xaml`, `DrawingImage`): her DPI'da keskin.

3.2 Kullanım kuralları
- Logo döndürülmez, eğilmez, renkleri değiştirilmez; etrafında en az halka kalınlığının 4 katı boşluk bırakılır.
- Tek renk gerekiyorsa (ör. baskı, gravür) logo düz altın ya da düz beyaz basılabilir; gradyan şart değildir.
- Konsept görselindeki fotoğraflı açılış görselleri (gelin-damat vb.) dağıtılmaz; lisansı net olmayan görsel kullanılmaz.

4. Renk sistemi
Renkler kodda yalnızca anlamsal anahtarlarla kullanılır (`Themes/Gece.xaml`, `Themes/Fildisi.xaml`); XAML ve C# içinde
sabit renk yazılmaz (sayfa içeriği — fon, çerçeve, kesim/güvenli alan kılavuzları — hariç; onlar baskı verisidir).

| Anahtar | Gece | Fildişi | Kullanım |
|---|---|---|---|
| Background | #111113 | #F4F0E8 | Pencere zemini |
| Surface | #18181B | #FAF7F1 | Paneller |
| SurfaceRaised | #222226 | #FFFFFF | Kart, giriş kutusu, açılır menü |
| SurfaceHover | #2B2B30 | #EFE8DB | Üzerine gelme |
| Border | #2C2C31 | #E3DACA | Ayırıcı çizgi |
| BorderStrong | #3E3E45 | #CFC3AD | Giriş kutusu kenarı |
| Text | #ECE8E1 | #1F1B16 | Metin |
| SecondaryText | #A29C92 | #6B6358 | Açıklama |
| MutedText | #7C776F | #837A6D | Pasif öğe, ipucu (yalnızca ≥ 3:1 yeter) |
| Accent | #D4A853 | #C99B48 | Altın dolgu (birincil düğme, seçim) |
| AccentHover | #E2BC6C | #D6AC5C | Üzerine gelme |
| AccentPressed | #B88D3E | #AE843A | Basılı |
| OnAccent | #1A1408 | #1A1408 | Altın üzerindeki metin |
| AccentText | #E0B864 | #8A6522 | Altın metin/ikon (zemin üzerinde) |
| Selection | #2F2819 | #F1E5CB | Seçili öğe zemini |
| Canvas | #0D0D0E | #E7E0D3 | Tuval zemini (sayfanın çevresi) |
| Success | #6CBF7A | #2E7D4F | Onay |
| Warning | #E8A64A | #9A5806 | Dikkat |
| Error | #E5675F | #B83A2E | Hata |
| Info | #7FB2E5 | #2F6FA8 | Bilgi |

Kurallar:
- Birincil düğme: altın dolgu + koyu metin (her iki temada). Altın üzerine beyaz metin yazılmaz (kontrast yetersiz).
- Metin kontrastı WCAG AA: gövde metni ≥ 4,5:1, büyük başlık, ikon ve pasif/ipucu metni (MutedText) ≥ 3:1. Tablo değerleri
  Surface zemini üzerinde bu ölçüte göre hesaplanmıştır (ör. Gece: Text 14,5:1, SecondaryText 6,5:1; Fildişi: Text 16:1, AccentText 5:1).
- Durum renkleri (başarı/uyarı/hata) her zaman bir ikon ya da sembolle birlikte kullanılır; anlam yalnızca renkle verilmez (Karar 12 §15).

5. Tipografi
- **Inter** (SIL Open Font License 1.1) uygulamaya gömülür: Light, Regular, Medium, SemiBold, Bold. Kurulu olması gerekmez;
  her bilgisayarda aynı görünür. Yedek: Segoe UI.
- Boyutlar: gövde 13 px · açıklama 12 px · küçük 11 px · bölüm başlığı 11 px büyük harf, 0,6 px harf aralığı · pencere başlığı
  18 px · karşılama başlığı 30 px.
- Ağırlıklar: 400 gövde, 500 düğme ve etiket, 600 başlık ve yazı markası.
- Lisans metni: `src/NexAlbum.UI/Assets/Fonts/OFL.txt`; ayrıca `THIRD-PARTY-NOTICES.md`.

6. İkonlar
- **Lucide** (ISC lisansı) ince çizgi ikonları; 24 birimlik kutu, 1,6 birim çizgi, yuvarlak uç. Karar 12 §9.6'daki
  "Feather/Tabler" yerine geçer (Lucide, Feather'ın devamıdır).
- SVG'ler `tools/ikonlar/lucide_to_xaml.py` ile `Themes/Icons.xaml` içine WPF geometrisi olarak üretilir; çalışma anında dosya
  ya da yazı tipi gerekmez, Windows 10'da da aynı görünür (Segoe Fluent Icons yalnızca Windows 11'dedir).
- Renk: metin rengini izler; etkin/seçili durumda AccentText.

7. Biçim dili
- Köşe yarıçapı: 6 px (düğme, giriş), 10 px (kart, panel içi blok), 14 px (açılış ekranı, komut paleti).
- Boşluk: 4 px ızgara; panel iç kenarı 16 px.
- Gölge: yalnızca yüzen öğelerde (komut paleti, açılır menü, tuvaldeki sayfa). Düz yüzeylerde gölge yok.
- Hareket: Karar 12 §17'ye uyar; komut paleti 120 ms opaklıkla açılır. Fotoğraf, yakınlaştırma ve sayfa değişiminde animasyon yok.

8. Pencere çerçevesi
- Ana pencerede Windows başlık çubuğu yerine marka başlığı (logo + yazı markası + araç çubuğu + pencere düğmeleri) kullanılır
  (WPF `WindowChrome`; ek bağımlılık yok). Sürükleme, çift tıkla büyütme, kenardan boyutlandırma ve Win+ok kısayolları korunur.
- Diyalog pencereleri Windows çerçevesini korur; Gece temasında başlık çubuğu koyu çizilir (DWM, Windows 10 20H1+).

9. Açılış ekranı
- `Assets/splash.png` (640×400, saydam köşeler), WPF `SplashScreen` ile ana pencere hazırlanırken gösterilir; kapanırken solar.
- İçerik yalnızca logo, yazı markası, slogan ve "Ücretsiz ve açık kaynak · GPL-3.0". Sürüm numarası pencerede gösterilir, görselde değil.
- Kaynak: `tools/marka/` (HTML + Inter); yeniden üretim adımları oradaki README'de.

10. Sunum ve kitap görünümü (Revizyon 1 — 13/10/2026)
- **Sunum modu (F5):** arayüz tamamen çekilir; koyu sahnede yalnızca sayfalar (karşılıklı çift sayfa, ortada cilt gölgesi).
  Taşma payı kırpılır, kesim/güvenli alan kılavuzları gizlenir: çift albümü baskıdaki gibi görür. Sayfa geçişinde 260 ms solma
  (yalnızca sunumda; düzenleyicide sayfa değişimi animasyonsuzdur, Karar 12 §17.3). Hareketsizlikte alt bilgi ve imleç gizlenir.
  Gerekçe: §2 — albüm seçimi stüdyoda, çiftle aynı ekrana bakılarak yapılır.
- **Kitap görünümü (Ctrl+B):** düzenleyicide karşılıklı iki sayfa (1–2, 3–4…); albüm açık kitap olarak değerlendirilir.
- **Boş yuva:** sıcak kâğıt tonu, altın "+" ve "Fotoğraf bırakın" ipucu (yalnızca ekranda; PDF'e çizilmez).

11. Albüm yazıları (Revizyon 2 — 13/10/2026)
- Sayfaya başlık, isimler, tarih, alıntı ve serbest metin eklenir (Metin sekmesi, Ctrl+T). Metin yuvaların üstündeki katmandadır;
  sürüklenerek taşınır, çift tıkla düzenlenir, Del ile silinir; tüm değişiklikler geri alınabilir.
- Gömülü albüm yazı tipleri (hepsi SIL OFL 1.1, Google Fonts): el yazısı **Great Vibes, Pinyon Script, Parisienne, Allura**;
  serif **Cormorant Garamond, Playfair Display**; sans **Montserrat** (ve arayüzün Inter'i). Kurulum gerekmez: ekran WPF kaynak
  yazı tipini, PDF aynı dosyayı gömer — stüdyo ile baskı evinde sonuç aynıdır. Sistemde kurulu başka yazı tipleri de seçilebilir
  (ön kontrol eksik yazı tipini bildirir).
- Varsayılan renkler: sıcak mürekkep #2B2620 (saf siyah yerine) ve altın #A9843F. Baskıdaki CMYK karşılığı profile göre değişir;
  değer uydurulmaz (Karar 03).
- Satır kaydırma ekran ve PDF'te aynı kuralla yapılır (`TextLines`: sözcük sınırında, sabit satır yüksekliği = punto × aralık).
- Lisans metni ve telif satırları: `src/NexAlbum.UI/Assets/AlbumFonts/OFL.txt`; ayrıca `THIRD-PARTY-NOTICES.md`.

12. Kapsam dışı (ileride)
- "Sistem" teması (Windows açık/koyu ayarını izleme) — Karar 12 §9.1'de zaten "ileride".
- Yüksek kontrast teması (Karar 12 §15) — Windows yüksek kontrast açıksa WPF sistem renklerine düşülmesi ayrıca ele alınacak.
- Web sitesi, sosyal medya ve baskı materyalleri.

13. Kararın diğer kararlarla ilişkisi
- 12 UI/UX: §9 (Tema ve Görsel Kimlik) bu kararla değişir — Karar 12 Revizyon 1.
- 08 Teknoloji: yeni NuGet bağımlılığı yok; gömülü varlıklar (Inter, Lucide) lisans matrisine eklendi — Karar 08 Revizyon 3;
  albüm yazı tipleri — Karar 08 Revizyon 4.
- 01/02: Tüm varlıklar ücretsiz ve GPL-3.0-or-later ile uyumludur (OFL yazı tipleri yazılımla birlikte dağıtılabilir; ISC izin verici).
