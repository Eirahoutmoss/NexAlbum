# Katkı rehberi

NexAlbum'e katkı vermek istediğiniz için teşekkürler! Kod yazmanız gerekmez: hata bildirmek, şablon ya da baskı profili
paylaşmak, belgeleri düzeltmek de çok değerli.

## Nasıl katkı verebilirim?

- **Hata bildirin** — [Hata bildirimi](https://github.com/Eirahoutmoss/NexAlbum/issues/new?template=hata.yml) formu.
- **Özellik önerin** — [Öneri](https://github.com/Eirahoutmoss/NexAlbum/issues/new?template=oneri.yml) formu.
- **Şablon / baskı profili / eklenti paketi** paylaşın — biçimler: [`docs/eklentiler.md`](docs/eklentiler.md), [`docs/kararlar/`](docs/kararlar/INDEKS.md).
- **Kod, test, çeviri, belge** — aşağıdaki akışla.

Büyük bir değişiklikten önce bir issue açıp tartışın; mimari ve ürün kararları [`docs/kararlar/`](docs/kararlar/INDEKS.md) altında
yazılıdır ve bağlayıcıdır. Bir karara aykırı değişiklik gerekiyorsa önce kararın revizyonunu önerin.

## Geliştirme ortamı

1. Depoyu fork edip klonlayın.
2. [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) kurun (`dotnet --list-sdks`).
3. Derleyin ve test edin:

   ```powershell
   dotnet build NexAlbum.slnx
   dotnet test NexAlbum.slnx
   dotnet run --project src/NexAlbum.UI
   ```

   Dar test: `dotnet test tests/NexAlbum.Core.Tests --filter "FullyQualifiedName~StoryTests"`.
   Biçim denetimi: `dotnet format NexAlbum.slnx --verify-no-changes`.
   Arayüz testleri yalnızca Windows'ta çalışır (Linux'ta derlenir, atlanır).

## Kod kuralları

- **Katmanlar:** `Core` hiçbir şeye bağımlı değildir (WPF, Skia, IO yok); `Rendering`, `Export`, `Assets`, `Persistence` yalnızca
  `Core`'a; `UI` hepsine. Kural `tests/NexAlbum.Architecture.Tests` ile denetlenir.
- **Değer uydurma yok:** taşma payı, DPI, güvenli alan, sırt genişliği profilden gelir; yoksa "yok" denir.
- **Kültür:** dosya biçimi, JSON, sayı/tarih → `CultureInfo.InvariantCulture`; dize karşılaştırma → `StringComparison.Ordinal`.
- **Deterministiklik:** rastgelelik yalnızca tohumla (`DeterministicRandom`); `System.Random` kullanılmaz.
- **Dosya biçimi:** `.albumproj` şeması değişirse göç adımı + testi zorunlu.
- **Bağımlılık:** yalnızca GPL-3.0 uyumlu lisanslar; sürüm `Directory.Packages.props`'ta, lisans Karar 08 tablosunda.
- Uyarılar hatadır; `.editorconfig` stiline uyun. Testleri silmeyin, atlamayın, gevşetmeyin.

## Commit ve PR

- Dal adı: `feature/…`, `fix/…`, `docs/…`.
- [Conventional Commits](https://www.conventionalcommits.org/tr/): `feat(hikaye): …`, `fix(pdf): …`, `docs: …`.
- **DCO:** her commit `git commit -s` ile imzalanır (CLA yok).
- PR şablonunu doldurun; arayüz değişikliğinde ekran görüntüsü ekleyin (CI her dalda iki temada üretir: `ekran/<dal>` dalı).
- CI (derleme, Windows + Ubuntu testleri) yeşil olmalı.

## Lisans

Katkılarınız [GPL-3.0-or-later](LICENSE) lisansıyla yayımlanır.
