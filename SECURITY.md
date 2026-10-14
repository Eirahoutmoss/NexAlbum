# Güvenlik politikası

## Desteklenen sürümler

NexAlbum erken geliştirme aşamasındadır; güvenlik düzeltmeleri yalnızca `main` dalına yapılır.

## Açık bildirme

Bir güvenlik açığı bulduysanız **lütfen herkese açık issue açmayın.** GitHub'ın
[özel güvenlik bildirimi](https://github.com/Eirahoutmoss/NexAlbum/security/advisories/new) özelliğini kullanın.

Bildirimde açığın türü, etkilenen dosya/bileşen, yeniden üretme adımları ve varsa örnek dosya (ör. hazırlanmış bir `.albumproj`
ya da `.nexpaket`) yer alsın. 72 saat içinde yanıt vermeyi hedefliyoruz.

## Kapsam ve tasarım ilkeleri

- `.albumproj` ve `.nexpaket` dosyaları güvenilmeyen girdi sayılır: yol aşımı, ZIP bombası ve JSON derinliğine karşı sınırlar vardır.
- Eklenti paketleri yalnızca veri taşır; kod, betik ya da DLL yüklenmez.
- HTML prova dış kaynak yüklemez ve hiçbir yere veri göndermez.
- Uygulama telemetri göndermez; yalnızca kullanıcı izin verdiğinde FFmpeg indirilir.
