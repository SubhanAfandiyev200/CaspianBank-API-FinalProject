# 97. Kreditlər: müraciət, qərar, ödəniş, gecikmə cəriməsi

Bu sənəd kredit funksiyasını sıfırdan başa düşmək və sonra özün yenidən yazmaq üçündür. Əvvəl qaydalar (nəzəriyyə), sonra fayllar, sonra axınlar, ən sonda müdafiə sualları.

## 1. Qaydalar (hamısı bank sətri `OwnBank` və ya `LoanEngine`-dədir)

| Qayda | Dəyər |
|---|---|
| Faiz | 10% illik (annuitet: hər ay eyni məbləğ) |
| Müddət | 6, 12, 18, 24, 36, 48 ay |
| Məbləğ | ən azı 500, ən çoxu 50 000 AZN |
| Maaşa görə hədd | maaş × 24 (xarici bankda 0 gecikmə), × 12 (1-2 gecikmə), × 4 (3 və çox gecikmə), 50 000-dən çox olmaz |
| Aylıq ödəniş həddi | bütün aylıq ödənişlər maaşın 45%-indən çox olmasın (işçiyə xəbərdarlıq, məcburi bloklama yox) |
| Açıq kredit sayı | ən çox 3 (müraciət anında Pending/Offered/Active/Court, təsdiq anında Active/Court sayılır) |
| Ödəniş günü | hər ayın **son günü**; ilk ödəniş krediti verildiyi aydan **sonrakı** ayın son günüdür |
| Cərimə | gecikmənin 1-ci günündən sabit 10 AZN + ödənilməmiş hissənin hər gün üçün 0,5%-i (cərimənin üstünə cərimə gəlmir) |
| Ödəniş sırası | əvvəl cərimə, sonra faiz, sonra əsas borc |
| Məhkəmə | 90 gün gecikmədən sonra kredit `Court` olur |
| Kart | Cashback kartına kredit pulu düşmür və o kartdan kredit ödənmir; bloklanmış kart olmur |
| Qara siyahı | müraciət qəbul olunur, amma pul iki təsdiqdən sonra gedir: əvvəl kredit şöbəsi, sonra Super Admin (bax 3a) |
| Rədd səbəbi | **daxilidir**: müştəri həmişə "We can't approve this application right now. You can apply again later." görür |

### Azaldılmış təklif (Offered)
İşçi istənilənin eynisini təsdiq edərsə kredit dərhal verilir (`Active`, pul karta düşür). Azını təsdiq edərsə kredit `Offered` olur: müştəriyə 7 gün (`OfferValidDays`) verilir, o razılaşsa kredit aktivləşir (kartı dəyişə bilər), imtina etsə və ya vaxt bitsə `Declined` olur.

### Vaxtından əvvəl ödəniş
- **Hissəvi** (ən azı `MinEarlyPayment` = 10 AZN): əsas borc azalır, **müddət dəyişmir**, aylıq ödəniş azalır. Məbləğ qalan əsas borcun 10%-i və çoxudursa faiz 1 punkt düşür (ən azı 5%).
- **Tam bağlama**: gecikmiş hər şey (cərimə + faiz + əsas borc) + gələcək əsas borc + cari ayın **mütənasib** yığılmış faizi. Gələcək faizlər silinir, ödənilməmiş taksitlər `Prepaid` (ləğv) olur, kredit `Closed` + `ClosedEarly` olur.
- Erkən ödənişi yalnız kreditin sahibi edir. Aylıq taksiti isə ödəniş kodunu (müqavilə nömrəsini) bilən hər kəs ödəyə bilər (Bills üçün).

## 2. Fayllar

**Domain**
- `Enums/LoanStatus.cs` (Pending, Rejected, Offered, Declined, Active, Closed, Court), `LoanPaymentType.cs` (Installment, PartialEarly, FullEarly), `LoanPurpose.cs`, `TransactionType.cs` (sonuna `LoanDisbursement`, `LoanPayment` əlavə olundu).
- `Entities/Loan.cs`, `LoanInstallment.cs` (hər ay bir sətir: əsas borc, faiz, ödənilən, cərimə), `LoanPaymentRecord.cs` (hər ödənişin cərimə/faiz/əsas borc bölgüsü), `OwnBank.cs` (yeni sütunlar: təklif müddəti, minimum erkən ödəniş, faiz endirimi).
- `Loans/LoanEngine.cs` + `LoanModels.cs`: **bütün hesablama bir yerdədir** (aylıq ödəniş, qrafik, cərimə, ödəniş bölgüsü, tam bağlama məbləği, Court). Saf funksiyalardır (bazaya toxunmur), ona görə konsol testi ilə yoxlanıla bilir.
- `Configurations/Loan*Configuration.cs` (unikal müqavilə nömrəsi, `RowVersion`).

**Repository**
- `Repositories/LoanRepository.cs`: pul hərəkətləri **bir tranzaksiyada** (kart + kredit + əməliyyat sətri). `Results/LoanResult.cs` nəticə növləri.
- Migration `AddedLoans`.

**Service**
- `Services/LoanService.cs` (müştəri), `LoanReviewService.cs` (işçi), `Helpers/LoanMapper.cs`, `LoanMessages.cs`, `Helpers/DTOs/Loans/*`, `Helpers/Validators/Loans/*`.

**API**
- `Controllers/Client/LoansController.cs` (`api/loans`), `Controllers/Admin/LoansController.cs` (`api/admin/loans`, rollar Accountant/Admin/SuperAdmin).
- `Helpers/LoanSeeder.cs` (hazır müştərilər üçün 6 nümunə kredit), `Helpers/LoanMaintenanceService.cs` (hər gün işləyən fon xidməti: vaxtı bitmiş təkliflər, cərimə, Court).

**MVC**
- Müştəri: `Controllers/LoansController.cs`, `Views/Loans/Index|Detail|Receipt.cshtml`, `ViewModels/Loans/LoanVMs.cs`, `wwwroot/js/loans.js`, `Helpers/LoanUi.cs` (mətn və rəng köməkçiləri).
- İşçi: `Areas/Admin/Controllers/LoansController.cs`, `Areas/Admin/Views/Loans/Index|Detail.cshtml`, `ViewModels/LoanReviews/LoanReviewVMs.cs`.
- History: `_ActivityRow.cshtml` (ikon) və `History/Index.cshtml` (filtr: "Loan received", "Loan payment").

## 3. Axınlar

**Müraciət:** MVC forma → `POST api/loans` → validator (məbləğ, müddət, məqsəd, "Other" olanda izah) → xidmət: qara siyahı? açıq kredit sayı? hədəf kart yararlıdır? → `Pending` (qara siyahıda `RequiresSuperAdminApproval` qoyulur, iki təsdiq lazımdır: bax 3a).

**Baxış:** işçi `GET api/admin/loans/{id}`: reyestr məlumatı, iş yerləri, xarici bank kreditləri (gecikmə, məhkəmə), Caspian kreditləri və qaydaların hesabı (`Rules`: maaşa görə hədd, ödəniş həddi, tövsiyə olunan maksimum, xəbərdarlıqlar). Qərar işçinindir: tövsiyədən fərqli məbləğ də təsdiq edə bilər.

**Təsdiq:** `POST api/admin/loans/{id}/approve` → məbləğ = istənilən? `Active` + `LoanDisbursement` əməliyyatı : `Offered`. **Rədd:** `POST .../reject` (səbəb ən azı 3 simvol, daxilidir).

**Ödəniş:** `POST api/loans/{id}/pay` `{cardId, type, amount, requestId}`:
1. Kredit və kart oxunur, `LoanEngine` ödənişi hesablayır (taksitlərə, qaydaya görə bölgü).
2. Bir tranzaksiyada kart balansı azalır, taksitlər və kredit yenilənir, `LoanPaymentRecord` və `Transactions` sətri yazılır.
3. Eyni vaxtda iki ödəniş gəlsə `RowVersion` toqquşur: sorğu 8 dəfəyə qədər təkrar oxunub yenidən hesablanır.
4. **İdempotentlik:** `RequestId`-dən `LP-<16 hex>` istinadı düzəlir. `Transactions`-da `(Reference, Type)` unikal indeksdir: eyni sorğu iki dəfə gəlsə (ikiqat klik, yeniləmə) pul bir dəfə çıxır, ikinci dəfə əvvəlki qəbz qaytarılır.

**MVC-də ödəniş:** forma `POST App/Loans/{id}/Pay` → uğurda qəbz `TempData`-ya yazılır və `Receipt` səhifəsinə **redirect** olur (PRG: səhifəni yeniləmək ödənişi təkrar göndərməsin).


## 3a. Qara siyahıdakı müştəri: iki təsdiq (2026-10-24)
Əvvəl qara siyahıdakı müraciət dərhal rədd olunurdu. İndi qəbul olunur və pul **iki təsdiqdən sonra** gedir.

```
Pending ──(kredit şöbəsi: Accountant/Admin təsdiq edir)──► AwaitingSuperAdmin ──(Super Admin təsdiq)──► Active  (və ya azaldılıbsa Offered)
   │                                                              │
   └── rədd (kredit şöbəsi) ──► Rejected                          └── rədd (yalnız Super Admin) ──► Rejected
```
- `Loan.RequiresSuperAdminApproval` müraciətdə qoyulur (`LoanService.ApplyAsync`). Müraciətdən **sonra** qara siyahıya düşənə də eyni qayda tətbiq olunur (`LoanReviewService.ApproveAsync` yoxlayır).
- Birinci təsdiq: `ILoanRepository.FirstApproveAsync` təsdiq edilən məbləği (`FirstApprovedAmount`), kimin təsdiq etdiyini (`FirstApprovedByUserId`) və vaxtı saxlayır, statusu `AwaitingSuperAdmin` edir. **Pul getmir.** Super Admin-lərə "Loan for a blacklisted customer needs your approval" bildirişi gedir.
- Son təsdiq: `ApproveAsync` yalnız `AwaitingSuperAdmin` statusunda (flag varsa) işləyir. Məbləğ **kredit şöbəsinin** təsdiq etdiyi məbləğdir: Super Admin başqa rəqəm göndərsə də dəyişmir.
- Rol qaydaları (servisdə, rollar `IAccountRepository.GetRolesAsync` ilə oxunur): **Super Admin qara siyahılı krediti birbaşa təsdiq edə bilər** (onun təsdiqi son təsdiqdir, ikinci addım yoxdur); kredit şöbəsi (Admin/Accountant) təsdiq edəndə iş Super Admin-ə gedir. Admin/Accountant son təsdiqi verə bilməz və iş Super Admin-ə keçəndən sonra rədd də edə bilməz.
- **Müştəri qara siyahı barədə heç nə bilmir:** `LoanMapper` `AwaitingSuperAdmin` statusunu müştəriyə `Pending` kimi qaytarır (`forStaff: false`). İşçi tərəfində `forStaff: true`.
- Migration: `AddedBlacklistTwoStepLoanApproval`. Testlər: `BlacklistedLoanApprovalTests` (11 test).
- MVC admin: siyahıda qırmızı "Blacklisted" nişanı, amber "Awaiting Super Admin" statusu; detalda hansı addımda olduğunu göstərən lövhə (`approval-banner`) və Super Admin üçün "Final approval" forması.
## 4. Tarix: "bu gün" necədir?
Bakı günü = `UtcNow + 4 saat` (`LoanEngine.BakuToday`). Gecə yarısından sonra UTC hələ əvvəlki gündürsə də müştəri üçün yeni gün başlayıb. Cərimə və ödəniş günü bu tarixlə hesablanır.

## 5. Çətin yerlər və tələlər
- **Pul `decimal`-dır**, `Round` hər addımda 2 onluğa yuvarlayır. Qrafikin sonuncu taksiti qalığı bağlayır (yuvarlaq xətanı sonuncu aya yığır).
- **Area + eyni ad:** MVC-də iki `LoansController` var (müştəri və `Admin` area-sı). Area controller-də `RedirectToAction(nameof(Detail), new { id })` müştəri controller-inə düşür (area düzgün tapılmır). Düzgün: `RedirectToAction(nameof(Detail), "Loans", new { area = "Admin", id })`.
- **Rate limit:** `transfer` siyasəti (dəqiqədə 20) yalnız yazan əməliyyatlara qoyulub (müraciət, razılıq, ödəniş, kod axtarışı). Oxuma sorğuları limitsizdir, yoxsa kredit səhifəsi özü limitə düşərdi.
- **Test haqqında:** API testində "429" xətası gördüm: bu qayda səhv deyildi, sadəcə testin ardıcıl yazan sorğuları pəncərəyə sığmadı.
- Razor-da kod blokunun içində `@foreach` yazılmır (`foreach`), yalnız markup içində `@` işlənir.

## 6. Hazır nümunə kreditlər (`LoanSeeder`)
| Müştəri | Kredit | Vəziyyət |
|---|---|---|
| Tural | KR-2025-000003 | Closed (erkən bağlanıb) |
| Aysel | KR-2025-000012 | Closed |
| Elçin | KR-2025-000041 | Active, faiz 9% (erkən ödənişlə endirilib) |
| Nigar | KR-2026-000007 | Active, 2 gecikmiş taksit və cərimə |
| Rauf | KR-2026-000019 | Offered (5 000 istəyib, 3 000 təklif olunub) |
| Tural | KR-2026-000021 | Pending (işçinin baxışını gözləyir) |

## 7. Gündəlik iş (fon xidməti) və necə sınandı
`LoanMaintenanceService` (BackgroundService) proqram açılandan 30 saniyə sonra, sonra hər saat işləyir. `LoanService.RunDailyMaintenanceAsync` repository-nin `RunMaintenanceAsync` metodunu çağırır:
1. Vaxtı bitmiş `Offered` təkliflər `Declined` olur (`"The offer expired."`).
2. Açıq kreditlərdə gecikmiş ödənişlərə cərimə yazılır (`AccrueAll`): sabit 10 AZN + ödənilməmiş hissənin hər gün 0,5%-i.
3. 90 gün gecikmiş kredit `Court` olur (`MoveToCourtIfNeeded`).
4. Repository həm də "müştəriyə deyilməli" hadisələri qaytarır, servis onları bildirişə çevirir (bax `98-notifications-signalr.md`).

**Real bazada necə sınandı:** üç kredit yaradıldı (üçünü fərqli rollar təsdiqlədi: Admin, SuperAdmin, Accountant), tarixlər SQL ilə geri çəkildi: biri 10 gün gecikmiş, biri 95 gün gecikmiş, biri vaxtı bitmiş təklif. İş işləyəndən sonra: təklif `Declined`, 10 gün gecikmədə cərimə `10.00 + 8.79` (175.83 × 0,5% × 10), Court kreditində cərimələr `62.64` və `39.56`, status `Court`. İş ikinci dəfə işləyəndə nə rəqəm, nə bildiriş təkrarlandı. Court kreditinin gecikmiş məbləği (cərimə daxil 517.81) ödəniləndə əvvəl cərimə, sonra faiz, sonra əsas borc bağlandı.

**Düzəliş:** Court-a keçid əvvəl UTC günü ilə, cərimə isə Bakı günü ilə sayılırdı (gecə 00:00-04:00 Bakı vaxtı arasında bir gün fərq). İndi `MoveToCourtIfNeeded` günü `BakuToday` ilə hesablayır.

**Bilinən qayda:** Court kreditində bütün gecikmə ödənilsə də status `Court` qalır, kredit tam bağlanana qədər. Hissəvi erkən ödəniş Court-da bağlıdır ("A loan in court can only be repaid in full"), aylıq ödəniş isə qəbul olunur.

## 8. Rol yoxlaması
`api/admin/loans`: Accountant, Admin, SuperAdmin 200; CustomerSupport, Security və müştəri 403; girişsiz 401. Üç pul rolu da qərar verə bilir (təsdiq/təklif); qara siyahıda isə birinci təsdiq Accountant/Admin-dir, son təsdiq yalnız SuperAdmin-dir.

## 9. Növbəti işlər
- Bills səhifəsi yazılanda aylıq kredit ödənişi **Recurring payments**-ə əlavə olunmalıdır (API-də `lookup/{code}` və `pay-by-code` hazırdır).

## Özünü yoxla
1. Niyə hesablama `LoanEngine`-də, servisdə yox? Bunun test üçün faydası nədir?
2. Niyə rədd səbəbi müştəriyə göstərilmir, imtina səbəbi isə göstərilir?
3. `RequestId` və `(Reference, Type)` unikal indeksi ikiqat klikdə pulu necə qoruyur?
4. İki ödəniş eyni anda gələrsə `RowVersion` nə edir?
5. Tam bağlama məbləği niyə cari ayın faizini mütənasib hesablayır?
6. Cashback kartı niyə kredit ala və ödəyə bilmir?
7. Bakı günü niyə `UtcNow + 4 saat`-dır və harada işlənir?
8. Niyə area controller-də redirect-ə `area = "Admin"` yazmaq lazım oldu?
