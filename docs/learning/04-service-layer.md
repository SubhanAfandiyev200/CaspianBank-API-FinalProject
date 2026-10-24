# 04. Service qatı

Layihə: `Service/`. **Bankın beyni budur**: qaydalar, yoxlamalar, hesablamalar. Controller nazik olur (sadəcə çağırır), Repository sadə olur (sadəcə yazır/oxuyur), qərar isə burada verilir.

## 1. Nəzəriyyə

### 1.1 Service-in vəzifəsi
Bir iş funksiyası (use case) = bir Service metodu. Məsələn "köçürmə et":
1. Daxil olan məlumatı yoxla.
2. İstifadəçini/kartları tap.
3. Qaydaları tətbiq et (bloklu? limit? Cashback?).
4. Hesabla (komissiya).
5. Repository-yə yazdır.
6. Nəticəni DTO-ya çevirib qaytar.

### 1.2 DTO (Data Transfer Object)
Entity-ni birbaşa API-yə çıxarmaq təhlükəlidir (parol hash-i, daxili sahələr sızar; həm də entity-dəki dəyişiklik API-ni sındırar). DTO: yalnız lazım olan sahələri olan sadə sinifdir.
- **Giriş DTO**: istifadəçidən gələn (`RegisterDto`, `TransferDto`).
- **Çıxış DTO**: istifadəçiyə gedən (`CardDto`, `TransferReceiptDto`).
- Eyni entity üçün bir neçə DTO ola bilər: `BrandDto` (oxu), `CreateBrandDto`, `UpdateBrandDto`.

> Bu layihədə qayda: API-də `DetailDto` yoxdur. Eyni `XDto` həm siyahıda, həm detalda istifadə olunur.

### 1.3 Validation (FluentValidation)
İstifadəçi məlumatına heç vaxt etibar edilmir. Hər giriş DTO-nun üçün validator yazılır:
```csharp
public class CreateBrandDtoValidator : AbstractValidator<CreateBrandDto>
{
    public CreateBrandDtoValidator()
    {
        RuleFor(x => x.Name).RequiredText("name", 100);
        RuleFor(x => x.Image).Must(i => i is not null && i.Length > 0).WithMessage("Choose an image.");
    }
}
```
Fayllar `Service/Helpers/Validators/<Model>/` altındadır. FluentValidation-un üstünlüyü: qaydalar oxunaqlıdır, təkrar istifadə olunur (`RequiredText`, `MustBeStrongPassword`), mesajlar bir yerdədir.

**Validator-lar necə tapılır?** `Service/DependencyInjection.cs`-də:
```csharp
services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
```
Bu sətir həmin assembly-dəki **bütün** validator-ları DI-ya qeydiyyat edir. Yeni validator yazanda ayrıca qeydiyyat lazım deyil.

### 1.4 Xəta idarəetməsi: exception vs nəticə obyekti
İki yanaşma var, biz ikisini də istifadə edirik:

| Yanaşma | Harada | Nümunə |
|---|---|---|
| **Exception atmaq** (`BadRequestException`, `NotFoundException`) | Admin CRUD servisləri | `BrandService`: ad boşdursa atılır |
| **Nəticə obyekti qaytarmaq** (`ServiceResult<T>`, `*Response`) | Müştəri funksiyaları (kart, köçürmə, login) | `TransferService` |

Exception-lar `ExceptionHandlingMiddleware` tərəfindən tutulur və JSON-a çevrilir (bax `06-api-controllers-middleware.md`). `ServiceResult` isə controller-də `Ok/BadRequest/NotFound`-a çevrilir.

`Service/Helpers/Responses/ServiceResult.cs`:
```csharp
ServiceResult<T>.Ok(data)       // IsSuccess = true, Data = data
ServiceResult<T>.Fail("mesaj")  // IsSuccess = false, Errors = [...]
ServiceResult<T>.NotFound()     // IsNotFound = true
```

### 1.5 Interface və DI
Hər servisin interfeysi var (`IBrandService`). Controller interfeysi görür. `Service/DependencyInjection.cs` bu bağlamaları edir. Yeni servis yazanda **mütləq** oraya `AddScoped` sətri əlavə olunmalıdır. Əks halda API işə düşəndə `Unable to resolve service` xətası verir.

### 1.6 `IOptions<T>` və konfiqurasiya
`appsettings.json`-dakı bölmələr tipli siniflərə bağlanır:
```csharp
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
```
Servis konstruktorunda `IOptions<JwtSettings> settings` istəyir və `settings.Value` ilə oxuyur.
Bu layihədə: `JwtSettings`, `SmtpSettings`, `FrontendSettings` (`Service/Helpers/Settings`).

## 2. Bizim layihədə

### 2.1 Qovluq xəritəsi
```
Service/
├─ DependencyInjection.cs        (AddServiceLayer)
├─ Services/                     (BrandService, TransferService, ...)
│   └─ Interfaces/               (IBrandService, ...)
└─ Helpers/
    ├─ DTOs/<Model>/             (CreateXDto, UpdateXDto, XDto)
    ├─ Validators/<Model>/       (CreateXDtoValidator, ...)
    ├─ Exceptions/               (BadRequestException, NotFoundException)
    ├─ Responses/                (ServiceResult, LoginResponse, ...)
    ├─ Settings/                 (JwtSettings, SmtpSettings, FrontendSettings)
    ├─ CardNumberGenerator.cs    (Luhn)
    ├─ ImageFileRules.cs         (magic bytes)
    └─ VideoFileRules.cs
```

### 2.2 `Service.csproj` və web tipləri
`Service` class library-dir, amma `IFormFile` və `IWebHostEnvironment` (fayl yükləmə üçün) lazımdır. Ona görə:
```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```
Bu olmasa `CS0246 IFormFile not found` xətası çıxır.

### 2.3 Admin CRUD servisi: `BrandService` (şablon)
`Service/Services/BrandService.cs`. Metodlar və niyə belədir:

```csharp
public async Task CreateAsync(CreateBrandDto model)
{
    await _createValidator.EnsureValidAsync(model);            // 1. yoxla
    var name = model.Name!.Trim();
    string imagePath = await _fileService.UploadFileAsync(model.Image!, "brands");   // 2. şəkli yaz

    var brand = new Brand { Image = imagePath, Name = name };
    try { await _brandRepo.AddAsync(brand); }                  // 3. bazaya yaz
    catch { await _fileService.DeleteFileAsync(imagePath); throw; }   // baza uğursuz oldusa şəkil yetim qalmasın
}
```
Prinsiplər:
- **Əvvəl yoxla, sonra yaz.**
- **Fayl yetim qalmasın**: bazaya yazılmadısa yüklənmiş fayl silinir.
- **Silmə sırası**: əvvəl baza, sonra fayl. (Baza silməsi xəta versə şəkil yerində qalır. Əksinə olsa sətir qalar, şəkil itər.)
- **Update**: yeni şəkil əvvəl yüklənir, baza uğurlu olandan sonra köhnəsi silinir. Köhnə şəklin yolu **həmişə bazadan** götürülür (müştəridən gələn yola etibar edilmir).
- `GetAllAsync` (admin siyahısı, köhnədən yeniyə) və `GetAllUIAsync` (Home üçün, yenidən köhnəyə) fərqli sıralayır.

`EnsureValidAsync` (`Service/Helpers/Validators/ValidationExtensions.cs`): validator xəta tapsa `BadRequestException` atır; mesajlar `.Distinct()` olur.

`RequiredText(name, maxLength)`: ortaq qayda: boş olmasın, `Trim` edildikdən sonra limiti keçməsin.

### 2.4 Müştəri servisləri
| Servis | Funksiya | Sənəd |
|---|---|---|
| `AccountService` | Register, Login, CheckEmail | `05-auth-jwt-otp.md` |
| `OtpService` | Email OTP: göndər, yoxla | `05-auth-jwt-otp.md` |
| `PasswordService` | Şifrə bərpası | `05-auth-jwt-otp.md` |
| `TokenService` | JWT yaradır | `05-auth-jwt-otp.md` |
| `CardService` | Kartlar, top-up, bloklama | `09-cards.md` |
| `TransferService` | Köçürmə | `10-transfers.md` |
| `HistoryService` | Tarixçə, çıxarış | `11-history-statement.md` |
| `FileService` | Fayl yükləmə | `07-files-upload.md` |

### 2.5 Email göndərmə
`IEmailSender` interfeysi, iki implementasiya:
- `MailKitEmailSender`: real SMTP (Gmail, port 587, STARTTLS).
- `LogEmailSender`: email göndərmir, mətni konsola yazır (SMTP qurulmayıbsa).

Hansını istifadə edəcəyi `Service/DependencyInjection.cs`-də seçilir:
```csharp
services.AddScoped<IEmailSender>(sp =>
    sp.GetRequiredService<IOptions<SmtpSettings>>().Value.IsConfigured
        ? ActivatorUtilities.CreateInstance<MailKitEmailSender>(sp)
        : ActivatorUtilities.CreateInstance<LogEmailSender>(sp));
```
`SmtpSettings.IsConfigured`: Host, UserName və Password hamısı doludursa `true`.
`ExposeCodes`: SMTP qurulmayıb **və** `ShowCodesWhenNotConfigured` (yalnız Development) → OTP kodu cavabda `DevCode` kimi qayıdır ki, real email olmadan sınaq mümkün olsun. Production-da bu bayraq **false** olmalıdır.

### 2.6 Validator nümunələri
- `RegisterDtoValidator`: email, ad, soyad, telefon (`^(0\d{9}|[1-9]\d{8})$`), doğum tarixi (18+), təsdiq tokeni, güclü parol.
- `TransferDtoValidator`: `Amount > 0`, max 100 000, maks 2 onluq; alıcı **ya** öz kart (`ToCardId`) **ya da** nömrə (XOR); not ≤ 140; `RequestId` GUID formatında.
- `TopUpDtoValidator`: məbləğ 1..5000; xarici kart nömrəsi Luhn ilə; vaxt `MM/yy` və keçməmiş; CVV 3–4 rəqəm.
- `PasswordRules.MustBeStrongPassword()`: Identity-nin `Program.cs`-dəki qaydaları ilə üst-üstə düşür (ən azı 6 simvol, böyük, kiçik, rəqəm, xüsusi simvol).

### 2.7 Card number: Luhn
`Service/Helpers/CardNumberGenerator.cs`: 16 rəqəm: Caspian BIN-i `96301020` (ilk 8 rəqəm) + 7 təsadüfi rəqəm + Luhn yoxlama rəqəmi. Eyni sinif `IsValid(number)` ilə top-up-da daxil edilən xarici kartı Luhn-a görə yoxlayır. Luhn: real kartlarda istifadə olunan sadə "səhv tutma" alqoritmidir. (Hesablama: sağdan sola hər ikinci rəqəm 2-yə vurulur, 9-dan böyükdürsə 9 çıxılır, cəm 10-a bölünməlidir.)

## 3. Yeni servis yazma şablonu (Brand-a oxşar)
1. DTO-lar: `Service/Helpers/DTOs/<X>/` (`XDto`, `CreateXDto`, `UpdateXDto`).
2. Validator-lar: `Service/Helpers/Validators/<X>/`.
3. `IXService` + `XService`.
4. `DependencyInjection.cs`-ə `AddScoped`.
5. Controller (bax `06`).

## Özünü yoxla

1. Servis qatında hansı üç əsas iş görülür (yoxla, qərar ver, yaz)?
2. DTO nə üçündür? Entity-ni birbaşa qaytarsaq hansı risk var?
3. `AddValidatorsFromAssemblyContaining<RegisterDtoValidator>()` nə edir?
4. Nə vaxt exception, nə vaxt `ServiceResult` istifadə olunur?
5. Brand yaradanda baza uğursuz olsa şəkilə nə olur? Niyə?
6. `IOptions<JwtSettings>` nədir və hardan dolur?
7. `LogEmailSender` və `MailKitEmailSender` arasında seçimi kim edir?
