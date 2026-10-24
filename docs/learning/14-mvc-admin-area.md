# 14. Admin Area (MVC)

Qovluq: `CaspianBank-MVC-FinalProject/Areas/Admin/`. Bu hissə işçilərin istifadə etdiyi idarəetmə panelidir.

## 1. Nəzəriyyə

### 1.1 Area nədir?
Böyük MVC layihəsində controller/view-ları qruplara bölmək üçün. Bizdə iki dünya var: müştəri saytı (kök) və `Admin` Area. Area-nın üç tələbi:
1. Controller-də `[Area("Admin")]`.
2. View-lar `Areas/Admin/Views/...` qovluğunda.
3. Marşrut: `{area:exists}/{controller}/{action}/{id?}` (`Program.cs`-də default marşrutdan **əvvəl**).

Link yazanda `asp-area="Admin"` mütləqdir. Admin-dən müştəri saytına keçəndə (məs. Logout) `asp-area=""`.

### 1.2 Explicit `[Area]` və `[Authorize]`
Hər admin controller-də **açıq** yazılır:
```csharp
[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class BrandController : ApiControllerBase
```
Ümumi bazadan miras almaq əvəzinə hər controller-də göstərilir: kim hara girə bilər, controller-ə baxanda dərhal görünür.

### 1.3 Rol qoruması iki qatda
- **MVC**: səhifənin özünü qoruyur (`[Authorize(Roles)]`), yanlış rol `AccessDenied`.
- **API**: məlumatı qoruyur (`api/admin/...` da `[Authorize(Roles)]`).
İkisi eyni rolları göstərməlidir. Əgər kimsə MVC-ni keçib API-yə birbaşa müraciət edərsə, API yenə rədd edir.

## 2. Panelin strukturu

`Areas/Admin/Views/Shared/_AdminLayout.cshtml`: iki rejim (yuxarıdakı tab-lar):
- **UI**: saytın görünüşü (Home məzmunu, kart dizaynı, kart növləri, ayarlar).
- **Bank**: bank əməliyyatları (Dashboard, Users, Loans, Reports, Contact Tickets, Audit Log).

Hansı rejimdə olduğu aktiv controller adına görə təyin olunur (`isUi`). Sidebar linkləri `On("Controller")` köməkçisi ilə aktiv sinfi alır.

### 2.1 UI bölməsi (API ilə tam qoşulub)

| Səhifə | Controller | API | Rollar | Növ |
|---|---|---|---|---|
| Dashboard | `UiDashboardController` | - | hamı işçi | statik giriş |
| Hero text | `CardHeroController` | `card-hero` | Admin/SuperAdmin | tək |
| Brands | `BrandController` | `brands` | eyni | siyahı, şəkil |
| Ticker | `TickerController` | `tickers` | eyni | siyahı |
| Benefits heading | `BenefitSectionController` | `benefit-section` | eyni | tək |
| Benefit items | `BenefitItemController` | `benefit-items` | eyni | siyahı + dropdown |
| Services heading | `ServiceSectionController` | `service-section` | eyni | tək |
| Service cards | `ServiceItemController` | `service-items` | eyni | sabit siyahı, ikon |
| About text | `AboutController` | `about` | eyni | tək + `EditVideo` |
| About pillars | `AboutPillarController` | `about-pillars` | eyni | siyahı, şəkil |
| Card designs | `CardDesignController` | `card-designs` | eyni | siyahı, şəkil, önizləmə (`_CardPreview`) |
| Card types | `CardTierController` | `card-tiers` | **Accountant**/Admin/SuperAdmin | sabit 4 |
| Settings | `SettingController` | `settings` | Admin/SuperAdmin | açar-dəyər + logo |

### 2.2 Bank bölməsi (hələlik statik dizayn)
`BankDashboard`, `Users`, `Loans`, `Reports`, `Tickets`, `Audit`: controller-lər yalnız `View()` qaytarır, View-lar nümunə sətirlərlə **statik dizayndır** (`wwwroot/admin/js/admin-*.js` demo davranışı). Bunlar həmin funksiyalar (kredit, hesabat, ticket...) API-də yazılanda real məlumata qoşulacaq. Hamısı `[Authorize(Roles = AppRoles.Staff)]` ilə qorunur.

## 3. CRUD səhifəsinin şablonu (Brand misalı)

Controller metodları (`BrandController`):

| Action | Metod | Nə edir |
|---|---|---|
| `Index` | GET | `GetAsync<List<BrandVM>>("api/admin/brands")` → siyahı |
| `Detail(id)` | GET | tək brend; yoxdursa `NotFound()` (404 səhifəsi) |
| `Create` | GET | boş forma (`BrandCreateVM`) |
| `Create(model)` | POST | yoxla → API-yə `multipart` → uğurda `Success` flash + `Index` |
| `Edit(id)` | GET | API-dən oxu, `BrandEditVM` doldur |
| `Edit(id, model)` | POST | yoxla → API-yə `PUT multipart` |
| `Delete(id)` | POST | `DeleteAsync` + flash |

**POST-un içində ardıcıllıq:**
1. Fayl ölçüsü yoxlanılır (2 MB) — API-nin limiti ilə eyni; böyük faylı boş yerə göndərmirik.
2. `ModelState.IsValid` (ViewModel-dəki `[Required]`, `[StringLength]`).
3. `MultipartFormDataContent`: mətn sahələri `StringContent`, fayl `StreamContent` (`Content-Type` müştəridən; real tip API-də faylın içindən təyin edilir).
4. API cavabı:
   - `Unauthorized` → `SessionExpiredAsync()`.
   - `NotFound` (Edit/Delete) → "This brand no longer exists." flash.
   - Xəta → `ModelState.AddModelError(string.Empty, error)` ilə formada göstərilir.
5. Uğur → `TempData["Success"] = "... was added."` → `RedirectToAction(nameof(Index))` (PRG).

**Edit-də xüsusi hal:** şəkil seçilməyibsə heç nə göndərilmir, API köhnə şəkli saxlayır. Xəta olub formu yenidən göstərəndə cari şəkil də görünməlidir: `EditViewAsync` yolu yenidən API-dən oxuyur. `CurrentImage` property-si `[BindNever]` ilə işarələnib (formadan oxunmur: müştəri öz yolunu göndərə bilməsin).

### 3.1 Delete düyməsi
```html
<form method="post" asp-action="Delete" asp-route-id="@item.Id">
  <button type="submit" class="btn btn-danger btn-sm"
          data-confirm-title="Delete this brand?" data-confirm-text="..." data-confirm-yes="Delete" data-confirm-danger="true">Delete</button>
</form>
```
`confirm-dialog.js` bu `data-confirm-*` atributlarını tapıb təsdiq pəncərəsi göstərir; "Yes" olarsa forma həmin düymə ilə göndərilir.

### 3.2 Fayl seçmə sahəsi
`[data-file-drop]` bloku + `file-drop.js`: önizləmə, fayl adı/ölçüsü, sürüklə-burax, tip/ölçü xətası. Şəkil üçün default (PNG/JPEG/WebP, 2 MB). Video üçün atributlarla: `data-kind="video" data-types="video/mp4,video/webm" data-max-bytes="52428800"`.

## 4. Xüsusi səhifələr

### 4.1 Card types (`CardTierController`)
Pul qaydaları rəqəmlərdir: mətn kimi qəbul edilir ("0,5" və "0.5"), `ParseAmount` ilə `decimal`-a çevrilir (`InvariantCulture`), xəta olarsa `ModelState`-ə yazılır; aralıq/onluq yoxlaması API-dədir. Edit səhifəsi dizayn siyahısını `api/admin/card-tiers/designs`-dən alır (Accountant `card-designs` endpoint-inə girə bilmir, ona görə ayrıca endpoint var).

### 4.2 Card designs
Home-da maksimum 3 dizayn göstərilir (bax `08`). `_CardPreview` partial-ı kartın bazadakı görünüşünü canlı göstərir. İstifadədə olan dizayn silinmir (API 400 qaytarır, MVC mesajı göstərir).

### 4.3 Benefit items
Düymə ünvanı `<select>`-dir; seçimlər `api/admin/benefit-items/destinations`-dən (`ButtonDestinationVM`) gəlir.

### 4.4 About video
`EditVideo` səhifəsi: `PUT api/admin/about/{id}/video` (multipart), 50 MB-a qədər MP4/WebM. Brauzer tərəfdə `file-drop.js` tip və ölçünü əvvəlcədən yoxlayır (yalnız rahatlıq üçün; real yoxlama API-dədir).

### 4.5 Settings
Hər açar bir sətir; `SettingLabels` açarın oxunaqlı adını və izahını verir. Logo açarı üçün `Edit` View-u mətn əvəzinə şəkil yükləmə formu göstərir (`isLogo` şərti) və `EditLogo` action-ına göndərir (`PUT api/admin/settings/{id}/logo`); `FooterDescription` çoxsətirli sahədir (`IsLong`).

## 5. Yeni admin səhifəsi əlavə etmək (qısa siyahı)
1. API controller + servis hazır olmalıdır.
2. ViewModel-lər: `XVM`, `XDetailVM`, `XCreateVM`, `XEditVM`.
3. `Areas/Admin/Controllers/XController : ApiControllerBase` (`[Area]`, `[Authorize(Roles=...)]`).
4. View-lar: `Index`, `Detail`, `Create`, `Edit` (`Areas/Admin/Views/X/`).
5. `_AdminLayout.cshtml` sidebar-ına link.
6. İstəyə görə `AdminSections.All`-a sətir (kim görür cədvəli).
7. Qayda: mövcud `Areas/` fayllarına toxunmadan yenisini əlavə et.

## Özünü yoxla

1. Area nədir və `asp-area` niyə lazımdır?
2. Rol niyə həm MVC-də, həm API-də yoxlanılır?
3. Brand `Edit` zamanı şəkil seçilməsə nə olur? `CurrentImage` niyə `[BindNever]`-dir?
4. Delete düyməsi təsdiq pəncərəsini necə açır?
5. Niyə Card types səhifəsi API-də ayrıca `designs` endpoint-i çağırır?
6. Bank bölməsinin səhifələri niyə hələ statikdir?
7. POST-dan sonra niyə redirect edilir?
