# 102. İşçilərin idarəsi (Staff management)

Yalnız **SuperAdmin** açır (API `api/admin/staff`, MVC `/Admin/Staff`). Admin rol verə və ala bilmir.

## Qaydalar
- İşçi = Customer rolundan başqa rolu olan istifadəçi. Hər işçinin **tək** rolu var. Verilə bilən rollar: Admin, Accountant, CustomerSupport, Security (`StaffRules.AssignableRoles`). SuperAdmin verilmir və alınmır.
- **Əlavə etmə:** ad, soyad, iş emaili, rol. Hesab təsadüfi uzun şifrə ilə yaradılır (heç kimə göstərilmir), `EmailConfirmed = true`. İşçiyə **şifrə seçmə linki** emaillə gedir (mövcud `Account/ResetPassword` səhifəsi, token 30 dəqiqə). Vaxtı keçibsə işçi "Forgot password" ilə özü yeni link ala bilər. SMTP qurulmayıbsa (Development, `ExposeCodes`) link SuperAdmin-ə ekranda göstərilir.
- **Rol dəyişmə:** köhnə rol silinir, yenisi verilir (rolsuz qalmasın deyə xəta olsa köhnə qaytarılır).
- **Deaktiv / aktiv:** `AppUser.IsRestricted` bayrağı istifadə olunur (müştəri dondurmasının eynisi): login bloklanır, səbəb "Deactivated by the super admin", kim və nə vaxt yazılır.
- SuperAdmin hesabına və öz hesabına toxunmaq olmaz (özünü bloklamaq və ya yetkisini itirmək mümkün deyil). Müştəri bu API ilə dəyişdirilə bilmir (404).
- Hər əməliyyat audit-ə yazılır: `StaffAdded`, `StaffRoleChanged`, `StaffDeactivated`, `StaffActivated`.

## Açıq tokenin etibarsız edilməsi
JWT 60 dəqiqə yaşayır, ona görə deaktiv edilən və ya rolu dəyişən işçinin köhnə tokeni əvvəl 60 dəqiqəyə qədər köhnə yetkiylə işləyərdi. `Program.cs`-də default JWT sxeminin `OnTokenValidated` hadisəsi **işçi tokenlərini** hər sorğuda bazaya qarşı yoxlayır: hesab dondurulubsa və ya bazadakı rollar tokendəki rollarla uyğun deyilsə 401 qaytarılır. MVC 401 alanda istifadəçini çıxarır (`SessionExpiredAsync`). Müştəri tokenləri bu yoxlamadan keçmir (əlavə baza sorğusu olmasın deyə); onların dondurulması əməliyyatlarda yoxlanır.

## Fayllar
`Repository/Repositories/StaffRepository.cs` (UserManager və roles), `Service/Services/StaffService.cs` (qaydalar, dəvət emaili), `Service/Helpers/Validators/Staff/StaffValidators.cs`, `Controllers/Admin/StaffController.cs`; MVC: `Areas/Admin/Controllers/StaffController.cs`, `Areas/Admin/Views/Staff/Index.cshtml`, `ViewModels/Staff/StaffVMs.cs`.

## Test olunanlar
Siyahı, Admin üçün 403, əlavə etmə, təkrar email, yanlış rol (SuperAdmin), rol dəyişmə, öz və SuperAdmin hesabına toxunma, deaktivdən sonra login və açıq token (401), yenidən aktiv, rol dəyişəndən sonra köhnə token (401).
