# 91. Layihəni sıfırdan yenidən yazma planı

Bu plan təhsil üçündür: layihəni **boş qovluqdan** yenidən yazırsan. Hər mərhələnin sonunda **yoxlama nöqtəsi** (checkpoint) var: keçmədən növbəti mərhələyə getmə. Hər mərhələdə əvvəl aid sənəd faylını oxu, sonra özün yaz, sonra orijinal kodla müqayisə et.

> İstifadə qaydası: yeni qovluqda `CaspianBank-Practice` adlı yeni solution aç. Orijinal layihə yalnız "cavab açarı"dır, kopyalama. Fərqli adlar işlət ki, kopyalamaq əvəzinə düşünəsən.

## Ümumi qaydalar
1. Hər mərhələni **commit** et (`git init` ilə öz repon).
2. Əvvəl sənədi oxu, sonra yazmağa başla, ilişəndə orijinal koda bax.
3. Yazdığını **sına**: Swagger-də (API) və brauzerdə (MVC).
4. Hər mərhələnin sonunda "Özünü yoxla" suallarını yazılı cavabla.
5. Vaxt təxmini ilk cəhd üçündür (yavaş öyrənənlər üçün).

---

## Mərhələ 0. Hazırlıq (1–2 saat)
- .NET 8 SDK, Visual Studio/VS Code, SQL Server Express, Git.
- Solution: `CaspianPractice.sln`; layihələr: `Domain` (class library), `Repository` (class library), `Service` (class library), `CaspianPractice.Api` (ASP.NET Core Web API).
- Referanslar: Api → Service → Repository → Domain.
- **Checkpoint:** solution build olur.

## Mərhələ 1. Domain + baza (4–6 saat) → oxu: `02`
1. `BaseEntity`.
2. `Brand` entity + `BrandConfiguration`.
3. `AppUser : IdentityUser` (Name, Surname, BirthDay).
4. `AppDbContext : IdentityDbContext<AppUser>`, `ApplyConfigurationsFromAssembly`.
5. Connection string, `AddDbContext`.
6. İlk migration (`AddedBrandTable`), `Database.MigrateAsync()`.
- **Checkpoint:** API işə düşəndə baza və `Brands`, `AspNetUsers` cədvəlləri yaranır (SSMS-də gör).

## Mərhələ 2. Repository + ilk CRUD (4–6 saat) → oxu: `03`, `04`, `06`
1. `IBaseRepository<T>` + `BaseRepository<T>` (GetAll, GetById, Add, Update, Delete).
2. `IBrandRepository`, `BrandRepository`, DI qeydiyyatı (`AddRepositoryLayer`).
3. DTO-lar: `BrandDto`, `CreateBrandDto`, `UpdateBrandDto`.
4. Validator-lar (FluentValidation) + `RequiredText` uzantısı.
5. `BadRequestException`, `NotFoundException`.
6. `BrandService` (şəkilsiz, əvvəl sadə mətn sahəsi ilə).
7. `ExceptionHandlingMiddleware`.
8. `BrandController` (ilkin: `[Authorize]`-sız).
- **Checkpoint:** Swagger-dən Brand yarat/oxu/yenilə/sil; boş ad göndərəndə `400 {isSuccess:false, errors:[...]}`; mövcud olmayan id-də `404`.

## Mərhələ 3. Fayl yükləmə (3–4 saat) → oxu: `07`
1. `ImageFileRules` (magic bytes).
2. `FileService` (Upload, Delete + regex).
3. `Brand`-a `Image` əlavə et (migration), `multipart/form-data`.
4. `UseStaticFiles`, `wwwroot/images/brands`.
- **Checkpoint:** PNG yüklənir; `.exe` adını `.png`-yə dəyişib yükləmək rədd edilir; 3 MB fayl rədd edilir; baza xəta versə fayl qalmır (yoxlamaq üçün müvəqqəti `throw` qoy).

## Mərhələ 4. Giriş sistemi (8–12 saat) → oxu: `05`
1. Identity qur (`AddIdentity`), parol qaydaları.
2. `JwtSettings`, `TokenService`, `AddJwtBearer`.
3. `AccountRepository` (UserManager sarğısı).
4. `RegisterDto`/`LoginDto` + validator-lar; `AccountService`; `AccountController` (register, login, me).
5. Rollar: `Roles`, `RoleSeeder`, `AdminSeeder`.
6. `[Authorize(Roles=...)]` Brand controller-ə.
7. Rate limiting (`auth`).
8. Lockout, `IsRestricted`.
9. **OTP:** `EmailOtp` + `OtpService` + `IEmailSender` (əvvəl `LogEmailSender`) + endpoint-lər (check-email, send-otp, verify-otp), `register`-i token tələb edən et.
10. Şifrə bərpası (`PasswordService`).
- **Checkpoint:** Swagger-də register (OTP ilə) → login → token → "Authorize" düyməsinə yapışdır → Brand yarat; müştəri tokeni ilə Brand yaratmaq `403`; 6 yanlış parol → lockout.

## Mərhələ 5. MVC: layout, ApiControllerBase, giriş (8–10 saat) → oxu: `12`, `13`
1. MVC layihəsi, `AddHttpClient("CaspianApi")`, cookie authentication.
2. `BearerTokenHandler`.
3. `ApiControllerBase` (GetAsync, PostAsync, ReadErrorsAsync, SessionExpiredAsync).
4. Layout-lar (`_AuthLayout`, `_AppLayout`).
5. `AccountController`: Welcome → Login/VerifyOtp → Register → Login. `SignInAsync` ilə cookie içində token.
6. `[Authorize]` olan test səhifəsi `/App`.
- **Checkpoint:** brauzerdə tam qeydiyyat → login → `/App`-da adın görünür; logout; yanlış parol xətası formada.

## Mərhələ 6. Admin Area + Brand CRUD (6–8 saat) → oxu: `14`, `08`
1. `Areas/Admin`, marşrut, `_AdminLayout`.
2. `BrandController` (Index, Detail, Create, Edit, Delete) + ViewModel-lər + View-lar + `PostFormAsync`.
3. Confirm dialog (`data-confirm-*`).
4. Flash mesajları.
- **Checkpoint:** admin hesabı ilə Brand siyahısı/yaratma/silmə; müştəri hesabı `/Admin`-ə girəndə AccessDenied.

## Mərhələ 7. Home məzmunu (10–14 saat) → oxu: `08`
Eyni şablonu təkrarla: Ticker, Hero (tək), About+Pillar, Services, Benefits (dropdown), Settings (açar-dəyər), CardDesign. Sonra Home üçün ictimai `HomeController` + ViewComponent-lər.
- **Checkpoint:** admin Ticker mətnini dəyişir, Home-da dərhal görünür.

## Mərhələ 8. Kartlar (8–10 saat) → oxu: `09`
1. `Card`, `CardTierConfig`, `Transaction` entity-ləri + konfiqurasiyalar (RowVersion, unikal indekslər, seed).
2. `CardNumberGenerator` (Luhn).
3. `CardRepository`, `CardService`: ilk kartlar, ödənişli kart, top-up, bloklama.
4. `CardsController`.
5. MVC `AppController` + View-lar.
- **Checkpoint:** yeni müştəri FİN yazır → Standard + Cashback pulsuz; Gold açmaq üçün 40 AZN çıxılır; top-up jurnalda görünür.

## Mərhələ 9. Köçürmə (10–14 saat) → oxu: `10`
1. `TransferDto` + validator.
2. `TransferService.ResolveAsync` (qaydalar + komissiya).
3. `TransferRepository.ExecuteAsync` (tranzaksiya + retry).
4. `RequestId`/idempotency.
5. Preview/Transfer endpoint-ləri, MVC 3 addımlı axın.
- **Checkpoint:** 8 yoxlama ssenarisini (10-cu faylın sonunda) hamısını sına, xüsusilə idempotency və paralel köçürmə.

## Mərhələ 10. Tarixçə və çıxarış (6–8 saat) → oxu: `11`
- Filtr + səhifələmə sorğusu, Bakı vaxtı, çıxarış, MVC səhifələri.
- **Checkpoint:** `açılış + giriş − çıxış = bağlanış` bərabərliyi tutur.

## Mərhələ 11. Cilalama
- Responsive CSS, JS (custom select, tarix təkəri), xəta səhifələri.
- Unit test-lər (bax aşağı).
- README.

---

## Test yazma planı (mərhələ 11 üçün)
**Servis testləri (xUnit + Moq):**
- `TransferService`: komissiya hesabı (limitdən aşağı/yuxarı), Cashback qaydaları, bloklu kart, yetərsiz balans.
- `OtpService`: düzgün kod, yanlış kod, 5 cəhd limiti, vaxtı keçmiş kod.
- `CardNumberGenerator`: `Generate` Luhn-u keçir, `IsValid` yanlışları rədd edir.
- `HistoryService`: Bakı gününün UTC-yə çevrilməsi.

**İnteqrasiya testləri:** `WebApplicationFactory` + InMemory/SQLite: login → kart → köçürmə axını.

## Əvvəlcədən bilməli olduğun mövzular (öyrənmə sırası)
1. C# əsasları: sinif, interfeys, `async/await`, `LINQ`, `null` (nullable reference types).
2. HTTP əsasları: metodlar, status kodları, başlıqlar, cookie.
3. SQL əsasları: `SELECT/JOIN/WHERE/GROUP BY`, indeks, tranzaksiya.
4. ASP.NET Core: DI, middleware, routing, model binding.
5. EF Core: DbContext, migration, LINQ → SQL, tracking.
6. Təhlükəsizlik: parol hash, JWT, CSRF, XSS, SQL injection, brute-force.
7. HTML/CSS/JS əsasları.

## Qeyd: nə vaxt ilişsən
- Xəta mesajını **sözbəsöz** oxu: ən çox cavab onun içindədir.
- Qeydiyyatdan çıxan servis xətası (`Unable to resolve service`): `DependencyInjection.cs`-də `AddScoped` unudulub.
- `415 Unsupported Media Type`: fayl göndərəndə `multipart` istifadə et, `[FromForm]`.
- `Something went wrong` MVC-də: konsolda `ApiCall` log-una bax; API-nin cavabını göstərir.
- Şəkil görünmür: View-da `ApiUrl.ToAbsolute(...)` unudulub.
