Karar Dokümanı 23 — Pro Efektler (Film, Sunum ve HTML Prova)
Proje: NexAlbum
Karar no: 23
Konu: Programa gömülü pro geçiş efektleri, başlık animasyonları ve çok fotoğraflı sahneler
Durum: Kesinleşti
Tarih: 14/10/2026
İlgili kararlar: 21 (Hikâye filmi), 22 (Eklenti paketleri), 11 Rev. 2 (HTML prova), 10 (Odak/şablon dili), 08 (Teknoloji)
İstek: Hasan Güler — iki örnek bileşen (Aperture: tıklanan noktadan açılan iris + halka; Focus Pull: ortası net, yanları bulanık şerit)
"çoğaltılsın, 25–30 senaryo olsun; hikâye geçişlerinde, sunumda ve müşteriye verilen HTML dosyasında kullanılsın"; ardından
"programa direkt entegre edelim, ayrıca bir şey yapmaya gerek kalmasın".

1. Karar Özeti
Efektler eklenti paketi olarak değil, **programın içinde** gelir; kurulum gerekmez. Örnek bileşenlerin kodu alınmadı (lisans belirtilmemiş,
React/web); fikir NexAlbum'ün kendi mimarisinde yeniden yazıldı. Tek bir hesap (Core) üç çiziciye beslenir: film (SkiaSharp), sunum
(WPF; aynı Skia çizimi) ve HTML prova (tarayıcı tuvali). Aynı efekt + ilerleme + oran + başlangıç noktası + tohum → aynı kare.

2. Efektler (33 geçiş)
| Grup | Efektler |
|---|---|
| Diyafram | Aperture (tıklanan noktadan iris, kenarda halka, yeni kare 1,18→1), Diyafram kanatları (dönen altıgen), Çift iris, Elmas açılış, Dairesel iris |
| Odak | Netlik kaydırma (rack focus), Odak kaydırma, Tilt-shift, Derinlikten |
| Işık ve film | Işık sızıntısı, Film yanığı, Flaş, Projektör, Polaroid banyosu |
| Klasik | Çapraz geçiş, Siyaha/Beyaza geçiş, Kesme |
| Maske | Perde, Yarık, Panjur, Çapraz silme, Mozaik, Petek, Mürekkep lekesi |
| Hareket | Paralaks, Hızlı kaydırma (whip pan), Sola/Sağa/Yukarı itme, Yakınlaşarak |
| Albüm | Kart yığını, Sayfa çevirme |
Başlık animasyonları: yavaş yaklaşma, maskeden yükselen satırlar, el yazısı çizimi, bulanıktan netleşen kelimeler.
Sahneler: Odak galerisi (Focus Pull şeridi), Kolaj akışı, Albüm açılışı (kapak açılır, ilk çift sayfa).
Film stilleri (11 yeni): Aperture, Objektif, Odak Galerisi, Işık, Analog, Geometri, Kolaj Akışı, Sahne, Albüm, Mürekkep, Hız.

3. Hesap ve çizim
- `Core/Effects/TransitionEffects`: her efekt saf bir işlevdir → `EffectFrame` (çıkan/giren katmanın ölçek, döndürme, kaydırma, opaklık,
  bulanıklık, doygunluk, parlaklık, gölge; giren katmanın maske çokgenleri; halka/ışıma/bant/düz renk üst katmanları). Alan: genişlik =
  en/boy oranı, yükseklik = 1. Rastgelelik tohumlu "mulberry32" (JavaScript ile birebir).
- `FocusPull`: şeritte ortaya uzaklığa göre ölçek (−%14), bulanıklık, opaklık (−%50), doygunluk (−%45).
- `Rendering/Effects/EffectPainter` (Skia) ve `Export/Proof/Assets/effects.js` (tarayıcı) aynı kareyi çizer. Eşdeğerlik: `EffectsScriptTests`
  tüm efektleri çeşitli ilerleme/oran/tohumlarda C# ve betik (Jint, yalnızca testte) ile karşılaştırır.
- Hareket azaltma tercihi (sunum/HTML) bulanıklık ve maskeleri kapatıp yumuşak geçişe döner.

4. Kullanım yerleri
- Hikâye filmi: stil seçimi, bölüm başına geçiş; tümü kare kare ve deterministik.
- Sunum (F5): tıklanan noktadan açılan geçiş, Sonraki/Önceki merceği, otomatik oynatma (dolan çizgi), «Göz at» odak şeridi; E efekt değiştirir.
- HTML prova: sayfalar odak şeridinde gezilir (sürükle, kaydır, ← →), ortadaki sayfa için ♥ ve not; «Gösteri» tam ekran, efekt seçilebilir
  (ya da "Karışık"), tıklanan noktadan açılır, mercek imleci, sayaç ve dolan çizgi. Tek dosya, çevrimdışı; dış kaynak yok.

5. Değişmeyenler
Baskı (PDF) etkilenmez; efektler yalnızca ekran, film ve prova içindir. Eklenti paketleri (Karar 22) yeni efekt adlarını film stillerinde
kullanabilir (kod taşımaz).
