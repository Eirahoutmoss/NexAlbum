# Marka

Bağlayıcı karar: [`docs/kararlar/17_marka_karari.md`](../kararlar/17_marka_karari.md).

| Dosya | İçerik |
|---|---|
| `nexalbum-logo.svg` | Logo, ≥ 40 px (katlanma üçgenleri ve ışık çizgisiyle) |
| `nexalbum-logo-kucuk.svg` | Logo, ≤ 32 px (sadeleştirilmiş, büyütülmüş "N", kalın halka) |
| `nexalbum-logo-256.png` | 256 px önizleme (README, web) |
| `nexalbum-marka-konsepti.png` | Hasan Güler'in 13 Ekim 2026 konsepti — kaynak görsel; bağlayıcı değil |

Uygulamadaki karşılıkları: `src/NexAlbum.UI/Themes/Brand.xaml` (vektör logo ve yazı markası),
`src/NexAlbum.UI/Assets/NexAlbum.ico` (16–256 px) ve `Assets/splash.png` (açılış ekranı).

## Yeniden üretim
Logo çizimi değişirse önce iki SVG güncellenir, `Brand.xaml`'daki geometri onlarla eşitlenir, sonra:

```bash
pip install pillow playwright && python -m playwright install chromium
python tools/marka/uret.py   # NexAlbum.ico, splash.png, nexalbum-logo-256.png
```

## Kullanım
- Logo döndürülmez, eğilmez, renkleri değiştirilmez; çevresinde halka kalınlığının en az 4 katı boşluk bırakılır.
- Konseptteki fotoğraflı açılış görselleri (gelin-damat vb.) dağıtılmaz; lisansı net olmayan görsel kullanılmaz.
