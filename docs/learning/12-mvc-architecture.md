# 12. MVC layihəsinin arxitekturası

Repo: `C:\Users\User\Desktop\CaspianBank-MVC-FinalProject\CaspianBank-MVC-FinalProject`

## 1. Nəzəriyyə

### 1.1 MVC nədir?
- **Model**: səhifəyə lazım olan məlumat (bizdə **ViewModel**).
- **View**: HTML şablonu (`.cshtml`, Razor).
- **Controller**: sorğunu qəbul edir, məlumatı hazırlayır, View-ya verir.

Bizdə Model API-dən gəlir. MVC layihəsinin bazaya çıxışı **yoxdur**: o, API-nin müştərisidir.

### 1.2 Razor
`.cshtml` fayllarında HTML içində C# yazılır: `@Model.Name`, `@foreach (...)`, `@if (...)`. Tag Helper-lər (`asp-controller`, `asp-action`, `asp-route-id`, `asp-area`) linkləri və formaları marşrutdan avtomatik qurur.

### 1.3 Layout, partial, ViewComponent
- **Layout** (`_AppLayout.cshtml`): səhifənin çərçivəsi (header, footer). `@RenderBody()` səhifənin öz məzmunudur. `@RenderSection("Scripts", required: false)` səhifəyə xüsusi JS yeri.
- **Partial** (`_ActivityRow.cshtml`): təkrar istifadə olunan HTML parçası, yalnız View.
- **ViewComponent**: partial + öz məntiqi (controller kimi). Bizdə hər biri API-dən məlumat çəkir (`BrandViewComponent` → `api/home/brands`). Controller-ə toxunmadan səhifənin bir hissəsini müstəqil doldurur.

### 1.4 ViewModel nə üçündür?
View-ya lazım olan **dəqiq** məlumat paketi. API-nin cavabı ilə UI-nin ehtiyacı eyni olmaya bilər (formatlama, əlavə sahələr). Eyni zamanda formadan gələn məlumatın yoxlama qaydaları (`[Required]`, `[StringLength]`) ViewModel-dədir.

Adlandırma qaydası bu layihədə:
- `XUIVM`: ictimai səhifə üçün (Home-da göstərilən).
- `XVM`: admin siyahısı.
- `XDetailVM`: admin detal.
- `XCreateVM`, `XEditVM`: formalar.

### 1.5 TempData və PRG
- **TempData**: növbəti sorğuya qədər yaşayan qısa yaddaş (cookie-də). Mesaj göstərmək üçün: `TempData["Success"] = "..."`.
- **PRG (Post → Redirect → Get)**: POST-dan sonra birbaşa View qaytarmırıq, redirect edirik. Yoxsa istifadəçi F5 basanda forma təkrar göndərilir (məsələn pul iki dəfə köçərdi).

### 1.6 `[ValidateAntiForgeryToken]` (CSRF)
Hər `POST` formasında gizli token olur (Tag Helper əlavə edir). Server yoxlayır ki, sorğu **bizim saytın formasından** gəlib; başqa sayt istifadəçinin adından gizli forma göndərə bilməsin.

## 2. Bizim layihədə

### 2.1 `Program.cs`
1. `AddControllersWithViews()`.
2. `AddHttpContextAccessor()` + `BearerTokenHandler` (Transient).
3. **`AddHttpClient("CaspianApi", ...)`**: API üçün adlandırılmış `HttpClient`. `BaseAddress = ApiSettings:BaseUrl`. `.AddHttpMessageHandler<BearerTokenHandler>()`: hər sorğuya token əlavə edir. Development-də yerli (loopback) ünvan üçün sertifikat yoxlaması keçilir (dev sertifikatı etibarlı olmaya bilər).
4. **Cookie authentication**: `Caspian.Auth` cookie-si, `HttpOnly`, `SameSite=Lax`, yalnız HTTPS (Development-də HTTP də olar). `LoginPath = /Account/Welcome`, `AccessDeniedPath = /Home/AccessDenied`. `SlidingExpiration = false` (cookie JWT ilə eyni vaxtda bitir).
5. Pipeline: `UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}")` (404/403 xəta səhifəsi), `UseHttpsRedirection`, `UseStaticFiles` (Development-də no-cache), `UseRouting`, `UseAuthentication`, `UseAuthorization`.
6. **Marşrutlar** (sıra vacibdir):
   - `areas`: `{area:exists}/{controller=BankDashboard}/{action=Index}/{id?}`: admin (`/Admin/...`)
   - `default`: `{controller=Home}/{action=Index}/{id?}`

### 2.2 `ApiControllerBase`: ən vacib köməkçi
`Controllers/ApiControllerBase.cs`: giriş tələb edən bütün səhifələrin ortaq əcdadıdır.

| Üzv | Nə edir |
|---|---|
| `Api` | adlandırılmış `HttpClient` ("CaspianApi") |
| `GetAsync<T>(url)` | GET; uğurda `(data, status)`; xətada `(null, status)`; API işləmirsə `(null, null)` |
| `PostAsync<T>(url, body)` / `PostAsync(url, body)` | JSON POST; xətada `Errors` massivi qaytarır |
| `PutAsync` | JSON PUT |
| `PostFormAsync` / `PutFormAsync` | `multipart/form-data` (fayl yükləmə) |
| `DeleteAsync` | DELETE |
| `ReadErrorsAsync(response)` | API-nin xəta cavabını mesaja çevirir (aşağıda) |
| `SessionExpiredAsync()` | Token bitibsə cookie-ni bağlayıb Welcome-ə yönləndirir |
| `Absolutize(cards)` | Kartların şəkil yolunu API ünvanı ilə tam URL edir |

**Xəta oxuyan (`ReadErrorsAsync`) iki format tanıyır:**
1. Bizim API: `{ "isSuccess": false, "errors": ["..."] }`
2. ASP.NET-in öz doğrulama cavabı: `{ "errors": { "Field": ["..."] } }`
3. `429` → "Too many attempts. Wait a minute and try again."
4. Gözlənilməz cavab (415, 403, boş gövdə) → istifadəçiyə ümumi mesaj, **log-a** status + gövdə (`ApiCall` logger).

Beləliklə controller-lər yalnız `(success, errors, status)` tuple-ı ilə işləyir, HTTP detalları bir yerdədir.

Tuple qaytarma nümunəsi:
```csharp
var (success, errors, status) = await PostAsync("api/cards/initial", new { fin = ... });
if (status == HttpStatusCode.Unauthorized) return await SessionExpiredAsync();
if (!success) { TempData["CardError"] = string.Join(" ", errors); return RedirectToAction(nameof(AddCard)); }
```

### 2.3 `BearerTokenHandler`
`Handlers/BearerTokenHandler.cs` — `DelegatingHandler`. API-yə gedən **hər** sorğuya iki şey əlavə edir:
1. `Authorization: Bearer <JWT>`. Token cookie-nin içindədir: `context.GetTokenAsync("access_token")`.
2. `X-Forwarded-For: <istifadəçinin IP-si>`. Çünki API-nin rate limiter-i IP-yə görə işləyir; yoxsa API bütün sorğuları "MVC serverinin IP-si" kimi görər və hamı bir limitə düşərdi.

### 2.4 Helper siniflər (`Helpers/`)
| Fayl | Nə edir |
|---|---|
| `ApiUrl.ToAbsolute(baseUrl, path)` | `/images/x.png` → `https://api/images/x.png` |
| `AppRoles` | Rol adları (API-dəki `Roles` ilə eyni; MVC Domain-i tanımır), `Staff` sabiti, `IsStaff(...)` |
| `RoleRedirect.HomeUrl(roles)` | Login-dən sonra: işçi → `/Admin`, müştəri → `/App` |
| `NavLinks` | Home-dakı bütün linklər: giriş vəziyyətinə görə hara getməli |
| `SiteSettingsLoader` | Header/footer üçün `api/settings`-i **bir sorğuda bir dəfə** çağırır (`HttpContext.Items` keşi) |
| `SettingLabels` | Admin Settings səhifəsində açarların oxunaqlı adları |
| `AdminSections` | Admin panelinin bölmələri və hansı rolların görə biləcəyi |

**`NavLinks` məntiqi:**
- Giriş yoxdursa: `Welcome` (qeydiyyat axını), dəstək üçün `#contact` (footer).
- Giriş varsa: öz səhifəsi (`/App`, `/App/AddCard`, `/App/Transfer`, ...).
- `FromDb(url, signedIn)`: bazadan gələn düymə ünvanını çevirir: `/Account/Welcome` → `/App/AddCard`, `/#contact` → `/App/Support`.
- `ForService(number, signedIn)`: Services kartı → `Number`-ə görə (1,4 → Cards; 2 → Transfer; 3 → Loans; 5 → Bills; 6 → Support).

### 2.5 ViewComponent-lər (`ViewComponents/` + `Views/Shared/Components/<Ad>/Default.cshtml`)
| Component | API çağırışı | Harada |
|---|---|---|
| `Header` | `api/settings` (loqo, ad) | bütün layout-lar |
| `Footer` | `api/settings` | bütün layout-lar |
| `HomeTicker` | `api/home/tickers` | Home |
| `AboutVideo` | `api/home/about` + `api/home/pillars` | Home |
| `Brand` | `api/home/brands` | Home |
| `CardHero` | `api/home/cardHero` + `api/home/cards` | Home |
| `Service` | `api/home/serviceSections` + `serviceItems` | Home |
| `Benefit` | `api/home/benefitSections` + `benefitItems` | Home |

Hamısı eyni şablonla: `HttpClient` ilə çağır; `HttpRequestException` tutulursa (API işləmir) **boş/statik** qalır, səhifənin qalanı açılır. Şəkil yolları `ApiUrl`-lə tam URL olur.

### 2.6 Layout-lar (`Views/Shared`)
| Layout | Nə üçün |
|---|---|
| `_HomeLayout` | Home (ictimai): header, footer, `home.css`, GSAP (CDN: animasiyalar), `home.js` |
| `_AuthLayout` | Welcome, Login, Register, VerifyOtp, ForgotPassword, ResetPassword |
| `_AppLayout` | Giriş etmiş müştəri: navbar (Cards, Transfers, History, Loans/Bills/Contact "tezliklə"), Logout, `confirm-dialog.js`, `app-cards.js` |
| `Areas/Admin/Views/Shared/_AdminLayout` | Admin: iki rejim (UI / Bank), sidebar linkləri, bildiriş zəngi |

`_ViewImports.cshtml`: bütün ViewModel namespace-lərini və Tag Helper-ləri bütün View-lar üçün əlavə edir (`@using ...`).

### 2.7 Səhifə siyahısı (müştəri tərəfi)
| URL | Controller | Fayl |
|---|---|---|
| `/` | `HomeController.Index` | `Views/Home/Index.cshtml` |
| `/Account/Welcome` ... | `AccountController` | `Views/Account/*` (bax `13`) |
| `/App` | `AppController.Index` | `Views/App/Index.cshtml` |
| `/App/AddCard` | `AppController.AddCard` | `Views/App/AddCard.cshtml` |
| `/App/Card/{id}` | `AppController.Card` | `Views/App/Card.cshtml` |
| `/App/Transfer` | `TransferController` | `Views/Transfer/*` |
| `/App/History` | `HistoryController` | `Views/History/*` |
| `/Home/Error` | `HomeController.Error` | `Views/Shared/Error.cshtml` |

Hələ yazılmayanlar (linkləri var): `/App/Loans`, `/App/Bills`, `/App/Support`.

### 2.8 Flash mesajları
`_Flash.cshtml`: `TempData["Success"]` (yaşıl) və `TempData["CardError"]` (qırmızı) göstərir. Admin səhifələri öz `Success`/`Error` açarlarını istifadə edir.

## Özünü yoxla

1. MVC layihəsi niyə bazaya qoşulmur?
2. `ApiControllerBase`-in vəzifəsi nədir? `GetAsync`/`PostAsync` niyə tuple qaytarır?
3. `BearerTokenHandler` hər sorğuya nə əlavə edir və niyə `X-Forwarded-For`?
4. ViewComponent ilə partial arasındakı fərq?
5. API işləmirsə Home açılırmı? Niyə?
6. PRG nədir? Hansı problemi həll edir?
7. `[ValidateAntiForgeryToken]` nədən qoruyur?
8. `NavLinks.FromDb` nə üçün lazımdır?
