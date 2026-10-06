# Caspian Bank: API

Caspian Bank layihəsinin backend hissəsi (ASP.NET Core 8 Web API, N-Tier: `Domain`, `Repository`, `Service`, API).
UI ayrı layihədir: **CaspianBank-MVC-FinalProject** (bu API-ni istehlak edir).

## Tələblər
- Visual Studio 2022 (ASP.NET and web development iş yükü) və **.NET 8 SDK**
- SQL Server Express və ya LocalDB (Visual Studio ilə gəlir)

## Tez başlanğıc
Heç bir əlavə ayar və ya kod yazmaq lazım deyil.

1. **Baza:** təqdim olunan baza SQL Server-ə import edilir (`CaspianBankDataBase` adı ilə).
   - Server adı kompüterdən-kompüterə fərqli olur. API işə düşəndə `.\SQLEXPRESS`, `(localdb)\MSSQLLocalDB`,
     `localhost` və s. ünvanları **avtomatik yoxlayıb** işləyəni seçir.
   - Baza import olunmayıbsa da problem yoxdur: API başlayanda bazanı və bütün cədvəlləri özü yaradır
     (migration-lar avtomatik tətbiq olunur), rollar avtomatik əlavə olunur.
2. Solution-u Visual Studio-da açın, `CaspianBank-API-FinalProject` layihəsini **`https`** profili ilə işə salın
   (API: `https://localhost:7293`, Swagger açılır).
3. UI üçün **CaspianBank-MVC-FinalProject**-i ayrıca açıb **`https`** profili ilə işə salın
   (`https://localhost:7082`). **API əvvəlcədən işləməlidir.**

## Qeydiyyat və email (demo rejimi)
Qeydiyyat email təsdiqi (OTP) tələb edir. Email serveri (SMTP) qurulmayıbsa sistem **demo rejimində** işləyir:
OTP kodu və şifrə bərpası linki **birbaşa səhifədə göstərilir**, emailə baxmağa ehtiyac yoxdur.

Axın: `Welcome` (email) → mövcuddursa `Login`; yenidirsə kod təsdiqi → `Register` → `Login`.

### Real email göndərmək (istəyə bağlı)
Gmail App Password ilə (parol Git-ə düşməsin deyə `user-secrets`-də saxlanır), API qovluğunda:
```powershell
dotnet user-secrets set "Smtp:UserName" "sizin.gmail@gmail.com"
dotnet user-secrets set "Smtp:Password" "16-herfli-app-password"
dotnet user-secrets set "Smtp:FromAddress" "sizin.gmail@gmail.com"
```
Üçü də təyin olunanda email avtomatik MailKit ilə göndərilir və demo rejimi söndürülür.

## Əsas texnologiyalar və təhlükəsizlik
- ASP.NET Core Identity, **JWT** (Bearer), rollar (`Customer`, `SuperAdmin`, `Admin`, `Accountant`, `WebDesigner`, `CustomerSupport`, `Security`)
- Email OTP (kod bazada **HMAC hash** ilə saxlanır, 5 dəqiqə, 5 cəhd limiti, yenidən göndərmə gözləməsi)
- Rate limiting (auth endpoint-lərində IP başına dəqiqədə 10 sorğu), lockout (5 uğursuz giriş → 5 dəqiqə blok)
- Şifrə bərpası (Identity token, 30 dəqiqə, bir dəfəlik), FluentValidation, MailKit
- `Jwt:Key` yalnız lokal işləmə üçün `appsettings.json`-da hazır verilib; real mühitdə `user-secrets` və ya mühit dəyişəni ilə dəyişdirilməlidir.

## Əsas endpoint-lər
`POST /api/account/check-email`, `send-otp`, `verify-otp`, `register`, `login`, `forgot-password`, `reset-password`,
`GET /api/account/me` (JWT tələb edir), `GET /api/home/tickers|brands|about|pillars`.
Swagger-də **Authorize** düyməsi ilə login-dən alınan token yapışdırılır.
