# FRP Helper

FRP dosyalarini parse eden, SQL ve Pascal kodlarini Monaco Editor ile duzenleyen, Supabase tabanli paylasimli havuz uygulamasi.

## Ozellikler

- Supabase ile Login / Register
- FRP/FRX/XML/ZIP/RAR yukleme
- SQL ve Pascal script bloklarini basliga tiklayarak acma
- Monaco Editor ile kod duzenleme
- Havuzdaki raporlari listeleme ve indirme
- GitHub Pages ile otomatik yayinlama

## Yerelde Calistirma

1. .NET 9 SDK kurulu olmali.
2. Proje kokunde komut calistirin:

```powershell
dotnet run --project .\FrpHelper.Web\FrpHelper.Web.csproj
```

3. Tarayicidan asagidaki adrese gidin:

```text
http://localhost:5129
```

## Supabase Hazirligi

1. Supabase SQL Editor icinde [docs/supabase-schema.sql](docs/supabase-schema.sql) dosyasini calistirin.
2. [FrpHelper.Web/wwwroot/appsettings.json](FrpHelper.Web/wwwroot/appsettings.json) dosyasindaki Supabase alanlarini kontrol edin.

## GitHub Pages Yayinlama

Bu repoda hazir workflow vardir: [.github/workflows/deploy-pages.yml](.github/workflows/deploy-pages.yml)

1. GitHub repo ayarlarinda `Settings > Pages > Source` kismini `GitHub Actions` secin.
2. `main` branch'e push yapin.
3. Workflow tamamlandiginda uygulama su URL'de acilir:

```text
https://ilkrdnr.github.io/MyRepository/
```
