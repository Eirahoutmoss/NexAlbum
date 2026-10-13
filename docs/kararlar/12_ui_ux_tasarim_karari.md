Karar Dokümanı 12 — UI/UX Tasarım Kararı
Proje: NexAlbum
Karar no: 12
Konu: Arayüz düzeni, etkileşimler, kısayollar, tema, erişilebilirlik ve Türkçe deneyim
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 01 (Ücretsiz), 02 (Açık Kaynak), 04 (Tek Kişi), 07 (Rakip Özellikleri), 08 (Teknoloji), 10 (Şablon Motoru), 11 (Çıktı)

Revizyon 1 — 13/10/2026
Değişiklik: §9 (Tema ve Görsel Kimlik) yerine Karar 17 (Marka) geçer. §9.1 varsayılan tema, §9.2–9.3 renk paletleri, §9.6 ikon seti ve §9.7 logo Geçersiz.
- Varsayılan tema **Gece** (koyu antrasit + altın); seçenek **Fildişi** (açık + altın); "Sistem" ileride (değişmedi).
- Vurgu rengi #0066CC yerine altın (Gece #D4A853, Fildişi #C99B48); tüm renkler anlamsal anahtarlarla (Karar 17 §4).
- §9.4 Tipografi: Inter uygulamaya gömülür (kurulum gerekmez); gövde 14 → 13 px, bölüm başlıkları 11 px büyük harf.
- §9.6 İkonlar: Lucide (ISC) ince çizgi, WPF geometrisi; Segoe Fluent/MDL2 yazı tipine bağımlılık kalkar.
- §4 Üst araç çubuğu pencere başlığıyla birleşir (özel pencere çerçevesi, Karar 17 §8); komut paleti (§11) Ctrl+K ile açılır.
Gerekçe: Karar 17 §2 — koyu nötr zemin fotoğraf renklerini doğru algılatır; albüm seçimi çoğu zaman müşteriyle aynı ekrana bakılarak
yapılır; altın düğünün kültürel rengidir. "Fotoğraf kahraman, arayüz sessiz" ilkesi (§2) değişmez, koyu zemin onu güçlendirir.
Değişmeyenler: 5 bölgeli düzen, kısayollar, erişilebilirlik ve kontrast hedefleri, Türkçe öncelik, animasyon ilkeleri.

1. Karar Özeti
NexAlbum'in arayüzü sade, hızlı, Türkçe öncelikli, klavye dostu ve fotoğraf odaklı olacaktır.

Tasarım ilkesi: "Fotoğraf kahraman, arayüz sessiz."

Ana ekran: 5 bölgeli (üst araç, sol galeri, orta tuval, sağ özellikler, alt sayfa şeridi).

Tema: Açık (varsayılan) + Koyu + Sistem. [Revizyon 1: varsayılan Gece (koyu-altın) + Fildişi (açık); bkz. Karar 17]

Dil: Türkçe (varsayılan) + i18n altyapısı (İngilizce ileride).

Kısayollar: Her ana eylem için klavye kısayolu.

Erişilebilirlik: Klavye gezinme, yüksek kontrast, ekran okuyucu temel desteği.

Onboarding: İlk açılışta 3 adımlı tur + örnek proje.

Hata mesajları: Ne oldu + neden + ne yapmalı (3 parçalı).

Performans: Arayüz donmaz; arka plan işleri bildirilir.

Tutarlılık: Tek bir tasarım dili; her ekran aynı kurallara uyar.

Bu karar, 07 (Rakip Özellikleri) sentezinin UX boyutudur ve 08 (Teknoloji) WPF seçimiyle uyumludur.

2. Tasarım Felsefesi
2.1 İlkeler
Fotoğraf önce. Arayüz, fotoğrafın önüne geçmez.

Sadelik. Gereksiz öğe yok; her şey bir işe yarar.

Tutarlılık. Aynı eylem her yerde aynı görünür.

Öngörülebilirlik. Kullanıcı ne olacağını bilir.

Hız. Klavye ile hızlı iş akışı.

Türkçe öncelikli. Metinler doğal Türkçe.

Bağışlayıcılık. Hata yapılırsa geri alınabilir.

Şeffaflık. Uzun işlemler bildirilir.

Erişilebilirlik. Herkes kullanabilir.

Sakinlik. Gereksiz animasyon, ses, dikkat dağıtıcı yok.

2.2 Anti-ilkeler
❌ Gereksiz modal pencere

❌ Aşırı animasyon

❌ Gizli özellik (keşfedilemeyen)

❌ Karmaşık menü hiyerarşisi

❌ İngilizce karışık Türkçe

❌ Renk körlüğüne duyarsız tasarım

❌ Yavaş açılan ekranlar

❌ Bilinmeyen ikonlar (tooltip'siz)

2.3 Tasarım referansları
Figma: Sadelik, tuval odaklılık

Photoshop: Katman paneli mantığı

Lightroom: Galeri + geliştirme modu

VS Code: Klavye kısayolları, komut paleti

Blender: Çalışma alanları (workspace)

Notion: Sade tipografi

3. Ana Ekran Düzeni
3.1 Beş bölge
text
┌──────────────────────────────────────────────────────────────┐
│  ① ÜST ARAÇ ÇUBUĞU                                            │
│  [Yeni] [Aç] [Kaydet] | [Geri] [İleri] | [Oto] [Önizleme] [Çıktı] │
├──────────┬────────────────────────────────┬──────────────────┤
│          │                                │                  │
│  ②       │        ③                       │       ④          │
│  SOL     │        ORTA                    │      SAĞ         │
│  PANEL   │        TUVAL                   │      PANEL       │
│  (Galeri)│                                │   (Özellikler)   │
│          │                                │                  │
│          │                                │                  │
├──────────┴────────────────────────────────┴──────────────────┤
│  ⑤ ALT ŞERİT (Sayfa küçük resimleri)                          │
│  [1] [2] [3] [4] [5] [6] [7] [8] ...                          │
└──────────────────────────────────────────────────────────────┘
3.2 Bölge boyutları
Bölge	Varsayılan	Min	Max	Ayarlanabilir
① Üst	48 px	48	64	❌
② Sol	280 px	200	500	✅
③ Orta	Kalan	400	∞	✅
④ Sağ	320 px	240	500	✅
⑤ Alt	120 px	80	200	✅
3.3 Panel aç/kapa
Her panel başlığından gizlenebilir.

Ctrl+1 Sol panel

Ctrl+2 Sağ panel

Ctrl+3 Alt şerit

Ctrl+0 Tüm panelleri gizle (tam tuval)

Durum çubuğunda "Düzeni sıfırla"

3.4 Tam ekran modu
F11 → tam ekran.

Tab → panelleri geçici gizle/göster.

Ctrl+Shift+F → sunum modu (sadece tuval).

4. Üst Araç Çubuğu
4.1 Düzen
text
[Logo] | [Yeni] [Aç] [Kaydet] | [Geri] [İleri] | [Oto Oluştur] [Önizleme] [Çıktı] | [Ara] | [Ayarlar]
4.2 Gruplar
Grup	Öğeler
Proje	Yeni, Aç, Kaydet, Farklı Kaydet
Geçmiş	Geri Al, Yinele
Ana eylem	Otomatik Oluştur, Önizleme, Dışa Aktar
Arama	Komut paleti (Ctrl+K)
Ayarlar	Tema, Dil, Kısayollar, Hakkında
4.3 İkonlar + metin
İkon: Her eylem için net ikon.

Tooltip: Üzerine gelince Türkçe açıklama + kısayol.

Metin: Kritik eylemlerde ikon + metin (örn. "Oto Oluştur").

Renk: Ana eylem (Oto Oluştur) vurgulu.

4.4 Durum göstergesi
Sağ tarafta:

text
[Kaydedilmedi ●] [Otomatik kayıt: 5 dk] [v0.1.0]
Proje değiştiyse "●" işareti.

Otomatik kayıt sayacı.

Sürüm.

5. Sol Panel — Galeri
5.1 Amaç
Fotoğrafları gözat, seç, sırala, filtrele.

5.2 Sekmeler
Sekme	İçerik
Klasörler	Klasör ağacı (içe aktarma kaynakları)
Galeri	Thumbnail ızgarası
Filtreler	Etiketler, favoriler, kullanılanlar
5.3 Galeri görünümü
Izgara: 3-6 kolon (panel genişliğine göre).

Liste: Küçük thumbnail + metadata.

Büyük önizleme: Tek fotoğraf.

Kullanıcı seçer.

5.4 Fotoğraf kartı
text
┌─────────────┐
│             │
│  [Fotoğraf] │
│             │
├─────────────┤
│ IMG_001.jpg │
│ ⭐ 6000×4000│
└─────────────┘
Favori: Yıldız tıklanabilir.

Kullanım: Kullanılanlar işaretli.

Metadata: Boyut, tarih (hover'da).

Seçim: Tıkla, Ctrl+tıkla (çoklu), Shift+tıkla (aralık).

5.5 Arama ve filtre
Arama kutusu: Dosya adı, tarih, etiket.

Filtreler:

⭐ Favoriler

📷 Kullanılanlar

❌ Kullanılmayanlar

⚠️ Düşük DPI

📅 Tarih aralığı

🏷️ Etiket

5.6 Sıralama
Manuel: Kullanıcı sürükler.

Dosya adı: A-Z, Z-A.

Tarih: Eski-yeni, yeni-eski.

Boyut: Küçük-büyük.

5.7 İçe aktarma
Düğme: "Fotoğraf Ekle"

Sürükle-bırak: Panelin üstüne bırak.

Klasör: "Klasörden Ekle"

5.8 Bağlam menüsü (sağ tık)
Albümden çıkar

Favori ekle/çıkar

Etiket ekle

Sayfaya yerleştir

Önizleme aç

Diskten sil (uyarı ile)

6. Orta Tuval
6.1 Amaç
Sayfa düzenini göster ve düzenle.

6.2 Görünüm modları
Mod	Açıklama
Tek sayfa	Bir sayfa
Çift sayfa	İki sayfa yan yana
Tüm albüm	Grid görünümü
Sunum	Sadece tuval
6.3 Tuval öğeleri
Sayfa: Beyaz/arka planlı dikdörtgen.

Bleed çizgisi: Kırmızı kesikli (opsiyonel göster).

Safe area çizgisi: Mavi kesikli (opsiyonel göster).

Gutter çizgisi: Mor kesikli (çift sayfa).

Yuvalar: Fotoğraf alanları.

Metinler: Metin alanları.

6.4 Görünüm kontrolleri
Zoom: %25 - %800.

Ctrl + + / Ctrl + -

Ctrl + 0 → %100

Ctrl + F → ekrana sığdır

Fare tekerleği (Ctrl ile) → zoom

Pan: Boşluk tuşu + sürükle.

Cetvel: Opsiyonel (mm/inch).

Kılavuz çizgileri: Opsiyonel.

Izgara: Opsiyonel.

6.5 Etkileşimler
Eylem	Sonuç
Fotoğrafı yuvaya sürükle	Yerleştir
Yuvaya tıkla	Seç
Yuva içinde sürükle	Kırpma/konum ayarla
Yuva üzerinde çift tıkla	Kırpma moduna geç
Boş alana tıkla	Seçimi kaldır
Del	Seçili öğeyi sil
Ctrl+D	Çoğalt
Sağ tık	Bağlam menüsü
Alt + sürükle	Fotoğrafı kopyala
Shift + sürükle	Hizala
6.6 Yuva durumları
Durum	Görünüm
Boş	Gri kesikli çerçeve + "Fotoğraf ekle"
Dolu	Fotoğraf
Seçili	Mavi çerçeve
Kilitli	Kilit ikonu
Hover	Hafif vurgu
Uyarı	Sarı çerçeve (düşük DPI, taşma)
Hata	Kırmızı çerçeve (eksik dosya)
6.7 Seçim
Tek seçim: Tıkla.

Çoklu seçim: Ctrl + tıkla.

Aralık: Shift + tıkla.

Tümü: Ctrl+A.

Grup: Sürükleyerek dikdörtgen.

6.8 Hizalama araçları
Sol, orta, sağ (yatay).

Üst, orta, alt (dikey).

Eşit aralık.

Sayfaya ortala.

Öğeleri ortala.

6.9 Ölçü göstergesi
Sağ altta:

text
300 × 300 mm | Zoom: %50 | 1:1 için Ctrl+0
7. Sağ Panel — Özellikler
7.1 Amaç
Seçili öğe/şablon/fotoğraf özelliklerini göster ve düzenle.

7.2 Bağlamsal sekmeler
Sekme	Ne zaman	İçerik
Sayfa	Sayfa seçili	Şablon, arka plan, kilit
Fotoğraf	Yuva seçili	Konum, kırpma, filtre, DPI
Metin	Metin seçili	Font, boyut, renk, hizalama
Şablonlar	Her zaman	Şablon galerisi, kategoriler
Profil	Her zaman	Baskı profili özeti
Geçmiş	Her zaman	Değişiklikler listesi
7.3 Sayfa sekmesi
text
┌───────────────────────────┐
│  Sayfa 5 / 20             │
│  ─────────────────────    │
│  Şablon: [4'lü Izgara]    │
│  Arka plan: [Beyaz ▼]     │
│  Kilit:    [🔓 Açık]      │
│  Notlar:   [.......]      │
│                            │
│  [Şablonu Değiştir]       │
│  [Sayfayı Çoğalt]         │
│  [Sayfayı Sil]            │
└───────────────────────────┘
7.4 Fotoğraf sekmesi
text
┌───────────────────────────┐
│  Fotoğraf: IMG_001.jpg    │
│  6000×4000, 24 MP, 300 DPI│
│  ─────────────────────    │
│  Konum:                    │
│   X: [___] mm Y: [___] mm │
│  Ölçek:  [====|====] 100% │
│  Döndürme: [====|====] 0° │
│  Kırpma: [Düzenle...]     │
│                            │
│  Ayarlar:                  │
│  Parlaklık: [====|====] 0  │
│  Kontrast:  [====|====] 0  │
│  Doygunluk: [====|====] 0  │
│                            │
│  [Sıfırla] [Oto Düzelt]   │
│                            │
│  ⚠️ DPI 180 (hedef 300)   │
└───────────────────────────┘
7.5 Şablonlar sekmesi
Kategori filtresi: Sade, Klasik, Modern, Kolaj...

Yön filtresi: Portre, Yatay, Kare.

Fotoğraf sayısı: 1, 2, 3, 4, 5+.

Izgara: 2-4 kolon thumbnail.

Hover: Önizleme büyür.

Tıkla: Uygula.

7.6 Profil sekmesi
text
┌───────────────────────────┐
│  Baskı Profili             │
│  ─────────────────────    │
│  30×30 Layflat             │
│  300×300 mm               │
│  Bleed: 3 mm              │
│  Safe Area: 5 mm          │
│  DPI: 300                 │
│  Renk: CMYK               │
│  ✅ Doğrulandı             │
│                            │
│  [Profili Düzenle]        │
└───────────────────────────┘
7.7 Geçmiş sekmesi
Değişiklikler listesi.

Her satır: eylem + zaman.

Tıkla: o adıma geri dön.

8. Alt Şerit — Sayfa Küçük Resimleri
8.1 Amaç
Sayfaları gözat, sırala, seç.

8.2 Görünüm
text
[1] [2] [3] [4] [5] [6] [7] [8] [9] [+]
Her sayfa: thumbnail + numara.

Aktif sayfa: vurgulu.

Kilitli sayfa: kilit ikonu.

Boş sayfa: gri arka plan.

8.3 Etkileşimler
Eylem	Sonuç
Tıkla	Sayfayı seç
Çift tıkla	Sayfaya git (tuvalde)
Sürükle	Sırala
Sağ tık	Bağlam menüsü
Del	Sayfayı sil (onay)
+	Yeni sayfa ekle
Ctrl+D	Çoğalt
8.4 Bağlam menüsü
Sayfayı çoğalt

Sayfayı sil

Sayfayı kilitle

Şablonu değiştir

Sayfayı taşı (başa, sona)

Sayfa notu ekle

8.5 Kapak ve sırt
Kapak ve sırt sayfaları farklı renkte arka plan.

Simge ile işaretli.

8.6 Toplu seçim
Ctrl + tıkla: çoklu seçim.

Shift + tıkla: aralık.

Toplu işlemler: sil, çoğalt, kilitle.

9. Tema ve Görsel Kimlik
[Geçersiz — Revizyon 1: bu bölümün yerine Karar 17 (Marka) geçer. Aşağıdaki metin tarihçe için korunur.]

9.1 Tema seçenekleri
Tema	Varsayılan
Açık	✅
Koyu	❌
Sistem	❌ (ileride)
9.2 Renk paleti (Açık tema)
Öğe	Renk	Kullanım
Arka plan	#FFFFFF	Ana
Yüzey	#F5F5F5	Panel
Kenarlık	#E0E0E0	Çizgi
Metin	#212121	Normal
Metin ikincil	#757575	Açıklama
Ana renk	#0066CC	Vurgu
Başarı	#2E7D32	Onay
Uyarı	#F57C00	Dikkat
Hata	#C62828	Hata
Seçim	#E3F2FD	Seçili öğe
9.3 Renk paleti (Koyu tema)
Öğe	Renk
Arka plan	#1E1E1E
Yüzey	#252526
Kenarlık	#3E3E42
Metin	#E0E0E0
Metin ikincil	#9E9E9E
Ana renk	#4FC3F7
Başarı	#66BB6A
Uyarı	#FFA726
Hata	#EF5350
9.4 Tipografi
Ana font: Inter (ücretsiz, açık kaynak).

Fallback: Segoe UI (Windows).

Boyutlar:

Başlık: 16 px

Gövde: 14 px

Açıklama: 12 px

Küçük: 11 px

Ağırlıklar: 400 (normal), 500 (orta), 600 (kalın).

9.5 Boşluk ve kenar
Birim: 4 px grid.

Kenar yuvarlaklığı: 4 px (küçük), 8 px (orta).

Gölge: Minimal; düz tasarım.

9.6 İkonlar
Set: Feather Icons veya Tabler Icons (ücretsiz, MIT).

Boyut: 16 px (küçük), 20 px (normal), 24 px (büyük).

Renk: Metin rengiyle uyumlu.

9.7 Logo
Basit: "AA" monogram + isim.

Renk: Ana renk.

Kullanım: Üst araç, splash, hakkında.

10. Klavye Kısayolları
10.1 Proje
Kısayol	İşlev
Ctrl+N	Yeni proje
Ctrl+O	Aç
Ctrl+S	Kaydet
Ctrl+Shift+S	Farklı kaydet
Ctrl+W	Kapat
Ctrl+Q	Çık
10.2 Düzenleme
Kısayol	İşlev
Ctrl+Z	Geri al
Ctrl+Y	Yinele
Ctrl+X	Kes
Ctrl+C	Kopyala
Ctrl+V	Yapıştır
Ctrl+D	Çoğalt
Del	Sil
Ctrl+A	Tümünü seç
Esc	Seçimi kaldır
10.3 Görünüm
Kısayol	İşlev
Ctrl+0	%100 zoom
Ctrl++	Yakınlaştır
Ctrl+-	Uzaklaştır
Ctrl+F	Ekrana sığdır
Ctrl+1	Sol panel
Ctrl+2	Sağ panel
Ctrl+3	Alt şerit
F11	Tam ekran
Tab	Panelleri gizle
Ctrl+R	Cetvel
10.4 Çalışma
Kısayol	İşlev
Ctrl+G	Otomatik oluştur
Ctrl+P	Önizleme
Ctrl+E	Dışa aktar
Ctrl+K	Komut paleti
Ctrl+,	Ayarlar
F1	Yardım
F2	Yeniden adlandır
Ctrl+L	Kilitle
10.5 Sayfa
Kısayol	İşlev
Ctrl+Alt+N	Yeni sayfa
Ctrl+Alt+D	Sayfayı çoğalt
Ctrl+Alt+Del	Sayfayı sil
PgUp	Önceki sayfa
PgDn	Sonraki sayfa
Home	İlk sayfa
End	Son sayfa
10.6 Fotoğraf
Kısayol	İşlev
1-9	Fotoğrafı yuvaya yerleştir
R	Döndür
H	Yatay çevir
V	Dikey çevir
Ctrl+Shift+R	Sıfırla
10.7 Kısayol özelleştirme
v1.1'de: kullanıcı kısayolları değiştirebilir.

Çakışma kontrolü.

Varsayılana dön.

11. Komut Paleti
11.1 Amaç
Klavyeden her eyleme hızlı erişim.

11.2 Açma
Ctrl+K veya Ctrl+Shift+P.

11.3 Görünüm
text
┌─────────────────────────────────────┐
│  🔍 Ne yapmak istersiniz?           │
├─────────────────────────────────────┤
│  > Yeni proje              Ctrl+N   │
│  > Otomatik oluştur        Ctrl+G   │
│  > Dışa aktar              Ctrl+E   │
│  > Şablon değiştir                  │
│  > Profil düzenle                   │
│  > Tema: Koyu                       │
│  ...                                │
└─────────────────────────────────────┘
11.4 Özellikler
Bulanık arama.

Kısayol gösterimi.

Son kullanılanlar üstte.

Klavye ile gezinme (↑↓, Enter).

11.5 Komut kategorileri
Proje

Düzenleme

Görünüm

Sayfa

Fotoğraf

Şablon

Profil

Çıktı

Ayarlar

12. Onboarding
12.1 İlk açılış
Karşılama ekranı:

text
┌─────────────────────────────────────┐
│        NexAlbum'a             │
│        Hoş Geldiniz                 │
│                                     │
│  [ Yeni Proje ]                     │
│  [ Örnek Projeyi Aç ]               │
│  [ Kısa Tur ]                       │
│                                     │
│  ☐ Bir daha gösterme                │
└─────────────────────────────────────┘
12.2 Kısa tur (3 adım)
Fotoğrafları içe aktarın.

Otomatik oluşturun.

PDF olarak dışa aktarın.

Her adım: metin + görsel + "İleri" düğmesi.

12.3 Örnek proje
10 sayfa, 20 fotoğraf.

Tüm özellikleri gösterir.

Kullanıcı üzerinde deneyebilir.

samples/example-album.albumproj

12.4 İpuçları
İlk kullanımda tooltip'ler.

Her önemli düğmede "?" ikonu.

Kapatılabilir.

Bir daha gösterilmeme seçeneği.

12.5 Yardım menüsü
F1 → yardım.

Kılavuz (markdown).

SSS.

Klavye kısayolları listesi.

Klavye kısayolları kartı (yazdırılabilir PDF).

13. Bildirimler ve Geri Bildirim
13.1 Bildirim türleri
Tür	Süre	Konum
Toast	3 sn	Sağ alt
Banner	Kalıcı	Üst
Modal	Kullanıcı kapatana kadar	Merkez
Durum çubuğu	Kalıcı	Alt
13.2 Toast örnekleri
✅ "Proje kaydedildi"

⚠️ "5 fotoğraf bulunamadı"

❌ "Dışa aktarma başarısız"

13.3 Banner örnekleri
"Bu proje 3 fotoğraf eksik. [Listele]"

"Yeni sürüm mevcut. [İndir]"

13.4 Modal örnekleri
Kaydetmeden çıkma onayı.

Silme onayı.

Preflight hataları.

İlk kurulum.

13.5 Uzun işlemler
Progress bar.

İptal düğmesi.

Tahmini süre (mümkünse).

Arka planda çalışabilir.

13.6 Sessiz mod
Toplu işlemlerde bildirimler gruplanır.

Örn. "5 sayfa oluşturuldu" (tek toast).

14. Hata Mesajları
14.1 3 parçalı yapı
Her hata mesajı:

Ne oldu? (kısa)

Neden? (açıklama)

Ne yapmalı? (öneri)

14.2 Örnekler
Örnek 1:

text
❌ Fotoğraf bulunamadı
"IMG_001.jpg" dosyası taşınmış veya silinmiş.

Öneri:
• "Yeniden Bağla" ile yeni konumu seç
• "Kaldır" ile projeden çıkar
Örnek 2:

text
❌ PDF oluşturulamadı
Hedef klasöre yazma izni yok.

Öneri:
• Farklı bir klasör seç
• Yönetici olarak çalıştır
Örnek 3:

text
⚠️ Düşük çözünürlük
Sayfa 12'deki fotoğraf 180 DPI (hedef 300).

Bu fotoğraf baskıda bulanık görünebilir.

Öneri:
• Daha yüksek çözünürlüklü fotoğraf kullan
• Yine de devam et
14.3 Hata detayı
"Detayları göster" düğmesi.

Log yolu.

Kopyalama düğmesi (hata raporu için).

14.4 Hata raporlama
Opsiyonel.

Anonim.

Kullanıcı onayı ile.

GitHub Issues'a yönlendirme.

14.5 Çökme sonrası
text
┌─────────────────────────────────────┐
│  Uygulama beklenmedik şekilde       │
│  kapandı.                            │
│                                      │
│  Kaydedilmemiş değişiklikler        │
│  kurtarılabilir.                    │
│                                      │
│  [ Kurtar ve Aç ]  [ Kapat ]        │
└─────────────────────────────────────┘
15. Erişilebilirlik
15.1 Klavye gezinme
Tüm etkileşimler klavye ile.

Tab / Shift+Tab ile gezinme.

Mantıklı sıra.

Odak göstergesi net (mavi çerçeve).

15.2 Ekran okuyucu
Temel destek (WPF AutomationProperties).

Ana öğeler etiketli.

Kritik eylemler duyurulur.

İleri düzey destek v2.0'da.

15.3 Yüksek kontrast
Sistem yüksek kontrast modu algılanır.

Tema otomatik uyum sağlar.

Özel yüksek kontrast teması.

15.4 Renk körlüğü
Renk tek başına anlam taşımaz.

İkon + renk kombinasyonu.

Deuteranopia/Protanopia testleri.

15.5 Yazı boyutu
Sistem ölçeği algılanır.

Kullanıcı ayarlardan büyütebilir.

%75 - %200 arası.

15.6 Animasyon azaltma
Sistem ayarı algılanır.

Animasyonlar azaltılır/kapatılır.

Tercih edilen ayar.

15.7 Fokus göstergesi
Her odaklanabilir öğede görünür.

Yüksek kontrastlı.

Boyut: 2 px.

16. Yerelleştirme (i18n)
16.1 Altyapı
Format: .resx (WPF standardı) veya JSON.

Kütüphane: Microsoft.Extensions.Localization.

Kod: Strings.AppTitle, Strings.SaveButton gibi anahtarlar.

Kaynak: Resources/Strings.tr.resx.

16.2 Diller
Dil	Durum
Türkçe	✅ Varsayılan, tam
İngilizce	v1.1
Almanca	Topluluk katkısı
Diğer	Topluluk katkısı
16.3 Çeviri kuralları
Doğal Türkçe. "Dosya kaydedildi" değil "Kaydedildi".

Kısa. Buton metinleri 1-2 kelime.

Tutarlı. Aynı kavram her yerde aynı.

Bağlam. Çevirmene not.

16.4 Tarih/sayı formatı
Tarih: dd.MM.yyyy (TR) / sistem.

Sayı: 1.234,56 (TR).

Ölçü: mm (varsayılan).

16.5 RTL (sağdan sola)
Şimdilik yok.

İleride (Arapça, İbranice).

Yapı hazır (WPF FlowDirection).

17. Animasyon ve Geçişler
17.1 İlke
Minimal. Animasyon işlevsel olmalı.

Hızlı. 100-250 ms.

İptal edilebilir. Kullanıcı bekleyemez.

17.2 Animasyonlar
Animasyon	Süre	Amaç
Panel aç/kapa	150 ms	Yumuşak geçiş
Toast giriş/çıkış	200 ms	Dikkat
Seçim vurgusu	100 ms	Geri bildirim
Modal giriş	150 ms	Odak
Progress	Sürekli	Durum
17.3 Animasyon yok
Fotoğraf render.

Zoom/pan.

Sayfa değişimi.

Kritik işlemler.

17.4 Azaltma
Sistem ayarına uy.

Kullanıcı kapatabilir.

0 ms seçeneği.

18. Ses ve Titreşim
18.1 Ses
Yok (varsayılan).

Opsiyonel: başarı/hata sesi.

Kullanıcı açabilir.

Sistem sesleri kullanılır (WPF).

18.2 Titreşim
Windows'ta masaüstü için yok.

Gelecekte dokunmatik için.

19. Kullanıcı Ayarları
19.1 Kategoriler
Kategori	Ayarlar
Genel	Dil, tema, yazı boyutu
Düzen	Panel boyutları, varsayılan görünüm
Düzenleme	Otomatik kayıt aralığı, undo seviyesi
Şablonlar	Varsayılan kategori, favoriler
Çıktı	Varsayılan format, kalite, klasör
Kısayollar	Özelleştirme (v1.1)
Gelişmiş	Log seviyesi, önbellek boyutu
Hakkında	Sürüm, lisans, katkıcılar
19.2 Kaydetme
%APPDATA%/NexAlbum/settings.json.

Anında kaydedilir.

Sıfırlama seçeneği.

19.3 Taşınabilir mod
Ayarlar ./data/settings.json (uygulama klasöründe).

Kurulum gerekmez.

20. Performans ve Algılanan Hız
20.1 Açılış
Süre	Deneyim
< 1 sn	Mükemmel
1-3 sn	İyi
> 3 sn	Kabul edilemez
Splash ekranı gerekmez (< 3 sn).

İlk açılışta örnek proje yüklenmez.

20.2 Yanıt süresi
İşlem	Hedef
Düğme tıklama	< 100 ms
Sayfa değiştirme	< 200 ms
Fotoğraf yükleme	< 500 ms
Thumbnail görüntüleme	< 100 ms
Filtre uygulama	< 300 ms
20.3 Uzun işlemler
Progress bar.

İptal düğmesi.

Arka planda.

UI donmaz.

Bildirim.

20.4 Önbellek
Thumbnail cache.

Preview cache.

Şablon listesi cache.

Profil listesi cache.

20.5 Algılanan hız
Skeleton loading (kısa süreli).

Optimistic UI (mümkünse).

Animasyon ile geçiş.

21. Test Stratejisi (UX)
21.1 Kullanılabilirlik testleri
5 kullanıcıyla ilk test (v0.5).

3 görev:

Yeni proje + 10 fotoğraf + otomatik oluştur.
Bir fotoğrafı değiştir + kaydet.
PDF olarak dışa aktar.
Gözlem: nerede takılıyor?

21.2 Sezgisel değerlendirme
Nielsen'in 10 sezgisi.

Her ekran gözden geçirilir.

21.3 Erişilebilirlik testleri
Klavye ile tüm görevler.

Ekran okuyucu (Narrator).

Yüksek kontrast.

Renk körlüğü simülatörü.

21.4 Yerelleştirme testleri
Uzun metin testi (Almanca).

Türkçe karakter testi (İ, ı, ş, ğ, ü, ö, ç).

Kısayol çakışması.

21.5 Geri bildirim kanalları
GitHub Issues.

Anket (opsiyonel).

Yorumlar.

22. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
Arayüz karmaşıklaşır	Orta	Yüksek	Tasarım ilkeleri; 5 bölge
Kullanıcı kaybolur	Orta	Orta	Onboarding, tooltip, komut paleti
Kısayol çakışması	Orta	Düşük	Çakışma kontrolü
Türkçe karakter sorunu	Düşük	Orta	UTF-8, test
Performans sorunu	Orta	Yüksek	Erken test, profil
Erişilebilirlik eksik	Orta	Orta	Otomatik test, kontrol listesi
Tema tutarsızlığı	Orta	Düşük	Tasarım token'ları
Animasyon abartı	Düşük	Düşük	Minimal animasyon
Modal yorgunluğu	Orta	Orta	Az modal, inline bildirim
Renk körlüğü	Orta	Orta	İkon + renk
Küçük ekran	Orta	Orta	Responsive paneller
Yüksek DPI	Orta	Düşük	Vektörel ikonlar
Kullanıcı geri bildirimi yok	Orta	Orta	Test kullanıcıları
23. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Ücretsiz font/ikon; ücretsiz UX araçları
02 — Açık Kaynak	Tasarım varlıkları açık lisanslı
03 — Kullanıcı Profilleri	Profil düzenleme ekranı
04 — Tek Kişi	Sade tasarım; sürdürülebilir
05 — Zaman	Aşama 2-5'te uygulanır
07 — Rakip Özellikleri	Sade + güçlü UX
08 — Teknoloji	WPF, XAML, MVVM
10 — Şablon Motoru	Şablon paneli
11 — Çıktı	Dışa aktarma ekranı
24. Açık Sorular (Sonraki Kararlara Bırakılan)
Kısayol özelleştirme ne zaman? → v1.1

İngilizce çeviri ne zaman? → v1.1

RTL desteği? → v2.0

Ekran okuyucu ileri destek? → v2.0

Çalışma alanları (workspace)? → v1.2

Sekmeli düzen? → v1.2

Komut paleti genişletme? → v1.1

25. Özet
NexAlbum'in arayüzü:

5 bölge: Üst araç, sol galeri, orta tuval, sağ özellikler, alt sayfa şeridi.

Sade: Fotoğraf önce; gereksiz öğe yok.

Türkçe: Doğal Türkçe; i18n altyapısı hazır.

Klavye dostu: Her eylem için kısayol; komut paleti.

Tema: Açık + Koyu; minimal, düz tasarım.

Onboarding: 3 adımlı tur + örnek proje.

Erişilebilirlik: Klavye, ekran okuyucu temel, yüksek kontrast.

Hata mesajları: Ne oldu + neden + ne yapmalı.

Bildirimler: Toast, banner, modal, durum çubuğu.

Animasyon: Minimal, hızlı, iptal edilebilir.

Ayarlar: Kategorize, JSON, taşınabilir.

Performans: Açılış < 1 sn, yanıt < 100 ms.

Bu karar, "sadelik + güç" dengesini kurar. Rakiplerin en iyi UX özellikleri (Canva'nın kolaylığı, Lightroom'un galerisi, VS Code'un kısayolları) ödünç alınır; karmaşıklık reddedilir. Tek geliştirici için sürdürülebilir, kullanıcı için keyifli bir arayüz hedeflenir.

Onay: Bu karar, ürünün kullanıcı deneyimi temelinin belgesidir.