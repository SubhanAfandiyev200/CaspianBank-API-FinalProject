# 08. Home səhifəsinin məzmun idarəsi (CMS) və təkrarlanan şablon

Home səhifəsindəki mətnlər, şəkillər, video və loqo **kodda yazılmayıb**, bazadadır. Admin panelindən dəyişdirilir. Bu faylda bütün bu bölmələrin ortaq şablonu izah olunur. Bir dəfə başa düşəndən sonra hamısı eynidir.

## 1. Fikir

```
Admin (Admin/SuperAdmin) ──panel──>  API (api/admin/...)  ──>  Baza
Ziyarətçi            ──Home──>   MVC ViewComponent ──>  API (api/home/...)  ──>  Baza
```
- **Admin tərəfi**: yazma (Create/Update/Delete), rollarla qorunur.
- **Ziyarətçi tərəfi**: yalnız oxuma (`api/home/...`), giriş tələb etmir.

## 2. Bölmələr və modellər

| Home bölməsi | Entity | Növ | Admin imkanları | Fayl? |
|---|---|---|---|---|
| Ticker (yuxarıdakı yazı zolağı) | `HomeTicker` | **Siyahı** | Create, Edit, Delete | yox |
| Brand zolağı | `Brand` | **Siyahı** | Create, Edit, Delete | şəkil |
| Hero (yuxarı sol mətn) | `CardHero` | **Tək** | yalnız Edit | yox |
| Hero kartları (yelpazə) | `CardDesign` | **Siyahı** (max 3 Home-da) | Create, Edit, Delete | şəkil |
| About mətn + video | `About` | **Tək** | Edit mətn, Edit video | video |
| About sütunları | `AboutPillar` | **Siyahı** (About-a bağlı) | Create, Edit, Delete | şəkil |
| Services başlıq | `ServiceSection` | **Tək** | yalnız Edit | yox |
| Services kartları | `ServiceItem` | **Sabit siyahı** (6 kart) | yalnız Edit (başlıq, mətn, ikon) | ikon |
| Benefits başlıq | `BenefitSection` | **Tək** | yalnız Edit | yox |
| Benefit kartları | `BenefitItem` | **Siyahı** | Create, Edit, Delete | yox |
| Loqo, şirkət adı, ünvan, footer mətni | `Setting` | **Açar-dəyər** | yalnız Edit dəyər / loqo | loqo |
| Kart növü qaydaları | `CardTierConfig` | **Sabit 4 növ** | yalnız Edit (Accountant) | yox |

### 2.1 Siyahı modeli (Brand, Ticker, BenefitItem, AboutPillar, CardDesign)
Tam CRUD: `GetAll`, `GetDetail`, `Create`, `Update`, `Delete`. Şablon: `BrandService`.

### 2.2 Tək-yazılı model ("singleton")
Hero, About, ServiceSection, BenefitSection: səhifədə yalnız **bir** blok var. Ona görə `Create` və `Delete` **yoxdur** (yoxsa admin silib səhifəni sındırar). Yalnız:
- `GetAsync()`: ən yeni sətri qaytarır (`BaseRepository.GetAsync()`).
- `UpdateAsync(id, dto)`.

Admin API-də "list" yoxdur, `GET api/admin/about` bir dənə qaytarır.

### 2.3 Sabit siyahı (ServiceItem)
6 service kartı var, kartın nömrəsi (`Number`) onun hara getdiyini müəyyən edir (MVC `NavLinks.ForService`). Admin kart əlavə/silmir, yalnız mətn və ikonu dəyişir. İkonu dəyişdirəndə köhnə `.svg` başlanğıc fayl silinmir (bu sistemin yazdığı fayl deyil).

### 2.4 Açar-dəyər (Setting)
`Setting { Key, Value }`. Yeni ayar üçün yeni cədvəl lazım deyil. Açarlar `SettingKeys`-dədir. Admin açarın adını **dəyişmir**, yalnız dəyərini. Loqo xüsusidir: mətnlə yox, şəkil yükləməklə dəyişir (`PUT {id}/logo`); `Logo` açarına mətn yazmaq cəhdi `BadRequestException` verir.

İctimai oxuma: `GET api/settings` → `{ "CompanyName": "...", "Logo": "/images/settings/xxx.png", ... }` (lüğət).

## 3. Home qaydaları, diqqət tələb edən yerlər

### 3.1 Kart dizaynı (`CardDesignService`)
- Home yelpazəsi **maksimum 3** kart üçün qurulub (`MaxHomeCards = 3`). `ShowOnHome = true` olan 4-cü dizayn əlavə etmək cəhdi `BadRequestException` verir. `EnsureHomeSlotAsync`.
- `DisplayOrder` kiçik rəqəm öndə.
- **İstifadədə olan dizayn silinmir** (kartlar və ya kart növü qaydaları ona bağlıdırsa): `IsUsedAsync`. Bazada da `DeleteBehavior.Restrict` var, yəni ikiqat qoruma.
- `GetHomeAsync()` yalnız `ShowOnHome` olanları, sıra ilə qaytarır (ictimai).

### 3.2 Benefit düymə ünvanı (`ButtonDestinations`)
Admin düymənin hara getdiyini **əl ilə yazmır**, sabit siyahıdan seçir (dropdown). Səbəb: yanlış və ya zərərli link (`javascript:...`) yazılmasın, mövcud olmayan səhifəyə getməsin.
- `Domain/Constants/ButtonDestinations.cs`: `/Account/Welcome`, `/App`, `/App/Transfer`, `/#contact`.
- Validator `ButtonDestinations.Contains(url)` ilə yoxlayır.
- `GET api/admin/benefit-items/destinations` dropdown-u doldurur.
- Yeni səhifə hazır olanda yalnız bu siyahıya **bir sətir** əlavə olunur.
- MVC tərəfində `NavLinks.FromDb(url, signedIn)`: giriş etmiş istifadəçi üçün `/Account/Welcome` → `/App/AddCard`, `/#contact` → `/App/Support`.

### 3.3 Home-da ziyarətçi vs giriş etmiş istifadəçi
Eyni düymə giriş vəziyyətinə görə fərqli yerə gedir. Məntiq MVC `Helpers/NavLinks.cs`-dədir (bax `12`).

## 4. Şablon: yeni siyahı modeli əlavə etmək (5 addım)

(Bu addımlar daha qısa halda `docs/mini-project-vs-caspian.md`-də də var.)

1. **Domain**: `Entity` + `Configuration` + `DbSet` + migration (bax `02`).
2. **Repository**: `IXRepository` + `XRepository` + DI qeydiyyatı (bax `03`).
3. **Service**: DTO-lar (`XDto`, `CreateXDto`, `UpdateXDto`) + validator-lar + `IXService`/`XService` + DI (bax `04`).
4. **API Controller** (`Controllers/Admin`): `[Route("api/admin/x")]`, `[Authorize(Roles=...)]`, 5 endpoint. Ziyarətçi üçün `HomeController`-ə oxuma endpoint-i.
5. **MVC**: `Areas/Admin/Controllers/XController : ApiControllerBase` (Index, Detail, Create, Edit, Delete), ViewModel-lər (`XVM`, `XCreateVM`, `XEditVM`, `XDetailVM`, `XUIVM`), View-lar, sidebar linki; Home üçün `XViewComponent` + View (bax `12` və `14`).

## 5. Bir sorğunun tam yolu: "Brand Create"

1. **MVC `Brand/Create.cshtml`**: forma `multipart/form-data` ilə `BrandController.Create`-ə POST olunur.
2. **MVC `BrandController.Create(BrandCreateVM)`**: ad və 2 MB limiti yoxlanır; `MultipartFormDataContent` qurulur; `PostFormAsync("api/admin/brands", ...)` çağırılır (`ApiControllerBase`). Token cookie-dən avtomatik əlavə olunur.
3. **API `BrandController.CreateBrand([FromForm] CreateBrandDto)`**: `[Authorize(Roles = ...)]` yoxlanır; `_brandService.CreateAsync(request)`.
4. **`BrandService.CreateAsync`**: validator; `FileService.UploadFileAsync(file, "brands")` şəkli yoxlayıb yazır və yolu qaytarır; `_brandRepo.AddAsync(brand)`. Baza xəta versə şəkil silinir.
5. **`BaseRepository.AddAsync`**: `Add` + `SaveChanges`.
6. **Cavab:** API `200`; xətada `ExceptionHandlingMiddleware` `{ isSuccess:false, errors:[...] }`. MVC xətanı formada göstərir; uğurda siyahıya yönləndirir və flash mesaj ("was added") göstərir.

## 6. Home-un oxunması (ziyarətçi tərəfi)
`Views/Home/Index.cshtml` bölmələri `ViewComponent` çağırışları ilə doldurur (`@await Component.InvokeAsync("Brand")` və s.). Hər ViewComponent `HttpClient` ilə `api/home/...` çağırır; API işləmirsə (`HttpRequestException`) bölmə boş/statik qalır, səhifə yenə açılır. Bax `12-mvc-architecture.md`.

## Özünü yoxla

1. Siyahı modeli ilə tək-yazılı model arasındakı fərq nədir? Niyə Hero-da Create/Delete yoxdur?
2. Düymə ünvanı niyə dropdown-dur? Əl ilə yazılsa hansı risk var?
3. Niyə Home-da maksimum 3 kart dizaynı var? 4-cü əlavə olunmağa çalışılanda nə baş verir?
4. İstifadədə olan kart dizaynını silməyə çalışsaq iki qoruma hansılardır?
5. `Setting` niyə açar-dəyər cədvəlidir? Yeni ayar üçün migration lazımdırmı?
6. Loqo niyə mətnlə dəyişdirilmir?
7. Yeni siyahı modelinə (məs. "Testimonial") 5 addımda nə yazarsan?
