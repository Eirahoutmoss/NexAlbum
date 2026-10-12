Karar Dokümanı 02 — Açık Kaynak Kod Modeli
Proje: NexAlbum
Karar no: 02
Konu: Ürünün açık kaynak olması, lisans seçimi ve topluluk modeli
Durum: Kesinleşti
Tarih: (doldurulacak)
İlgili kararlar: 01 (Ücretsiz), 04 (Tek Kişi Geliştirme), 06 (Bütçe)

1. Karar Özeti
NexAlbum tamamen açık kaynak bir masaüstü uygulaması olacaktır.

Kaynak kodu herkese açık.

Lisans: GPL-3.0-or-later (gerekçesi §4'te).

Geliştirme GitHub üzerinde, herkese açık.

Katkılar (PR, issue, tartışma) memnuniyetle karşılanır.

Çatal (fork) serbest.

Ticari kullanım serbest (GPL koşulları çerçevesinde).

Kapalı kaynak türev yasak (GPL doğası gereği).

Marka adı ve logo ayrı korunur (§9).

Bu karar, 01 numaralı "Ücretsiz" kararıyla birlikte alınmıştır ve onunla tutarlıdır. Ücretsiz olmak açık kaynağı gerektirmez ama açık kaynak olmak ürünün kimliğini güçlendirir.

2. "Açık Kaynak" Ne Anlama Geliyor?
2.1 Kullanıcı açısından
Kaynak kodu okuyabilir, inceleyebilir, denetleyebilir.

Ne yaptığını bilmediği bir "kara kutu" değildir.

Gizli veri toplama, arka kapı, reklam SDK'sı olmadığını doğrulayabilir.

Kendi ihtiyacına göre değiştirebilir.

Kendi derlemesini yapabilir.

Güvenmediği bir sürümü kullanmak zorunda değildir.

Hata bulursa bildirebilir, hatta düzeltebilir.

Ürün "ölse" bile kod ortada kalır, topluluk devam ettirebilir.

2.2 Geliştirici açısından
Kod tabanı kamuya açık; her satır savunulabilir olmalı.

Hata bildirimleri ve PR'lar topluluktan gelir.

Test yükü bir miktar paylaşılır.

Çeviri, şablon, profil gibi katkılar topluluktan gelebilir.

"Neden bunu yaptın?" sorusuna kod cevap verir.

Ün ve portfolyo değeri taşır.

Başka projelerde kullanılabilir (GPL uyumluysa).

2.3 Mimari açısından
Modüler yapı zorunlu. Her katman ayrı ayrı anlaşılabilir olmalı.

Okunabilirlik öncelikli. Kod, yorum ve dokümantasyon İngilizce (uluslararası katkı için); kullanıcıya görünen metinler Türkçe + i18n.

Test edilebilirlik zorunlu. Katkı kabulü için test şartı.

Bağımlılıklar şeffaf. Lisans uyumu kontrol edilir (§7).

Derleme tek komutla. dotnet build veya eşdeğeri.

Kod stili tutarlı. Editör yapılandırması (EditorConfig) ve biçimlendirici (formatter) depoda.

2.4 Topluluk açısından
Katkı rehberi (CONTRIBUTING.md) net.

Davranış kuralları (CODE_OF_CONDUCT.md) var.

Sorun şablonları (issue templates) standart.

Tartışma alanı (Discussions) açık.

Sürüm notları düzenli.

Yol haritası kamuya açık.

3. "Açık Kaynak" Ne Anlama Gelmiyor?
Yanlış beklentileri önlemek için sınırlar:

"Herkes istediğini yapar" değildir. Katkılar gözden geçirilir; kalite ve uyum şartı vardır.

"Ücretsiz destek" değildir. Topluluk yardımlaşır; birebir destek vaadi yoktur.

"Her özellik kabul edilir" değildir. Yol haritasına uymayan özellikler reddedilebilir.

"Marka serbest" değildir. Kod GPL'dir ama "NexAlbum" adı ve logosu ayrı korunur (§9).

"Sınırsız kaynak" değildir. Tek geliştirici + topluluk; SLA yok.

"Her çatal meşrudur" değildir. GPL uyumlu olmayan çatallar (örneğin kapalı kaynak türev) lisans ihlalidir.

4. Lisans Seçimi
4.1 Değerlendirilen lisanslar
Lisans	Artıları	Eksileri	Uygunluk
MIT	Basit, özgürlükçü, ticari dostu	Kapalı kaynak türev serbest; "geri katkı" zorunlu değil	❌ Ürünün ruhuna uymaz
Apache 2.0	Patent koruması, ticari dostu	Kapalı kaynak türev serbest	❌ Aynı sorun
MPL 2.0	Dosya bazlı copyleft; dengeli	Topluluk için karmaşık	⚠️ Düşünülebilir
LGPL 3.0	Kütüphane dostu	Masaüstü uygulaması için gereksiz karmaşık	❌ Uygun değil
GPL 3.0	Güçlü copyleft; türev açık kalır; patent koruması	Ticari kapalı türev yasak; bazı şirketler kaçınır	✅ Seçildi
AGPL 3.0	Ağ üzerinden kullanımda da copyleft	Yerel masaüstü uygulaması için gereksiz	❌ Aşırı
4.2 Seçim: GPL-3.0-or-later
Gerekçeler:

Ürünün ruhuyla uyumlu. NexAlbum ücretsiz ve topluluğa ait. Birinin kodu alıp kapalı kaynak ücretli ürüne dönüştürmesi engellenir.

Geri katkı zorunlu. İyileştirmeler topluluğa döner.

Patent koruması. Apache 2.0'dan gelen patent maddeleri sayesinde katkıcılar korunur.

Anti-tivoization. Donanım kısıtlamalarıyla özgürlüğün engellenmesi yasak.

Tek geliştirici için sürdürülebilir. Kod ortada kalır; geliştirici bıraksa bile topluluk devam ettirebilir.

Ticari kullanım serbest. Fotoğrafçılar albüm yapıp para kazanabilir; sadece yazılımın kendisini kapalı kaynak yapamazlar.

"or-later" eki: Kullanıcı, GPL 3.0 veya sonrası herhangi bir sürümü seçebilir. Gelecekte GPL 4.0 çıkarsa uyum kolaylaşır.

4.3 Lisans dosyaları
Depoda bulunacaklar:

LICENSE — GPL-3.0 tam metni (İngilizce)

LICENSE.tr.md — GPL-3.0 Türkçe özet (hukuki bağlayıcı değil, bilgi amaçlı)

COPYING — LICENSE'a kısa yönlendirme (isteğe bağlı)

Her kaynak dosyanın başında SPDX kimliği:

text
// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2025 NexAlbum Katkıcıları
4.4 Telif hakkı
Telif, katkıcılarda kalır (ayrıca devredilmez).

"Katkıcılar" ifadesi tüm katkı yapanları kapsar.

Büyük katkılarda katkıcı adı AUTHORS.md dosyasına eklenir.

Katkı gönderen, GPL-3.0+ koşullarıyla uyumlu olduğunu kabul eder (CONTRIBUTING.md'de belirtilir).

Ayrıca katkıcıdan DCO (Developer Certificate of Origin) imzası istenir; karmaşık CLA (Contributor License Agreement) istenmez.

5. Depo Yapısı
5.1 Ana depo
text
nexalbum/
├── .github/
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   ├── feature_request.md
│   │   └── config.yml
│   ├── PULL_REQUEST_TEMPLATE.md
│   ├── workflows/
│   │   ├── build.yml
│   │   ├── test.yml
│   │   ├── release.yml
│   │   └── codeql.yml
│   └── FUNDING.yml
├── docs/
│   ├── kararlar/
│   ├── mimari/
│   ├── kullanici-kilavuzu/
│   └── gelistirici-kilavuzu/
├── src/
│   ├── NexAlbum.Core/
│   ├── NexAlbum.Rendering/
│   ├── NexAlbum.Assets/
│   ├── NexAlbum.Preflight/
│   ├── NexAlbum.Export/
│   ├── NexAlbum.UI/
│   └── NexAlbum.App/
├── tests/
│   ├── NexAlbum.Core.Tests/
│   ├── NexAlbum.Rendering.Tests/
│   └── NexAlbum.Integration.Tests/
├── templates/
├── profiles/
├── samples/
├── scripts/
│   ├── build.ps1
│   ├── package.ps1
│   └── verify-licenses.ps1
├── .editorconfig
├── .gitattributes
├── .gitignore
├── CHANGELOG.md
├── CODE_OF_CONDUCT.md
├── CONTRIBUTING.md
├── AUTHORS.md
├── GOVERNANCE.md
├── ROADMAP.md
├── SECURITY.md
├── SUPPORT.md
├── README.md
├── README.tr.md
├── LICENSE
└── NexAlbum.sln
5.2 Yardımcı depolar (ileride)
nexalbum-templates — topluluk şablonları

nexalbum-profiles — topluluk baskı profilleri

nexalbum-website — tanıtım sitesi (GitHub Pages)

İlke: Ana depo minimal ve derlenebilir kalır. Büyük varlıklar ayrı depolarda tutulur.

6. Katkı Modeli
6.1 Katkı türleri
Tür	Örnek	Süreç
Hata bildirimi	Issue	Şablon doldurulur
Özellik önerisi	Issue + tartışma	Yol haritasıyla karşılaştırılır
Kod katkısı	Pull request	İnceleme + test + birleştirme
Çeviri	PR veya ayrı repo	Dil bazlı inceleme
Şablon	PR (JSON)	Şema doğrulaması
Baskı profili	PR (JSON)	Kaynak doğrulaması
Dokümantasyon	PR	Dil + doğruluk incelemesi
Test	PR	Kapsam artışı
6.2 Katkı akışı
Kullanıcı issue açar veya mevcut issue'yu seçer.

Katkı vermek istediğini belirtir.

Fork + branch açar.

Değişikliği yapar, testleri çalıştırır.

PR açar, şablonu doldurur.

CI (GitHub Actions) otomatik çalışır: derleme, test, lisans kontrolü, kod stili.

Geliştirici inceler.

Gerekirse değişiklik istenir.

Onaylanırsa main'e birleştirilir.

Katkıcı AUTHORS.md'ye eklenir (büyük katkılarda).

6.3 Katkı kuralları
Küçük ve odaklı PR'lar tercih edilir. Tek PR = tek konu.

Test şartı. Yeni özellik = yeni test. Hata düzeltmesi = regresyon testi.

Kod stili. .editorconfig ve biçimlendiriciye uyum zorunlu.

Commit mesajı. Anlamlı, İngilizce, tercihen Conventional Commits (feat:, fix:, docs: ...).

DCO imzası. git commit -s ile.

Lisans uyumu. Yeni bağımlılık eklenirse lisansı kontrol edilir.

Geriye uyumluluk. Proje dosyası şeması bozulamaz; migration zorunlu.

Davranış kuralları. CODE_OF_CONDUCT.md'ye uyum zorunlu.

6.4 İnceleme öncelikleri
Doğruluk. İşlevi doğru mu?

Test. Yeterince test edilmiş mi?

Basitlik. Gereksiz karmaşıklık var mı?

Tutarlılık. Mevcut kodla uyumlu mu?

Performans. Belirgin yavaşlama var mı?

Güvenlik. Risk var mı?

Belgeleme. Gerekli dokümantasyon güncellendi mi?

7. Bağımlılık ve Lisans Uyumu
7.1 İlke
Ana ürün GPL-3.0+ olduğu için, kullanılan tüm bağımlılıklar GPL uyumlu olmalıdır.

7.2 İzin verilen lisanslar
MIT

BSD (2/3-clause)

Apache 2.0

ISC

Zlib

LGPL (dinamik bağlantı ile)

GPL / AGPL (uyumlu)

Public domain / CC0

7.3 Yasak lisanslar
Kapalı kaynak / ticari lisans (örneğin bazı ticari PDF kütüphaneleri)

CC-BY-NC (ticari kullanım yasak)

Bilinmeyen / özel lisans

"Free for non-commercial" gibi kısıtlı lisanslar

7.4 Örnek bağımlılık listesi (öngörü)
Kütüphane	Lisans	Uygun mu?
.NET 8 Runtime	MIT	✅
SkiaSharp	MIT	✅
ImageSharp	Apache 2.0 (v3) / Six Labors Split (v2)	⚠️ Lisansa dikkat
QuestPDF	MIT (topluluk) / ticari (gelir > 1M$)	⚠️ Ücretsiz kullanım koşulu kontrol edilmeli
PDFsharp	MIT	✅
Magick.NET	Apache 2.0	✅
SQLite	Public domain	✅
LiteDB	MIT	✅
Avalonia UI	MIT	✅
WPF (.NET)	MIT	✅
Serilog	Apache 2.0	✅
Not: QuestPDF'in lisans modeli gelir bazlıdır. Ücretsiz açık kaynak proje olduğumuz için topluluk lisansı yeterlidir; ancak bu koşul kullanıcılar için de geçerli mi netleştirilmelidir. Alternatif: PDFsharp + manuel çizim.

7.5 Lisans kontrol otomasyonu
scripts/verify-licenses.ps1 her PR'da çalışır.

dotnet list package --include-transitive çıktısı taranır.

Bilinmeyen lisans → CI başarısız.

Bağımlılık listesi THIRD_PARTY_NOTICES.md dosyasında tutulur.

Her bağımlılık için: ad, sürüm, lisans, kaynak URL.

8. Dokümantasyon Stratejisi
8.1 Zorunlu dosyalar
README.md (İngilizce) — proje tanıtımı, kurulum, hızlı başlangıç

README.tr.md (Türkçe) — ana kullanıcı kitlesi için

CONTRIBUTING.md — katkı rehberi

CODE_OF_CONDUCT.md — davranış kuralları (Contributor Covenant 2.1)

GOVERNANCE.md — karar alma süreci

SECURITY.md — güvenlik açığı bildirimi

SUPPORT.md — destek kanalları

ROADMAP.md — yol haritası

CHANGELOG.md — sürüm notları (Keep a Changelog formatı)

AUTHORS.md — katkıcılar

THIRD_PARTY_NOTICES.md — bağımlılık lisansları

8.2 Dokümantasyon dili
Kod ve teknik doküman: İngilizce (uluslararası katkı için)

Kullanıcı dokümanı: Türkçe (öncelik) + İngilizce

Commit mesajları: İngilizce

Issue/PR: Türkçe veya İngilizce (katkıcının tercihi)

Kod içi yorumlar: İngilizce

Değişken/fonksiyon isimleri: İngilizce

8.3 Dokümantasyon araçları
Markdown (GitHub uyumlu)

Diyagramlar için Mermaid (metin tabanlı, versiyonlanabilir)

API dokümantasyonu için XML doc comment + DocFX (ileride)

Kullanıcı kılavuzu için MkDocs (ileride)

9. Marka ve Kimlik
9.1 Kod vs marka ayrımı
Kod: GPL-3.0+ ile özgür.

Marka: "NexAlbum" adı ve logosu ayrı korunur.

9.2 Marka kuralları
Çatal (fork) yapan, kodu değiştirebilir ama:

Ürün adını değiştirmelidir (örneğin "Album Studio" gibi).

Logoyu kullanmamalıdır.

"Resmi değildir" ibaresi eklemelidir.

Resmi olmayan sürümler, kullanıcıyı yanıltmamalıdır.

Marka kullanımı için ayrı bir TRADEMARK.md dosyası tutulur.

9.3 Logo ve görsel kimlik
Logo lisansı: Kod ile aynı (GPL) ama marka kullanımı kısıtlı.

Renk paleti, tipografi docs/kimlik/ altında.

Ekran görüntüleri CC-BY-4.0 ile paylaşılabilir.

10. Güvenlik ve Sorumlu Açıklama
10.1 SECURITY.md içeriği
Güvenlik açığı nasıl bildirilir? (özel e-posta veya GitHub Security Advisory)

Yanıt süresi hedefi: 72 saat içinde ilk yanıt.

Düzeltme süresi: önem derecesine göre 7-30 gün.

Açıklama politikası: düzeltme yayınlandıktan sonra kamuya açıklanır.

Kapsam: uygulama, bağımlılıklar, dağıtım.

Kapsam dışı: üçüncü taraf eklentiler, kullanıcı yapılandırması.

10.2 Otomatik güvenlik
GitHub Dependabot aktif.

CodeQL analizi her PR'da.

Bağımlılık güncellemeleri otomatik PR.

Sır taraması (secret scanning) aktif.

11. Sürüm ve Yayın Stratejisi
11.1 Sürüm numaralandırma
Semantic Versioning (SemVer): MAJOR.MINOR.PATCH

MAJOR: Geriye uyumsuz değişiklik (proje dosyası şeması bozulması)

MINOR: Yeni özellik (geriye uyumlu)

PATCH: Hata düzeltmesi

11.2 Sürüm kanalları
Stable: Kararlı sürümler (v1.0.0 gibi)

Beta: Test sürümleri (v1.1.0-beta.1)

Nightly: Geliştirme sürümleri (main'den otomatik)

11.3 Yayın süreci
CHANGELOG.md güncellenir.

Sürüm numarası artırılır.

Git tag atılır (v1.0.0).

GitHub Actions otomatik derler ve yayınlar.

Kurulum dosyası + taşınabilir ZIP eklenir.

Sürüm notları yayınlanır.

Duyuru: GitHub Releases + Discussions.

11.4 Yayın kanalları
GitHub Releases (birincil)

winget (Windows paket yöneticisi) — ileride

Chocolatey — ileride

Scoop — ileride

12. Yönetişim (Governance)
12.1 Mevcut model (tek geliştirici)
Benevolent Dictator For Now (BDFN): Tek geliştirici son kararı verir.

Katkılar dinlenir, tartışılır ama son söz geliştiricide.

Şeffaflık: kararlar docs/kararlar/ altında dokümante edilir.

12.2 Gelecek model (topluluk büyürse)
Maintainer ekibi: Düzenli katkı verenler arasından seçilir.

Alt-proje sorumluları: Şablonlar, profiller, çeviriler için ayrı sorumlular.

RFC süreci: Büyük değişiklikler için öneri dokümanı.

Oylama: Basit çoğunluk, maintainer'lar arasında.

12.3 Karar dokümanları
Bu belge gibi karar dokümanları docs/kararlar/ altında tutulur.

Her karar numaralıdır ve durumu vardır: Taslak, Kesinleşti, Geçersiz, Yerine yenisi geldi.

Kararlar geriye dönük değiştirilebilir; eski karar silinmez, "geçersiz" işaretlenir.

13. Topluluk Kanalları
13.1 Birincil kanallar
GitHub Issues: Hata bildirimi, özellik önerisi

GitHub Discussions: Sorular, fikirler, genel tartışma

GitHub Pull Requests: Kod katkısı

13.2 İkincil kanallar (ileride)
Discord veya Matrix sunucusu

Aylık topluluk çağrısı (isteğe bağlı)

Blog / devlog (GitHub Pages)

13.3 Dil politikası
Kanallarda Türkçe ve İngilizce serbest.

Resmi dokümanlar İngilizce (ana) + Türkçe (çeviri).

Moderatörler her iki dilde de yanıt verebilir.

14. Riskler ve Önlemler
Risk	Olasılık	Etki	Önlem
Katkı gelmez	Yüksek	Orta	Küçük başlangıç görevleri etiketlenir (good first issue)
Katkı kalitesi düşük	Orta	Orta	Net CONTRIBUTING, CI kontrolü
Lisans ihlali (bağımlılık)	Orta	Yüksek	Otomatik lisans taraması
Kapalı kaynak türev iddiası	Düşük	Yüksek	GPL net; hukuki danışma gerekirse
Marka kötüye kullanımı	Düşük	Orta	TRADEMARK.md; nazik uyarı
Bakımsız kalma	Orta	Yüksek	Küçük MVP; topluluk devralabilir
Çatal kaosu	Düşük	Düşük	GPL zaten buna izin verir; sorun değil
Güvenlik açığı	Orta	Yüksek	SECURITY.md; Dependabot; CodeQL
Zararlı katkı (malware)	Düşük	Yüksek	İnceleme zorunlu; CI; imzalı sürümler
Dil/çeviri tutarsızlığı	Orta	Düşük	Çeviri rehberi; terim sözlüğü
15. Başarı Metrikleri
Katkıcı sayısı (en az 5 düzenli katkıcı ilk yılda hedef)

PR sayısı (yıllık)

Issue kapanma oranı (%70+ hedef)

Yıldız sayısı (GitHub — gösterge, hedef değil)

Çatal sayısı (topluluk ilgisi göstergesi)

Çeviri kapsamı (en az 2 dil)

Bağımlılık lisans uyumu (%100)

Güvenlik açığı yanıt süresi (< 72 saat ilk yanıt)

16. Kararın Diğer Kararlarla İlişkisi
Karar	İlişki
01 — Ücretsiz	Açık kaynak, ücretsiz kararını güçlendirir ve tutarlı kılar.
03 — Kullanıcı Profilleri	Profiller açık formatta (JSON); topluluk katkısına açık.
04 — Tek Kişi	Tek geliştirici için açık kaynak, sürdürülebilirlik sigortasıdır.
05 — Zaman	Açık kaynak, zaman baskısını hafifletir; topluluk yardım eder.
06 — Bütçe	Ticari kütüphanelerden kaçınılır; GPL uyumlu ücretsiz alternatifler seçilir.
07 — Rakip Özellikleri	Açık kaynak, "en iyi özellikleri bir araya getirme" stratejisini meşrulaştırır.
17. Açık Sorular (Sonraki Kararlara Bırakılan)
CLA mı, DCO mu? → Karar: DCO (bu dokümanda belirtildi)

Marka tescili yapılacak mı? → Karar 06 (bütçe)

Ayrı topluluk deposu ne zaman açılacak? → Karar 04 (zaman)

Çeviri altyapısı nasıl olacak (Crowdin, Weblate, manuel)? → Karar 04

Kod imzalama sertifikası alınacak mı? → Karar 06

18. Özet
NexAlbum GPL-3.0-or-later lisansıyla açık kaynak bir üründür. Bu karar:

Ürünün kimliğini tanımlar (topluluğa ait).

Kodun açık kalmasını garanti eder.

Kapalı kaynak türevleri engeller.

Ticari kullanımı serbest bırakır (yazılımı satmak hariç).

Katkıyı teşvik eder.

Tek geliştirici için sürdürülebilirlik sigortasıdır.

Ücretsiz kararıyla tutarlıdır.

Bu karar, ürünün kurucu ilkelerinden biridir ve README'de kullanıcıya açıkça duyurulur. Değiştirilmesi (örneğin MIT'ye geçiş) ürünün ruhunu zedeler; mevcut niyet bu kararın kalıcı olduğudur.

Onay: Bu karar, ürünün kurucu ilkelerinden biridir.