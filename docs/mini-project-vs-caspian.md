# MVC-MiniProject ilə Caspian Bank arasındakı fərqlər

MiniProject-də hər şey **bir web layihəsindədir**. Caspian Bank-da iş **qatlara və iki layihəyə** bölünüb: API (məlumat və qaydalar) və MVC (səhifələr). Aşağıdakı cədvəl MiniProject-də yazdığın kodun burada hara düşdüyünü göstərir.

## 1. Fərqlər

| MiniProject | Caspian Bank | Nəticəsi |
|---|---|---|
| Bir web layihəsi, `IFormFile` və `IWebHostEnvironment` hər yerdə görünür | `Service` ayrı class library-dir (`Service.csproj`-da ASP.NET istinadı əlavə olunub) | Servisdə web tipi lazımdırsa `FrameworkReference` olmalıdır |
| Service birbaşa `DbContext` ilə işləyir | Arada **Repository** qatı var (`IBrandRepository`, `BaseRepository`) | `_brandRepo.AddAsync(...)`. `AddAsync/UpdateAsync/DeleteAsync` özü `SaveChanges` edir |
| Controller `View(model)` qaytarır, xəta `ModelState`-dədir | API controller **JSON** qaytarır | Xəta üçün `BadRequestException` / `NotFoundException` atılır, middleware onları 400/404 JSON-a çevirir |
| Form birbaşa controller-ə gedir | MVC form → `HttpClient` → API controller | Fayl yükləyəndə `multipart/form-data` və API-də `[FromForm]`. `[FromBody]` fayl qəbul etmir (415) |
| `FileService.Upload` fayl adı qaytarır, View `~/images/@Model.Image` yazır | Şəkillər **API-nin** `wwwroot`-undadır | Bazada tam yol (`/images/brands/x.png`), View-da `ApiUrl.ToAbsolute(...)` |
| Fayl adı müştəridən (`Guid_originalAd`) | Ad həmişə təsadüfi, növ faylın içindəki imzadan | `FileService` ölçünü, növü yoxlayır və yalnız öz yüklədiyi faylı silir |
| Giriş yoxdur | JWT + cookie, rollar (`[Authorize(Roles = ...)]`) | API-də və MVC-də hər iki tərəfdə rol yoxlanır |

## 2. Brand-ın bir sorğusu: "Create" düyməsinə basılandan bazaya qədər

1. **MVC `Brand/Create.cshtml`**: forma `multipart/form-data` ilə `BrandController.Create`-ə POST olunur.
2. **MVC `BrandController.Create(BrandCreateVM)`**: ad və 2 MB limiti yoxlanır. `MultipartFormDataContent` qurulur və `PostFormAsync("api/admin/brands", ...)` çağırılır (`ApiControllerBase`). Token cookie-dən avtomatik əlavə olunur.
3. **API `BrandController.CreateBrand([FromForm] CreateBrandDto)`**: `[Authorize(Roles = ...)]` yoxlanır və `_brandService.CreateAsync(request)` çağırılır.
4. **`BrandService.CreateAsync`**: ad yoxlanır, `FileService.UploadFileAsync(file, "brands")` şəkli yoxlayıb `wwwroot/images/brands/<guid>.png` kimi yazır və yolu qaytarır. Sonra `_brandRepo.AddAsync(brand)`. Baza xəta versə şəkil silinir.
5. **`BaseRepository.AddAsync`**: `Add` + `SaveChanges` (bazaya yazılır).
6. **Cavab:** API `200` qaytarır. Xətada `ExceptionHandlingMiddleware` `{ isSuccess:false, errors:[...] }` qaytarır. MVC xətanı formada göstərir, uğurda siyahıya yönləndirir və "was added" mesajı göstərir.

## 3. Yeni model əlavə etmək üçün 5 addım (Brand/Ticker kimi)

1. **DTO-lar** (`Service/Helpers/DTOs/<Model>`): `XDto` (Id daxil), `CreateXDto`, `UpdateXDto`.
2. **Service**: `IXService` + `XService` (`GetAll`, `GetDetail`, `Create`, `Update`, `Delete`). Create/Update/Delete `Task`-dır. Mövcud olmayan id üçün `NotFoundException`, yanlış məlumat üçün `BadRequestException`.
3. **API controller** (`Controllers/Admin`): `[Route("api/admin/<ad>")]`, `[Authorize(Roles = ...)]`, 5 endpoint. Şəkil varsa `[FromForm]` + `[Consumes("multipart/form-data")]`.
4. **MVC**: `XController : ApiControllerBase` (`Index`, `Detail`, `Create`, `Edit`, `Delete`) və `XVM`, `XCreateVM`, `XEditVM`. Yan menyuya link əlavə et (`_AdminLayout`).
5. **View-lar**: `Index`, `Detail`, `Create`, `Edit`. Delete düyməsi `data-confirm-*` atributları ilə `POST` formasındadır.

## 4. Tez-tez rast gəlinən səhvlər

- `CS0246 IFormFile/IWebHostEnvironment not found` → kod `Service`/`Repository` layihəsindədir, web tipi orada yoxdur.
- `415 Unsupported Media Type` → API `[FromBody]`, MVC isə `multipart` göndərir (və ya əksinə).
- `Something went wrong` MVC-də → API-nin cavabı gözlənilən formatda deyil. MVC konsolunda `ApiCall` log-una bax (status və gövdə orada yazılır).
- Şəkil görünmür → View-da `ApiUrl.ToAbsolute(Configuration["ApiSettings:BaseUrl"], path)` yoxdur və ya API işləmir.
- `CS4014 call is not awaited` → servisdə repo çağırışının qarşısında `await` yoxdur (silmə/yazma yarımçıq qala bilər).
- Sıra: **əvvəl baza, sonra fayl silinir**; şəkil dəyişəndə **əvvəl yeni fayl, sonra baza, ən sonda köhnə fayl silinir**.
