# 06. API controller-lər, middleware, Program.cs

Layihə: `CaspianBank-API-FinalProject/` (web layihəsi). Bu qat HTTP dünyasıdır: route, status kodu, giriş yoxlaması.

## 1. Nəzəriyyə

### 1.1 Controller nə edir?
**Nazik** olmalıdır. Üç iş:
1. HTTP sorğusundan məlumatı götür (`[FromBody]`, `[FromForm]`, `[FromQuery]`, `[FromRoute]`).
2. Service metodunu çağır.
3. Nəticəni HTTP cavabına çevir (`Ok`, `BadRequest`, `NotFound`, `NoContent`).

Controller-də qayda **olmamalıdır** (komissiya hesabı, kart bloklu yoxlaması və s.). Onlar Service-dədir.

### 1.2 Atributlar
| Atribut | Mənası |
|---|---|
| `[ApiController]` | API davranışı: avtomatik model yoxlaması, binding qaydaları |
| `[Route("api/cards")]` | URL |
| `[HttpGet]`, `[HttpPost("{id:int}/top-up")]` | HTTP metodu + alt yol |
| `[Authorize]` | Giriş (JWT) tələb edir |
| `[Authorize(Roles = "Admin,SuperAdmin")]` | Rol tələb edir (bir rol kifayətdir) |
| `[FromBody]` | JSON gövdədən |
| `[FromForm]` + `[Consumes("multipart/form-data")]` | Forma/fayl yükləmə |
| `[FromQuery]` | URL-dəki `?a=1&b=2` |
| `[EnableRateLimiting("auth")]` | Sürət limiti siyasəti |

### 1.3 HTTP status kodları
`200 OK` uğur · `204 NoContent` uğur, gövdə yox (PUT/DELETE) · `400 BadRequest` yanlış məlumat · `401 Unauthorized` token yoxdur/pozulub · `403 Forbidden` rol uyğun deyil · `404 NotFound` tapılmadı · `429 TooManyRequests` sürət limiti · `500` server xətası.

### 1.4 Middleware zənciri
Middleware: sorğu və cavab arasındakı "filtrlər" ardıcıllığı. Hər biri sorğunu öz işindən keçirir, sonrakına ötürür (`_next(context)`), cavab geri qayıdanda da işini görür. **Sıra çox vacibdir.**

### 1.5 Rate limiting
Müəyyən vaxt ərzində IP-dən gələn sorğu sayını məhdudlaşdırır. Brute-force, spam və təxmin hücumlarına qarşı. Biz **FixedWindow** istifadə edirik: pəncərədə (1 dəq) limit (permit).

## 2. Bizim layihədə

### 2.1 `Program.cs` düzülüşü
Fayl iki hissəyə bölünür: **qeydiyyat** (`builder.Services...`) və **pipeline** (`app.Use...`).

**Qeydiyyat hissəsi:**
1. `AddControllers()`.
2. `AddSwaggerGen` + Bearer təhlükəsizlik tərifi (Swagger-də "Authorize" düyməsi, tokeni yapışdırıb sınamaq üçün).
3. `ConnectionStringResolver.Resolve(...)` (SQL Server-i tapır) → `AddDbContext<AppDbContext>(UseSqlServer)`.
4. `AddServiceLayer()` və `AddRepositoryLayer()` (öz uzantı metodlarımız, DI qeydiyyatları).
5. `AddIdentity<AppUser, IdentityRole>(...)`.
6. `Configure<JwtSettings>`, `Configure<SmtpSettings>`, `Configure<FrontendSettings>`.
7. `Jwt:Key` boşdursa və ya 32 simvoldan qısadırsa **tətbiq dayanır** (təhlükəsizlik: zəif açarla başlamasın).
8. `AddAuthentication().AddJwtBearer(...)` (bax `05`).
9. `ForwardedHeaders`: MVC API-yə server tərəfdən müraciət edir, real istifadəçi IP-si `X-Forwarded-For` ilə gəlir.
10. `AddRateLimiter`: iki siyasət: **`transfer`** (dəqiqədə 20) və **`auth`** (dəqiqədə 10), IP-yə görə. Aşanda `429`.

**Başlanğıc (bir dəfə):** `MigrateAsync()` → `RoleSeeder` → `AdminSeeder`.

**Pipeline sırası** (dəyişməməlidir):
```
UseMiddleware<ExceptionHandlingMiddleware>   // ən başda: sonrakı hər şeydə atılan xətanı tutur
UseForwardedHeaders
UseHttpsRedirection
UseStaticFiles                               // wwwroot (şəkillər, videolar)
UseRateLimiter
UseAuthentication                            // tokeni oxuyur, User-i qurur
UseAuthorization                             // [Authorize]-ı yoxlayır
MapControllers
```
Niyə `ExceptionHandlingMiddleware` ən başdadır? Əgər o ortada olsaydı, ondan əvvəlki middleware-lərdə atılan xəta tutulmazdı.
Niyə `UseAuthentication` `UseAuthorization`-dən əvvəl? Əvvəl "sən kimsən" bilməliyik, sonra "icazən var?" yoxlanır.

### 2.2 `ExceptionHandlingMiddleware`
`CaspianBank-API-FinalProject/Middlewares/ExceptionHandlingMiddleware.cs`
```csharp
try { await _next(context); }
catch (NotFoundException ex)   { await WriteAsync(context, 404, ex.Message); }
catch (BadRequestException ex) { await WriteAsync(context, 400, ex.Errors); }
catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { /* istifadəçi ləğv edib */ }
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled exception: {Method} {Path}", ...);
    await WriteAsync(context, 500, "Something went wrong. Please try again later.");
}
```
Cavab həmişə eyni formatdadır:
```json
{ "isSuccess": false, "errors": ["mesaj 1", "mesaj 2"] }
```
Mühüm: gözlənilməz xətanın **real mətni** istifadəçiyə göstərilmir (təhlükəsizlik), yalnız log-a yazılır.

Bu, kodu sadələşdirir: servis xəta olduqda sadəcə `throw new BadRequestException("...")` yazır. Hər controller-də `try/catch` lazım deyil.

### 2.3 Controller-lərin xəritəsi

**Müştəri (`Controllers/Client`):**

| Controller | Route | Qoruma | Nə üçün |
|---|---|---|---|
| `AccountController` | `api/account` | ictimai (rate limit) | qeydiyyat, login, OTP, şifrə bərpası |
| `HomeController` | `api/home/...` | ictimai | Home məzmunu (brands, tickers, about, services, benefits, cards, cardHero) |
| `SettingsController` | `api/settings` | ictimai | loqo, şirkət adı, ünvan (açar-dəyər lüğəti) |
| `CardTiersController` | `api/card-tiers` | ictimai | kart növü qaydaları (qiymətlər açıqdır) |
| `CardsController` | `api/cards` | `[Authorize]` | kartlar, top-up, bloklama, activity |
| `TransfersController` | `api/transfers` | `[Authorize]` + `transfer` limit | preview, transfer |
| `HistoryController` | `api/history` | `[Authorize]` | tarixçə, çıxarış |

**Admin (`Controllers/Admin`)**, hamısı `api/admin/...`, rollarla qorunur:

| Controller | Route | Rollar |
|---|---|---|
| `BrandController` | `brands` | Admin, SuperAdmin |
| `HomeTickerController` | `tickers` | eyni |
| `CardHeroController` | `card-hero` | eyni |
| `AboutController` | `about` (+ `PUT {id}/video`) | eyni |
| `AboutPillarController` | `about-pillars` | eyni |
| `ServiceSectionController` / `ServiceItemController` | `service-section` / `service-items` | eyni |
| `BenefitSectionController` / `BenefitItemController` | `benefit-section` / `benefit-items` (+ `GET destinations`) | eyni |
| `CardDesignController` | `card-designs` | eyni |
| `SettingController` | `settings` (+ `PUT {id}/logo`) | eyni |
| `CardTierController` | `card-tiers` (+ `GET designs`) | **Accountant**, Admin, SuperAdmin (pul qaydalarıdır) |

### 2.4 Müştəri controller-lərinin ortaq şablonu
```csharp
[Route("api/cards")]
[ApiController]
[Authorize]
public class CardsController : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    ...
    private IActionResult ToResponse<T>(ServiceResult<T> result)
    {
        if (result.IsNotFound) return NotFound(new { isSuccess = false, errors = result.Errors });
        if (!result.IsSuccess) return BadRequest(new { isSuccess = false, errors = result.Errors });
        return Ok(result.Data);
    }
}
```
**Əsas təhlükəsizlik qaydası:** `UserId` **yalnız tokendən** götürülür. Heç bir endpoint "userId" parametri qəbul etmir. Beləliklə, birinin başqasının kartına baxması və ya pul göndərməsi mümkün deyil (sorğuya başqasının id-sini yazsa belə nəzərə alınmır).

`ToResponse<T>` `ServiceResult`-u HTTP-yə çevirir. Eyni funksiya Cards və Transfers controller-lərində təkrarlanır.

### 2.5 Admin controller-lərinin şablonu (Brand)
```csharp
[Route("api/admin/brands")]
[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
public class BrandController : ControllerBase
{
    [HttpGet]            → Ok(list)
    [HttpGet("{id}")]    → Ok(detail)
    [HttpPost] [Consumes("multipart/form-data")] CreateBrand([FromForm] dto)  → Ok()
    [HttpPut("{id}")] [Consumes("multipart/form-data")] UpdateBrand(...)     → NoContent()
    [HttpDelete("{id}")] → NoContent()
}
```
Nəzər yetir: **heç bir `try/catch` və heç bir `BadRequest` yoxdur.** Xəta olsa servis `throw` edir, middleware tutur.

Tək-yazılı modellərdə (About, Hero, BenefitSection, ServiceSection) yalnız `GET` və `PUT {id}` var (Create/Delete yoxdur).

### 2.6 Video yükləmə üçün limit
`AboutController.UpdateAboutVideo`: `[RequestSizeLimit(60 MB)]` və `[RequestFormLimits(MultipartBodyLengthLimit = 60 MB)]`. Çünki ASP.NET-in defolt limiti (~30 MB) 50 MB video üçün azdır. Faylın real limiti (50 MB) `FileService`-də yoxlanılır.

### 2.7 `ConnectionStringResolver`
Başqa kompüterdə SQL Server-in adı fərqli ola bilər. Resolver:
1. `appsettings`-dəki serveri yoxlayır.
2. Tanınmış ünvanları sınayır (`.\SQLEXPRESS`, `(localdb)\MSSQLLocalDB`, `localhost`, `MachineName\SQLEXPRESS` ...).
3. Əvvəl **bazası mövcud** olan serveri axtarır (import olunmuş baza), tapılmasa **serveri** tapır və `MigrateAsync()` bazanı yaradır.
4. Heç nə tapılmasa xəta atmır (ki, `dotnet ef` kimi əmrlər işləyə bilsin), ilk bazaya müraciətdə aydın xəta çıxır.

Bu, layihəni müəllimin kompüterində heç nə dəyişmədən işlətmək üçündür.

## Özünü yoxla

1. Controller-də hansı üç iş olmalı, hansı iş olmamalıdır?
2. `401` və `403` fərqi?
3. Middleware sırası niyə vacibdir? `ExceptionHandlingMiddleware` niyə birincidir?
4. `UserId` niyə tokendən götürülür? Əgər sorğu gövdəsindən götürülsəydi nə olardı?
5. `transfer` və `auth` rate limit siyasətləri hansı endpoint-ləri qoruyur və niyə?
6. Servis `throw new BadRequestException` edəndə cavab necə qayıdır?
7. Video yükləmə üçün niyə xüsusi `RequestSizeLimit` lazımdır?
