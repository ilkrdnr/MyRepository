# FRP Helper

FRP dosyalarini parse eden, SQL ve Pascal kodlarini Monaco Editor ile duzenleyen, Supabase tabanli paylasimli havuz uygulamasi.

## Ozellikler

- Supabase ile Login / Register / Forgot Password / Remember Me
- FRP/FRX/XML/ZIP/RAR yukleme
- SQL ve Pascal script bloklarini duzenleme
- Duzenlenen raporu `.frp` olarak indirme
- Ortak havuza buton ile erisim
- Kullanici yetki semasi: havuz goruntuleme, yukleme, rapor duzenleme
- Admin paneli ile kullanici yetkisi yonetimi
- Havuzda liste gorunumu: GUID, yukleyen, zaman bilgileri, admin icin guncelle/sil
- GitHub Pages ile otomatik yayinlama ve custom domain destegi

## Yerelde Calistirma

1. .NET 9 SDK kurulu olmali.
2. Proje kokunde komut calistirin:

```powershell
dotnet run --project .\FrpHelper.Web\FrpHelper.Web.csproj
```

3. Tarayicidan uygulamanin yazdigi URL'e gidin.

## Supabase Hazirligi

1. Supabase SQL Editor icinde [docs/supabase-schema.sql](docs/supabase-schema.sql) dosyasini calistirin.
2. Ilk yoneticiyi tanimlamak icin SQL Editor'de asagidaki sorguyu kendi kullanici ID'niz ile bir kez calistirin:

```sql
update public.user_permissions
set is_admin = true
where user_id = '<KENDI-USER-ID>';
```

3. [FrpHelper.Web/wwwroot/appsettings.json](FrpHelper.Web/wwwroot/appsettings.json) dosyasindaki Supabase alanlarini kontrol edin.

## GitHub Pages ve Domain

Workflow: [.github/workflows/deploy-pages.yml](.github/workflows/deploy-pages.yml)

1. `Settings > Pages > Source` kismini `GitHub Actions` yapin.
2. `main` branch'e push edin.
3. Proje URL'i varsayilan olarak su formatta acilir:

```text
https://<kullanici>.github.io/<repo>/
```

4. Custom domain kullanmak icin repository variable ekleyin:
	- `Settings > Secrets and variables > Actions > Variables`
	- `CUSTOM_DOMAIN` adinda bir variable olusturun (or: `frphelper.ornek.com`)
5. Domain DNS tarafinda CNAME kaydini `kullanici.github.io` hedefine yonlendirin.
