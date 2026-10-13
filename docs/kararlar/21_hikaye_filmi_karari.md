Karar Dokümanı 21 — Hikâye Filmi ve Sosyal Medya Klipleri
Proje: NexAlbum
Karar no: 21
Konu: Albümün hikâye bölümlerinden geçiş efektli, müzikli video üretimi (sosyal medya klipleri ve 2K/4K film)
Durum: Kesinleşti
Tarih: 13/10/2026
İlgili kararlar: 00, 04, 05, 07 (Video kapsam dışıydı — bu kararla "Geçersiz" işaretlendi), 08 (Teknoloji, Revizyon 5), 10 (Hikâye akışı, Rev. 2), 17 (Yazı etkileri), 20 (Harici araç yöntemi)
İstek: Hasan Güler — "hikâyeleri pro geçiş efektleriyle video hâline getirme; sosyal medya taslak ve şablonları; 2K–4K çıktı;
otomatik ya da kullanıcının seçtiği efektler; yerel müzik kütüphanesi". Efekt dili: "NexAlbum — Fotoğraf Efektleri ve Hikâye" belgesi.

1. Karar Özeti
Düğün fotoğrafçısı albümü teslim ederken çifte bir de film ve paylaşılabilir kısa klip verir. NexAlbum, Karar 10 Rev. 2'deki
hikâye bölümlerini iskelet alarak videoyu kendisi kurar: bölüm başlığı kartı, fotoğraflarda yavaş hareket (Ken Burns), bölüm
içinde ve arasında geçişler, müziğin vuruşuna oturan kesmeler, sonda müziğin yumuşakça kısılması. Tümü yerel ve deterministiktir.

2. Çıktılar
| Biçim | Ölçü | Kullanım |
|---|---|---|
| Dikey 9:16 | 1080×1920 | Reels, TikTok, Shorts, hikâye |
| Kare 1:1 | 1080×1080 | Gönderi |
| Dikey 4:5 | 1080×1350 | Akış gönderisi |
| Full HD 16:9 | 1920×1080 | Paylaşım, TV |
| 2K 16:9 | 2560×1440 | Film |
| 4K 16:9 | 3840×2160 | Film, arşiv |
Kare hızı 24 / 30 / 60. MP4 (H.264, yuv420p, AAC 192 kb/sn). Sosyal biçimlerde hedef süre (15 / 30 / 60 sn) verilir; fotoğraflar
bölümlere orantılı paylaştırılır, önce favoriler seçilir. Film biçimlerinde tüm hikâye anlatılır (hedef süre isteğe bağlı).

3. Stiller (otomatik kip)
| Stil | Geçiş | Hareket | Görünüm |
|---|---|---|---|
| Zarif | Çapraz geçiş, odak kaydırma | Yavaş yaklaşma/kayma | Doğal |
| Sinematik | Siyaha geçiş, çapraz | Yavaş, geniş | Hafif sıcak, vinyet, sinema şeritleri yok (kırpmasız) |
| Dinamik | İtme, yakınlaşarak geçiş, iris | Hızlı, vuruşa oturur | Doğal |
| Nostalji | Beyaza geçiş, çapraz | Yavaş | Sepya, gren, vinyet |
Elle kip: kullanıcı her bölüm için geçişi ve görünümü seçer (otomatiğin yerine geçer). Geçişler: kesme, çapraz geçiş, siyaha
geçiş, beyaza geçiş, odak kaydırma, dairesel iris, itme (sol/sağ/yukarı), yakınlaşarak geçiş. Görünümler: doğal, sıcak, soğuk,
siyah-beyaz, sepya; vinyet ve gren isteğe bağlı. Bu görünümler yalnızca videoda uygulanır; fotoğrafın albümdeki/baskıdaki görünümünü
değiştiren düzenlemeler ayrı bir modeldir (efekt belgesi §1, ayrı karar).

4. Müzik
- Müzik kullanıcının seçtiği yerel klasörlerden gelir (mp3, m4a, aac, wav, flac, ogg). NexAlbum müzik dağıtmaz; telif
  kullanıcıdadır. Sosyal biçimlerde arayüz bunu hatırlatır.
- Parça FFmpeg ile tek kanallı PCM'e çözülür; enerji zarfından vuruşlar ve tempo (60–180 BPM) bulunur. Kesmeler en yakın vuruşa
  oturtulur (fotoğraf süresi ±%35 sınırında); vuruş bulunamazsa sabit ritim kullanılır.
- Video müziğin sessiz girişini atlar; sonda 2 sn kısılma. Müzik videodan kısaysa sessizlikle biter (döngü yok).

5. FFmpeg (Karar 20 yöntemi, Karar 08 Rev. 5)
- Kareler SkiaSharp ile çizilir, FFmpeg'e ham olarak verilir; FFmpeg kodlar ve müziği ekler.
- FFmpeg uygulamayla **gömülmez** (kurulum büyümesin). Sırayla aranır: Ayarlar'daki yol → NexAlbum araç klasörü
  (%LOCALAPPDATA%\NexAlbum\tools\ffmpeg) → PATH.
- Bulunamazsa kullanıcıya sorulur: "Hikâyeleri videoya dönüştürmek için FFmpeg gerekiyor (GPL, yaklaşık 100 MB). İndirip
  kurulmasına izin veriyor musunuz?" Evet → resmi derleme (BtbN/FFmpeg-Builds, win64 GPL) indirilir, ilerleme gösterilir, araç
  klasörüne açılır, çalıştığı doğrulanır. Hayır → video dışındaki her şey çalışmaya devam eder. İnternet yalnızca bu, kullanıcı
  onaylı indirmede kullanılır.
- İleride kurulabilir paket (Karar 16) FFmpeg'i isteğe bağlı bileşen olarak içerebilir.

6. Değişmez ilkeler
- Video baskıyı etkilemez: hareket, geçiş ve görünüm albüm sayfalarına ve PDF'e aktarılmaz.
- Aynı proje + aynı ayarlar + aynı müzik → aynı video (tohumlu, deterministik).
- Uzun iş iptal edilebilir, ilerleme gösterilir; yarım dosya iptal ya da hatada silinir.
- Hareket azaltma: "Sabit" seçeneği Ken Burns'ü ve hareketli geçişleri kapatır (yalnızca çapraz geçiş).
- Ayarlar (biçim, stil, müzik, hedef süre, elle seçimler) projede saklanır (Karar 09 Rev. 4, şema v5).

7. Katmanlar
- Core: video modeli, planlayıcı (zaman çizelgesi), vuruş bulucu — saf, testli.
- Rendering: SkiaSharp kare çizici (fotoğraf, hareket, geçiş, görünüm, başlık; yazı etkileri `TextLayout` ile ortak).
- Video (yeni): FFmpeg bulma/izinli kurulum/kodlama, müzik kütüphanesi ve çözümleme. Core ve Rendering'e bağlıdır.
- UI: "Hikâye Filmi" penceresi (önizleme, ayarlar, dışa aktarma).
