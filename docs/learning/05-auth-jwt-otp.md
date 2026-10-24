# 05. Giriş sistemi: Identity, JWT, rollar, OTP

Bu fayl layihənin ən "qəliz" hissəsini izah edir. Əvvəl nəzəriyyə, sonra bizim kod.

## 1. Nəzəriyyə

### 1.1 Authentication vs Authorization
- **Authentication (kimlik)**: "Sən kimsən?" → login.
- **Authorization (icazə)**: "Sən bunu edə bilərsənmi?" → rollar, `[Authorize]`.

### 1.2 ASP.NET Identity
Hazır istifadəçi sistemi: istifadəçi cədvəlləri, parol hash-ləmə, rol, lockout, email təsdiq, şifrə bərpa tokenləri.
- **`UserManager<AppUser>`**: istifadəçi əməliyyatları (yarat, parolu yoxla, rol ver...).
- **`RoleManager<IdentityRole>`**: rol yarat/yoxla.
- Parol **heç vaxt** düz saxlanmır. Identity onu PBKDF2 ilə hash edir (`PasswordHash` sütunu), duz (salt) ilə.

`Program.cs`:
```csharp
builder.Services.AddIdentity<AppUser, IdentityRole>(options => { ... })
       .AddEntityFrameworkStores<AppDbContext>()
       .AddDefaultTokenProviders();
```
- `AddEntityFrameworkStores`: Identity cədvəlləri bizim `AppDbContext`-dədir.
- `AddDefaultTokenProviders`: şifrə bərpa tokenlərini yaradır (30 dəqiqə etibarlı: `DataProtectionTokenProviderOptions.TokenLifespan`).

Bizim qaydalar: parol ≥ 6, böyük + kiçik + rəqəm + xüsusi simvol; 5 uğursuz cəhddən sonra 5 dəqiqə lockout; email unikal.

### 1.3 JWT (JSON Web Token)
Login olandan sonra server istifadəçiyə **token** verir. İstifadəçi hər sorğuda tokeni göndərir: `Authorization: Bearer <token>`. Server saxlamır, **yoxlayır** (stateless).

JWT üç hissədir (`.` ilə ayrılıb): `Header.Payload.Signature`
- **Header**: alqoritm (`HS256`).
- **Payload (claims)**: tokenin içindəki məlumat: `NameIdentifier` (UserId), `Email`, `Name`, `Role`-lar, `Jti` (unikal id), `exp` (bitmə).
- **Signature**: `HMAC-SHA256(header.payload, gizli açar)`. Payload dəyişdirilsə imza uyğun gəlmir, token etibarsız olur.

Tokenin içi **şifrəli deyil** (Base64-dir, oxumaq olar). Ona görə içinə parol və s. qoymuruq. İmza yalnız "dəyişdirilməyib" deyir.

**Doğrulama (`TokenValidationParameters`)** hansı yoxlamaları edir:
- `ValidateIssuer/Audience`: token bizim API üçün və bizim tərəfimizdən verilib.
- `ValidateLifetime`: vaxtı bitməyib (`ClockSkew = 1 dəq`).
- `ValidateIssuerSigningKey`: imza düzgündür.

### 1.4 Claims və rollar
Claim: "istifadəçi haqqında bir fakt" (`ClaimTypes.Role = "Admin"`). `[Authorize(Roles = "Admin,SuperAdmin")]` tokendəki `Role` claim-ləri ilə uyğunluğu yoxlayır (bir rol kifayətdir).

### 1.5 OTP (One-Time Password)
Qeydiyyatdan əvvəl emailin həqiqətən istifadəçiyə aid olduğunu sübut edirik: 6 rəqəmli kod emailə göndərilir, istifadəçi onu yazır.
Təhlükəsizlik üçün:
- Kod bazada **hash** kimi saxlanır (HMAC-SHA256), düz yox.
- Hash açarı serverdədir (JWT açarı), bazada yox: baza sızsa belə 6 rəqəmli kodu "oflayn tapmaq" mümkün olmur (yoxsa 1 milyon variantı sınamaq asandır).
- Kodun ömrü 5 dəqiqə, cəhd limiti 5, yeniden göndərmə arası 60 saniyə.
- Müqayisə `CryptographicOperations.FixedTimeEquals` ilə (zaman hücumlarının qarşısı).

### 1.6 Brute-force və enumeration qorunması
- **Rate limiting**: eyni IP-dən dəqiqədə 10 cəhd (`auth` siyasəti).
- **Lockout**: 5 yanlış parol → 5 dəqiqə blok.
- **User enumeration**: login-də "email yoxdur" və "parol yanlışdır" mesajı eyni olmalıdır (`Invalid email or password.`). Şifrə bərpasında email olsa da olmasa da eyni cavab. `check-email` rate limit ilə qorunur.

## 2. Bizim layihədə

### 2.1 Qeydiyyat axını (4 addım)

```
[MVC Welcome]  email yaz
   │ POST /api/account/check-email   → Exists?
   │   Exists = true ──────────────────────────────────→ Login səhifəsi
   │   Exists = false
   ▼
   POST /api/account/send-otp        → 6 rəqəmli kod emailə (EmailOtp sətri yazılır)
   ▼
[MVC VerifyOtp]  kodu yaz
   │ POST /api/account/verify-otp    → kod düzgündürsə VerificationToken qayıdır
   ▼
[MVC Register]  ad, soyad, telefon, doğum tarixi, parol
   │ POST /api/account/register      (VerificationToken ilə)
   ▼
[MVC Login] ──POST /api/account/login──> JWT
```

### 2.2 `OtpService` (`Service/Services/OtpService.cs`)

**`SendOtpAsync`**:
1. Validator.
2. Email normallaşdırılır (`Trim` + `ToLowerInvariant`).
3. Email artıq qeydiyyatlıdırsa → xəta.
4. Cooldown yoxlanır (son kod 60 saniyədən yenidirsə → `RetryAfterSeconds`, controller `429` qaytarır).
5. Kod: `RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")` (kriptoqrafik təsadüfi).
6. Köhnə aktiv kodlar ləğv edilir (`InvalidateActiveAsync`), yeni `EmailOtp` yazılır (`CodeHash = HMAC(...)`).
7. Email göndərilir. Xəta olsa istifadəçiyə ümumi mesaj, real səbəb log-da.
8. `DevCode` yalnız `ExposeCodes` olduqda qayıdır.

**`VerifyOtpAsync`**:
1. Ən son aktiv kod tapılır (`!IsUsed`, vaxtı keçməyib).
2. `Attempts++`; limit (5) aşıbsa kod ləğv edilir.
3. Hash müqayisə olunur (`FixedTimeEquals`). Yanlışdırsa cəhd sayı saxlanır, xəta.
4. Düzgündürsə: **təsdiq tokeni** (32 təsadüfi bayt, Base64Url) yaradılır; bazada onun **hash-i** (`VerificationTokenHash`) və `VerifiedAt` yazılır; kod `IsUsed = true`. Token istifadəçiyə qayıdır.

**`Register` bu tokeni tələb edir.** Yəni emaili təsdiqləməyən Register edə bilməz. Token 15 dəqiqə etibarlıdır, bir dəfə işləyir (`ConsumeVerificationAsync` `VerificationTokenHash`-i null edir).

**Hash funksiyası**: `Hash(purpose, email, value)` = `HMACSHA256(key, "purpose|email|value")`. `purpose` ("email-otp" / "otp-verification") fərqli məqsədlərin hash-lərinin qarışmasının qarşısını alır.

### 2.3 `AccountService` (`Service/Services/AccountService.cs`)

**Register**:
1. Validator xətalarını `RegisterResponse.Errors`-a yığır.
2. `IsVerifiedAsync(email, token)` → təsdiq yoxdursa rədd.
3. `AppUser` yaradılır: `UserName = Email` (login email ilədir), `EmailConfirmed = true`.
4. `_accountRepository.RegisterAsync(user, password)` (`UserManager.CreateAsync`: parolu hash-ləyir).
5. `Customer` rolu verilir.
6. Token istifadə olunmuş sayılır.

**Login**:
1. Validator.
2. İstifadəçi tapılmadısa → `Invalid email or password.`
3. Lockout varsa → mesaj.
4. Parol yoxlanır. Yanlışdırsa `AccessFailedAsync` (sayğac++), eyni mesaj.
5. Düzgündürsə sayğac sıfırlanır.
6. **`IsRestricted`** (admin dondurub) → `Your account is restricted.`
7. Rollar götürülür, `TokenService.CreateToken(user, roles)` JWT yaradır.
8. `LoginResponse`: `Token, ExpiresAt, UserId, Email, FullName, Roles`.

**CheckEmail**: email mövcuddurmu (Welcome səhifəsi buna görə Login/Register seçir).

### 2.4 `TokenService`
`Service/Services/TokenService.cs`:
```csharp
var claims = new List<Claim> {
    new(ClaimTypes.NameIdentifier, user.Id),
    new(ClaimTypes.Email, user.Email ?? ""),
    new(ClaimTypes.Name, $"{user.Name} {user.Surname}".Trim()),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};
claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var token = new JwtSecurityToken(issuer, audience, claims, expires, credentials);
return new JwtSecurityTokenHandler().WriteToken(token);
```
Müddət: `Jwt:ExpiryMinutes` (60).

### 2.5 JWT doğrulaması: `Program.cs`
```csharp
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => { options.TokenValidationParameters = new TokenValidationParameters { ... }; });
```
Nə üçün `Default...Scheme = JwtBearer`? `AddIdentity` defolt olaraq **cookie** sxemini seçir. Bu API-dir, ona görə 401 əvəzinə "login səhifəsinə yönləndirmə" cavabı verməsin deyə JWT-ni defolt edirik.

Middleware sırası: `UseAuthentication()` (tokeni oxuyub `User`-i qurur) → `UseAuthorization()` (rolları yoxlayır). Sıra dəyişməməlidir.

### 2.6 Rollar və seed
`Domain/Constants/Roles.cs`: `Customer, SuperAdmin, Admin, Accountant, CustomerSupport, Security` (WebDesigner rolu 2026-10-10-da silindi: Home məzmununu Admin və SuperAdmin idarə edir). Başlanğıcda `RoleSeeder` hamısını yaradır. Qeydiyyatdan keçən hər müştəriyə `Customer` verilir. İşçi hesabları `AdminSeeder` ilə `SeedAdmins` konfiqurasiyasından yaranır (parollar `appsettings.Local.json`-da).

### 2.7 Şifrə bərpası (`PasswordService`)
1. **Forgot**: email tapılırsa Identity `GeneratePasswordResetTokenAsync` ilə token yaradır. Link: `{Frontend:BaseUrl}/Account/ResetPassword?email=...&token=<Base64Url>`. Email göndərilir. Email olmasa da cavab **eynidir**. `IsRestricted` hesablara link göndərilmir.
2. **Reset**: token Base64Url-dan çevrilir, `ResetPasswordAsync(user, token, newPassword)`. `InvalidToken` xətası ümumi mesaja çevrilir. Uğurlu olsa lockout sayğacı sıfırlanır.

Token niyə Base64Url? Identity tokenində `+`, `/`, `=` simvolları ola bilər, URL-də problem yaradır.

### 2.8 `AccountController` (API)
`Controllers/Client/AccountController.cs`, route `api/account`:

| Endpoint | Rate limit | Nəticə |
|---|---|---|
| `POST register` | yox | 200 / 400 |
| `POST login` | `auth` | 200 / 401 |
| `POST check-email` | `auth` | 200 / 400 |
| `POST send-otp` | `auth` | 200 / 400 / 429 |
| `POST verify-otp` | `auth` | 200 / 400 |
| `POST forgot-password` | `auth` | 200 / 400 |
| `POST reset-password` | `auth` | 200 / 400 |
| `GET me` | yox | tokenin içindəki məlumatlar (`[Authorize]`) |

## 3. Tipik suallar
- **"Token oğurlansa?"** Müddəti bitənə qədər işləyir. Yumşaltma: qısa ömür (60 dəq), HTTPS, MVC-də token cookie içində `HttpOnly` saxlanır (JavaScript oxuya bilmir).
- **"Parol niyə hash olunur?"** Baza sızsa parollar bilinməsin. Hash geri çevrilmir; login-də daxil edilən parol hash-lənib müqayisə olunur.
- **"Hash və şifrələmə (encryption) fərqi?"** Hash birtərəflidir, şifrələmə açarla geri açılır.
- **"Niyə OTP-nin hash-i HMAC-dır, adi SHA256 yox?"** Açar olmadan 1 milyon kodu sınayıb tapmaq asandır; açar serverdə olduğu üçün baza sızsa belə kodu tapmaq olmur.

## Özünü yoxla

1. Authentication və Authorization fərqi?
2. JWT-nin üç hissəsi nədir? İmza nəyi təmin edir?
3. `[Authorize(Roles = "Admin,SuperAdmin")]` necə işləyir?
4. Qeydiyyat axınını 4 addımda say. `VerificationToken` niyə lazımdır?
5. Login-də "email yoxdur" və "parol yanlışdır" niyə eyni mesajdır?
6. Lockout necə işləyir? Hansı metodlar onu idarə edir?
7. Niyə `DefaultScheme` JWT-yə təyin olunub?
8. OTP kodu niyə bazada düz saxlanmır?

## 4. Əlavə (2026-10-10): OTP artıq yalnız qeydiyyat üçün deyil
`EmailOtp.Purpose` ilə eyni mexanizm giriş etmiş istifadəçinin həssas əməliyyatları üçün də istifadə olunur (kartı bloklamaq və blokdan çıxarmaq). Fərqlər:
- Qeydiyyat: email hələ qeydiyyatda deyil; kod düzgün olanda **təsdiq tokeni** yaranır (`Register` bunu tələb edir).
- Kart əməliyyatı: istifadəçi artıq giriş edib; kod hesabın emailinə gedir, düzgün olanda kod sadəcə istifadə olunmuş sayılır, token yoxdur.
- Hash məqsədə bağlıdır: bir məqsədin kodu başqasında işləmir. Təfsilat: `09-cards.md` bölmə 3.4.
Metodlar: `IOtpService.SendCodeAsync(email, purpose, subject, intro)` və `ConfirmCodeAsync(email, purpose, code)`.
