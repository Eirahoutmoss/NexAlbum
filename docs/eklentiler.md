# NexAlbum eklenti paketleri (.nexpaket)

NexAlbum'e yeni **film stilleri** (ileride yazı stilleri, çerçeveler, fotoğraf görünümleri ve şablonlar) eklemek için paket hazırlayabilirsiniz.
Paket yalnızca veri taşır; kod çalıştırmaz (Karar 22).

## Paket nasıl yapılır?

1. Bir klasör açın ve içine `manifest.json` koyun (örnek: [`samples/eklentiler/ornek-sinema/manifest.json`](../samples/eklentiler/ornek-sinema/manifest.json)).
2. Klasörün **içeriğini** ZIP'leyin ve uzantıyı `.nexpaket` yapın (ZIP'in kökünde `manifest.json` olmalı).
3. NexAlbum'de Komut paleti (Ctrl+K) → **Eklenti paketi yükle** → dosyayı seçin. Yeni stiller **Hikâye filmi** penceresinde paket adıyla görünür.

Paketler `%LOCALAPPDATA%\NexAlbum\eklentiler` klasöründe durur (Komut paleti → **Eklenti klasörünü aç**). Klasör olarak da bırakılabilir.

## manifest.json

```json
{
  "format": "nexalbum-extension",
  "formatVersion": 1,
  "id": "pro-sinema",
  "name": "Pro Sinema",
  "version": "1.0.0",
  "author": "Stüdyo Adı",
  "license": "CC-BY-4.0",
  "description": "Kısa açıklama",
  "videoStyles": [
    {
      "id": "noir",
      "name": "Noir",
      "hint": "Listede adın altında görünen açıklama",
      "transitions": ["crossfade", "dipToBlack", "focusShift"],
      "chapterTransition": "dipToBlack",
      "photoSeconds": 4.2,
      "transitionSeconds": 1.3,
      "motion": 0.1,
      "look": "blackWhite",
      "vignette": true,
      "grain": true
    }
  ]
}
```

| Alan | Değerler |
|---|---|
| `id` (paket ve stil) | küçük harf, rakam, tire; en fazla 64 karakter. Stilin tam kimliği `paket-id/stil-id` olur. |
| `transitions` | Bölüm içinde sırayla kullanılan geçişler: `cut`, `crossfade`, `dipToBlack`, `dipToWhite`, `focusShift`, `iris`, `pushLeft`, `pushRight`, `pushUp`, `zoomThrough` ve pro efektler (Karar 23): `aperture`, `diaphragm`, `doubleIris`, `diamondReveal`, `rackFocus`, `tiltShift`, `depthFocus`, `lightLeak`, `filmBurn`, `flash`, `projector`, `curtain`, `blinds`, `diagonalWipe`, `mosaic`, `honeycomb`, `inkBlot`, `split`, `parallax`, `whipPan`, `cardStack`, `polaroidDevelop`, `pageTurn` |
| `chapterTransition` | Bölüm başlarındaki geçiş (aynı değerler; varsayılan `dipToBlack`) |
| `photoSeconds` | Fotoğraf başına süre, 0,8–15 sn (varsayılan 3,5) |
| `transitionSeconds` | Geçiş süresi, 0–4 sn (varsayılan 1) |
| `motion` | Yavaş yaklaşma/kayma miktarı, 0–0,3 (varsayılan 0,08) |
| `look` | `natural`, `warm`, `cool`, `blackWhite`, `sepia` |
| `vignette`, `grain` | `true` / `false` |

Geçersiz bir stil atlanır ve nedeni gösterilir; paketin diğer stilleri yüklenir. `textStyles`, `frameStyles`, `photoLooks`, `templates`
alanları sonraki sürümler için ayrılmıştır (şimdilik atlanır).
