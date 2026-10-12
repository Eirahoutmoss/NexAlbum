# NexAlbum — Claude Code kuralları

Windows öncelikli, yerel, ücretsiz, açık kaynak (GPL-3.0-or-later) Türkçe albüm tasarım masaüstü uygulaması.
Kullanıcıyla Türkçe yaz. Karar belgeleri bağlayıcıdır: `docs/kararlar/` — hangi göreve hangi belge, bkz. `docs/kararlar/INDEKS.md`. Tamamını okuma; yalnızca ilgili bölümü oku.

## Mimari (Karar 08 §13)
C# 12 / .NET 8 / WPF / SkiaSharp / PDFsharp / SQLite / Serilog / CommunityToolkit.Mvvm. MVVM + Clean Architecture.
Bağımlılık yönü — ihlal etme:
- `NexAlbum.Core` → hiçbir şey (saf domain; WPF, Skia, IO yok)
- `Rendering` → Core · `Export` → Core, Rendering · `Assets` → Core · `Persistence` → Core
- `NexAlbum.UI` (WPF) → hepsi

## Değişmez kurallar
- Değer uydurma yok: bleed, DPI, ICC, safe area, gutter profilden gelir; yoksa "yok" denir (Karar 03, 11).
- Ölçüler domain'de mm, `double` (Karar 09).
- `.albumproj` = ZIP + JSON; her dosyada `schemaVersion`; şema değişikliği = migration + testi (Karar 09 §14).
- Otomatik yerleşim deterministik: aynı girdi + tohum → aynı çıktı; `Random` yalnızca tohumla (Karar 10 §15).
- Yeni bağımlılık yalnızca GPL-3.0 uyumlu lisansla; Karar 08 lisans tablosuna ekle.
- Nullable açık, analyzer uyarısı yok, `.editorconfig` stili (Karar 13 §11).
- Kararla çelişen bir değişiklik gerekiyorsa kodu değiştirme; önce kullanıcıya sor. Karar belgesi silinmez, "Geçersiz" işaretlenir.

## Komutlar
- Derleme: `dotnet build NexAlbum.sln -warnaserror`
- Test (dar → geniş): `dotnet test tests/<Proje> --filter "FullyQualifiedName~<Sınıf>"` → `dotnet test NexAlbum.sln`
- Kapsam: `dotnet test --collect:"XPlat Code Coverage"` (hedef: kritik yol ≥%80, Domain ≥%85, genel ≥%70)
- Biçim: `dotnet format --verify-no-changes`
- Test: xUnit + Moq + FluentAssertions; E2E: FlaUI; benchmark: BenchmarkDotNet; örnek dosyalar `samples/tests/`.

## Çalışma biçimi
- Kapsamı önce Glob/Grep ile belirle; ilgisiz dosyayı, aynı dosyayı/çıktıyı tekrar okuma. Uzun çıktıyı `| tail -n 40` veya hata satırlarıyla sınırla.
- Küçük işte doğrudan uygula. Çok dosyalı veya mimari işte önce dokunulacak dosyaları ve yaklaşımı 2–5 satırda yaz, onay al.
- Hata: kök nedeni bul; yeni bilgi olmadan aynı komutu tekrarlama; bağımlılık/yapılandırma hatası çözülmeden sonraki aşamaya geçme.
- Testleri silme, `Skip` etme, assert gevşetme. Başarısız derleme/test "tamamlandı" sayılmaz.
- Alt ajan yalnızca belirgin fayda varsa (ör. geniş arama → Explore).
- Commit: Conventional Commits, DCO (`git commit -s`), dallar `feature/…`, `fix/…`, `docs/…` (Karar 14).

## Görev sonu raporu (yalnızca bunlar)
1. Değişiklikler 2. Çalıştırılan komutlar 3. Sonuçlar 4. Kalan engeller 5. Sıradaki mantıklı görev
