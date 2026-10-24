# 01. Böyük şəkil

## 1. Layihə nədir?

Caspian Bank: onlayn bank simulyasiyasıdır. Müştəri:
- qeydiyyatdan keçir (email OTP kodu ilə təsdiq),
- kart açır (ilk kartlar pulsuz, əlavə kartlar ödənişli),
- balans artırır (xarici kartdan simulyasiya),
- pul köçürür (öz kartlarına və ya başqa müştərinin kartına),
- əməliyyat tarixçəsinə baxır və kart çıxarışı alır.

İşçilər (admin, web dizayner, mühasib və s.) admin paneldən Home səhifəsinin məzmununu, kart dizaynlarını, kart növü qaydalarını idarə edir.

## 2. İki ayrı layihə: niyə?

| | API | MVC |
|---|---|---|
| Rolu | Məlumat və qaydalar | Səhifələr |
| Qaytarır | JSON | HTML |
| Bazaya çıxışı | Var (EF Core) | Yoxdur |
| Giriş saxlama | JWT yaradır və yoxlayır | JWT-ni cookie içində saxlayır |
| Kimə müraciət | Kimsə (Swagger, MVC, telefon tətbiqi) | Yalnız API |

**Nəzəriyyə: niyə ayırırlar?** Eyni "bank beyni"ni gələcəkdə başqa müştəri də (mobil tətbiq) istifadə edə bilər. Qaydalar bir yerdədir, ona görə təhlükəsizlik yoxlamaları da bir yerdədir. MVC sadəcə "göstərən" təbəqədir.

**Bizim layihədə:**
- API: `C:\Users\User\Desktop\CaspianBank-API-FinalProject` (SQL Server bazası: `CaspianBankDataBase`)
- MVC: `C:\Users\User\Desktop\CaspianBank-MVC-FinalProject\CaspianBank-MVC-FinalProject`
- Dizayn mənbəyi (statik HTML): `C:\Users\User\Desktop\CaspianBank-Frontend`

## 3. N-Tier (qatlı) arxitektura

API-nin daxilində kod **dörd layihəyə** bölünüb. Hər qat yalnız altındakı qatı tanıyır:

```
CaspianBank-API-FinalProject   (Controller-lər, Program.cs, Middleware)   <- HTTP dünyası
        │
        ▼
Service                        (qaydalar, DTO, validator, JWT/OTP məntiqi)  <- "bank beyni"
        │
        ▼
Repository                     (EF Core, DbContext, sorğular, tranzaksiyalar) <- bazaya çıxış
        │
        ▼
Domain                         (Entity-lər, Enum-lar, Konfiqurasiyalar)       <- "nə saxlanır"
```

**Qayda:** Service → Repository → Domain. Heç vaxt əksinə yox. Controller Repository-ni birbaşa çağırmır, Repository Service-i tanımır.

**Nə üçün?**
- **Dəyişmək asandır**: bazanı başqa texnologiya ilə əvəz etsən yalnız Repository dəyişir.
- **Test asandır**: Service-i test edəndə həqiqi bazaya ehtiyac yoxdur (interfeys saxta ilə əvəz olunur).
- **Məsuliyyət aydındır**: "komissiya necə hesablanır" sualının cavabı yalnız Service-də axtarılır.

### Hər qatda nə yazılır

| Qat | Nə yazılır | Nümunə fayl |
|---|---|---|
| Domain | Cədvəl sətirinin forması (`Card`, `Transaction`), enum (`CardTier`), EF konfiqurasiyası | `Domain/Entities/Card.cs` |
| Repository | `AppDbContext`, sorğular, `SaveChanges`, tranzaksiya | `Repository/Repositories/CardRepository.cs` |
| Service | Qaydalar, yoxlama, DTO ↔ Entity çevirməsi, xarici şeylər (email, token) | `Service/Services/TransferService.cs` |
| API | HTTP: route, `[Authorize]`, status kodu | `CaspianBank-API-FinalProject/Controllers/Client/TransfersController.cs` |

## 4. Bir sorğunun yolu (ən vacib anlayış)

Nümunə: müştəri **"Transfer"** düyməsinə basır.

```
1. Brauzer ──POST /App/Transfer/Confirm──> MVC TransferController.Confirm
2. MVC BearerTokenHandler cookie-dən JWT götürür, sorğuya "Authorization: Bearer ..." əlavə edir
3. MVC ──POST /api/transfers (JSON)──> API TransfersController.Transfer
4. API: rate limiter (dəqiqədə 20) ──> JWT yoxlanır ──> [Authorize] keçir
5. Controller: UserId tokendən götürülür (sorğudan yox!) ──> TransferService.TransferAsync(UserId, model)
6. TransferService: yoxlamalar (validator, kart sahibi, bloklu?, limit, komissiya)
7. TransferService ──> TransferRepository.ExecuteAsync(plan)
8. Repository: SQL tranzaksiya: göndərən balansı azalır, alanın artır, 3 Transaction sətri yazılır
9. Nəticə yuxarı qalxır: Repository → Service (ServiceResult) → Controller (Ok/BadRequest) → MVC → brauzer
```

**Xəta olarsa:** Service `BadRequestException` atır → `ExceptionHandlingMiddleware` onu tutur → `{isSuccess:false, errors:[...]}` JSON-u 400 ilə qaytarır → MVC `ApiControllerBase.ReadErrorsAsync` oxuyur → səhifədə göstərilir.

Bu fikir hər funksiyada təkrarlanır. Başa düşəndən sonra yeni funksiya yazmaq "doldurma işi"dir.

## 5. Əsas texnologiyalar və nə üçün

| Texnologiya | Nə edir | Harada |
|---|---|---|
| ASP.NET Core 8 Web API | HTTP endpoint-lər | API layihəsi |
| ASP.NET Core 8 MVC | Razor səhifələr | MVC layihəsi |
| Entity Framework Core 8 | C# obyektləri ↔ SQL cədvəlləri | Repository |
| SQL Server | Verilənlər bazası | `DESKTOP-B24N2SA\SQLEXPRESS` |
| ASP.NET Identity | İstifadəçi, parol hash-i, rollar, lockout | `AppUser`, `AccountRepository` |
| JWT (JSON Web Token) | "Mən kiməm" sübutu | `TokenService`, `Program.cs` |
| FluentValidation | Daxil olan məlumatın yoxlanması | `Service/Helpers/Validators` |
| MailKit | Email göndərmə (SMTP) | `MailKitEmailSender` |
| Rate limiting | Spam/brute-force müdafiəsi | `Program.cs`, `[EnableRateLimiting]` |
| Swagger | API-ni brauzerdə sınamaq | Yalnız Development |

## 6. Konfiqurasiya fayllarının rolu

- `appsettings.json`: ümumi ayarlar. `Jwt` (açar, issuer, audience, müddət), `Smtp`, `Frontend:BaseUrl`, `SeedAdmins` (boşdur).
- `appsettings.Development.json`: Development ayarları (məs. OTP kodunu cavabda göstərmək).
- `appsettings.Local.json`: yalnız bu kompüterdə, `.gitignore`-dadır. İşçi hesablarının parolları burada.
- MVC `appsettings.json`: `ApiSettings:BaseUrl`: API ünvanı.

> Qeyd: `Jwt:Key` hazır açar kimi `appsettings.json`-dadır ki, layihə müəllimin kompüterində heç nə qurmadan işləsin. Real mühitdə bu açar gizli saxlanmalıdır (user-secrets, mühit dəyişəni).

## 7. Başlanğıcda nə baş verir? (`Program.cs` axını)

1. `appsettings.Local.json` əlavə oxunur (varsa).
2. Servislər qeydiyyatdan keçir (DI konteyneri): DbContext, Identity, JWT, rate limiter, Service və Repository layihələri.
3. Bağlantı sətri tapılır (`ConnectionStringResolver`: başqa kompüterdə SQL Server adı fərqli ola bilər).
4. Tətbiq qurulur (`Build()`), sonra **bir dəfə**: `Database.MigrateAsync()` (bazanı yaradır/yeniləyir), rolları yaradır (`RoleSeeder`), işçi hesablarını yaradır (`AdminSeeder`).
5. Middleware zənciri qurulur (sıra vacibdir, bax `06-api-controllers-middleware.md`).
6. `app.Run()`: server sorğu gözləyir.

## Özünü yoxla

1. Niyə MVC bazaya birbaşa qoşulmur?
2. N-Tier-də hansı qat hansını tanıyır? Controller niyə Repository-ni çağırmır?
3. "Transfer" düyməsinə basılandan bazaya yazılana qədər sorğu hansı 5 yerdən keçir?
4. UserId niyə sorğudan yox, tokendən götürülür?
5. Xəta atılanda cavab hansı formatda qayıdır və kim bunu hazırlayır?
