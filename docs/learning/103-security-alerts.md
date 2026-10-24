# 103. Şübhəli fəaliyyət (Security)

`Security` roluna ("şübhəli hesabatları idarə edir") indi real iş var. Səhifə: MVC `/Admin/Security` (menyuda "Suspicious activity"), API `api/admin/security/alerts`. Security, Admin və SuperAdmin görür.

## Necə işləyir
1. **Sistem siqnal yaradır.** Müştəri başqasına pul göndərəndən sonra `TransferService` `ISecurityMonitor.EvaluateTransferAsync`-i çağırır. Qaydalar yalnız **başqasına** (başqa müştəriyə və ya başqa banka) göndərilən pula baxır; öz kartları arasında köçürmə (təsviri "Transfer to your..." ilə başlayır) sayılmır.
2. **Heç nə avtomatik bloklanmır.** Siqnal yalnız yazılır və Security, Admin, SuperAdmin-ə bildiriş gedir. Qərarı insan verir.
3. **İşçi qərar verir:** "Mark safe" (qeyd istəyə bağlı) və ya "Freeze" (səbəb məcburidir, 5-300 simvol; müştəri hesabı mövcud dondurma ilə eyni qaydada donur, "Security: səbəb" kimi yazılır). Siqnal `Open → Dismissed` və ya `Frozen` olur, kim və nə vaxt yazılır.
4. **Audit log-a yazılır:** `SecurityAlertDismissed`, `SecurityAlertFrozen` (dondurma ayrıca `AccountRestricted` kimi də yazılır).

## Qaydalar (`Domain/Security/SecurityRules.cs`, hədləri dəyişmək üçün yalnız bura)
| Siqnal | Şərt | Ciddilik |
|---|---|---|
| LargeTransfer | tək köçürmə 3 000 AZN və ya çox | 8 000+ yüksək, qalanı orta |
| RapidTransfers | 10 dəqiqədə 5 köçürmə | orta |
| DailyVolume | Bakı günü ərzində cəmi 15 000 AZN və ya çox | yüksək |
| NewAccountOutflow | 7 gündən gənc hesab 1 000 AZN və ya çox göndərir (böyük köçürmə siqnalı artıq varsa ikinci yazılmır) | orta |
| AccountLockout | təkrar yanlış şifrədən sonra müştəri hesabı bloklandı (`AccountService` yoxlayır) | orta |

**Təkrar siqnalın qarşısı:** `(Kind, Reference)` unikal indeksdir. Referans köçürmənin kodudur (TR-...), pəncərəli qaydalarda isə açardır (`rapid:{user}:{pəncərə}`, `daily:{user}:{gün}`, `lock:{user}:{saat}`). Eyni hadisə ikinci siqnal yaratmır.

## Detallar
- `SecurityMonitor` xəta atsa, köçürməni pozmur (try/catch və log).
- Köçürmə sayı və cəmi `Transaction` jurnalından hesablanır (`SecurityRepository`).
- Siyahı: açıq siqnallar əvvəldə, onlarda yüksək əvvəl, sonra ən yeni. Süzgəc: vəziyyət, ciddilik, növ, axtarış (müştəri, email, kod, izah). Səhifədə 3 kart: açıq, yüksək, son 7 gündə baxılan.
- Dondurma yalnız müştəri hesabına aiddir (işçi 404 alır; işçi blokları siqnal yaratmır). Artıq dondurulmuş hesabda "Freeze" düyməsi göstərilmir.
- Migration `AddedSecurityAlerts`.

## Fayllar
`Domain/Entities/SecurityAlert.cs`, `Domain/Security/SecurityRules.cs`, `Repository/Repositories/SecurityRepository.cs`, `Service/Services/SecurityMonitor.cs` (qaydalar), `Service/Services/SecurityAlertService.cs` (siyahı, qərar), `Controllers/Admin/SecurityController.cs`; MVC: `Areas/Admin/Controllers/SecurityController.cs`, `Areas/Admin/Views/Security/Index.cshtml`.

## Test olunanlar
Böyük köçürmələr (orta və yüksək), gündəlik cəm, 5 köçürmə qaydası, yeni hesab qaydası, yanlış şifrə ilə bloklanma siqnalı, siyahı və süzgəc, "mark safe", ikinci dəfə baxmağa icazə verilməməsi, səbəbsiz dondurma rədd, dondurma sonrası loginin bloklanması, CustomerSupport üçün 403.
