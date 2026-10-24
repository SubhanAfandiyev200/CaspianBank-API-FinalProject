# 13. MVC-də giriş axını (cookie içində JWT)

Fayllar (MVC): `Controllers/AccountController.cs`, `Views/Account/*`, `Handlers/BearerTokenHandler.cs`, `Program.cs`, `Helpers/RoleRedirect.cs`, `Helpers/AppRoles.cs`, `wwwroot/js/auth-fields.js`, `otp-input.js`, `wheel-date.js`.

## 1. Nəzəriyyə

### 1.1 Problem
API JWT ilə işləyir, brauzer isə veb-səhifələrdə **cookie** ilə işləyir. İstifadəçi MVC səhifələrində gəzərkən "mən kiməm" məlumatı necə saxlanır?

### 1.2 Bizim həll: cookie authentication + token cookie içində
1. İstifadəçi login olur → MVC API-dən **JWT** alır.
2. MVC **cookie** yaradır (`Caspian.Auth`) və JWT-ni onun **içində** saxlayır (`StoreTokens`). Cookie şifrələnir (ASP.NET Data Protection).
3. Sonrakı səhifələrdə brauzer cookie göndərir; MVC onu oxuyub `User`-i qurur (`User.Identity.Name`, rollar).
4. API-yə müraciət edəndə `BearerTokenHandler` tokeni cookie-dən çıxarıb `Authorization` başlığına qoyur.

Niyə JWT-ni `localStorage`-da saxlamırıq? XSS hücumunda JavaScript `localStorage`-ı oxuya bilər. Cookie-ni isə `HttpOnly` etmişik: JavaScript onu **görmür**.

### 1.3 Cookie müddəti = JWT müddəti
`ExpiresUtc = login.ExpiresAt` və `SlidingExpiration = false`. İkisi eyni anda bitir. Bununla "cookie hələ var amma token bitib" vəziyyəti azalır. Bu hələ də baş verə bilər (məsələn, server tərəfdə token ləğv olunubsa), ona görə hər controller `Unauthorized`-ı tutub `SessionExpiredAsync()` çağırır.

### 1.4 Welcome → Login/Register axını (email-first)
Giriş səhifəsi yoxdur, **Welcome** var: istifadəçi əvvəl yalnız email yazır.
- Email mövcuddur → Login.
- Email yoxdur → OTP kodu göndərilir → VerifyOtp → Register.

Bu, istifadəçi üçün sadədir (bir sahə) və qeydiyyatla girişi birləşdirir. Təhlükəsizlik nəticəsi: `check-email` emailin mövcudluğunu açır (user enumeration), ona görə rate limit ilə qorunur.

## 2. Addım-addım

### 2.1 `Welcome` (GET/POST)
- GET: artıq giriş varsa `RoleRedirect.HomeUrl(roles)`-a yönləndirir. Yoxdursa form (əvvəl yazılmış email `TempData.Peek("AuthEmail")` ilə dolu).
- POST: `api/account/check-email`.
  - `429` → "Too many attempts".
  - `Exists = true` → `TempData["AuthEmail"] = email` → `Login`.
  - `Exists = false` → `SendOtpAsync` → `TempData["Info"]`, `DevCode` (yalnız Development), `OtpResendAt` → `VerifyOtp`.

### 2.2 `VerifyOtp`
- GET: `AuthEmail` yoxdursa Welcome-ə. Əks halda 6 xanalı kod səhifəsi + "Resend code" geri sayımı.
- POST: kod `^\d{6}$` formatındadırsa `api/account/verify-otp`. Uğurlu → `TempData["VerificationToken"]` → `Register`.
- `ResendOtp`: yeni kod istəyir. Geri sayım serverdə `OtpResendAt` ilə saxlanılır ki, səhv kod yazılıb səhifə yenilənəndə sayğac sıfırlanmasın. API cooldown (60 san.) üçün `429 + RetryAfterSeconds` qaytarsa qalan vaxt davam edir.

### 2.3 `Register`
- GET: `AuthEmail` və `VerificationToken` olmasa Welcome-ə qaytarır (OTP-siz Register açılmır).
- POST: parollar uyğun gəlməlidir; telefon rəqəmlərə çevrilir (`Regex.Replace(.., @"\D", "")`); `api/account/register`. Uğurlu → `TempData["Info"]` + Login.

Tarix seçimi: `wheel-date.js` (təkər tipli seçici, `input[type=date]`-i avtomatik əvəz edir).
Telefon maskası: `auth-fields.js`.

### 2.4 `Login`
```csharp
var response = await client.PostAsJsonAsync("api/account/login", new { email, password });
...
await SignInAsync(result);
```
`SignInAsync`:
```csharp
var claims = new List<Claim> {
    new(ClaimTypes.NameIdentifier, login.UserId),
    new(ClaimTypes.Email, login.Email),
    new(ClaimTypes.Name, login.FullName)
};
claims.AddRange(login.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

var principal  = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
var properties = new AuthenticationProperties { IsPersistent = false, AllowRefresh = false, ExpiresUtc = login.ExpiresAt };
properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = login.Token } });
await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
```
- `IsPersistent = false`: brauzer bağlananda giriş bitir.
- **Rollar cookie-də də saxlanır** (`[Authorize(Roles = ...)]` MVC-də də işləsin deyə). Həm MVC, həm API rolu yoxlayır.

Sonra yönləndirmə: `ReturnUrl` yerli (`Url.IsLocalUrl`) olarsa ora (**open redirect** qorunması: xarici sayta yönləndirməyə icazə yoxdur); əks halda `RoleRedirect.HomeUrl(roles)` → işçi `/Admin`, müştəri `/App`.

Parol formaya geri qaytarılmır (`model.Password = ""`).

### 2.5 `Logout`
`POST` (GET deyil: link ilə kimsə istifadəçini gizli çıxara bilməsin), `[ValidateAntiForgeryToken]`. `SignOutAsync` + Home. UI-da təsdiq pəncərəsi (`data-confirm-*`, `confirm-dialog.js`).

### 2.6 Şifrə bərpası
- `ForgotPassword` POST → `api/account/forgot-password`; email olsa da olmasa da eyni "link göndərildi" səhifəsi (Development-də `DevLink` göstərilir).
- `ResetPassword?email=...&token=...` GET → forma; POST `api/account/reset-password`. Uğurlu → Login.

### 2.7 Giriş tələb edən səhifələr
`[Authorize]` — cookie yoxdursa `LoginPath` (`/Account/Welcome`) yönləndirilir. Rol uyğun gəlməsə `AccessDeniedPath` (`/Home/AccessDenied`).

### 2.8 Rolun işləməsi
- `AppRoles.IsStaff(...)`: `Customer`-dən başqa hər rol işçidir.
- Admin controller-lərdə `[Authorize(Roles = "Admin,SuperAdmin")]` və s.
- `AdminSections.All` — hansı bölməni hansı rolun gördüyünün cədvəli (sidebar və təsvir üçün).
- **Hələ edilməyib** (planlaşdırılıb, sən yazacaqsan): tam rol matrisi (kim hansı bölməni görür) və sidebar-ın rola görə gizlədilməsi.

## 3. Təhlükəsizlik xülasəsi
| Təhlükə | Müdafiə |
|---|---|
| XSS ilə tokenin oğurlanması | token cookie içindədir, `HttpOnly` |
| CSRF | `[ValidateAntiForgeryToken]` + `SameSite=Lax` |
| Open redirect | `Url.IsLocalUrl(ReturnUrl)` |
| User enumeration | eyni xəta mesajları, rate limit |
| Brute force | API-də rate limit + lockout |
| Parol sızması | parol View-a geri qaytarılmır |
| Cookie interception | Production-da `SecurePolicy=Always` |

## Özünü yoxla

1. Niyə JWT cookie içində saxlanılır, `localStorage`-da yox?
2. Cookie və JWT-nin müddəti niyə eynidir? Yenə də token bitə bilərmi?
3. Welcome səhifəsi niyə var? Axının addımlarını say.
4. `IsPersistent = false` nə deməkdir?
5. Open redirect nədir? Kodda necə qorunur?
6. Logout niyə POST-dur?
7. Rollar həm cookie-də, həm JWT-də niyə var?
