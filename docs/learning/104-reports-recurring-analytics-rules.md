# 104. Şübhəli əməliyyat bildirişi, təkrarlanan köçürmə, xərc analitikası, bankın qaydaları

Bu sənəd 2026-10-21-də yazılan dörd xüsusiyyəti izah edir. Hamısı eyni qəlibdədir: `Domain` (entity + configuration) → `Repository` → `Service` (qaydalar burada) → `Controller` (nazik) → MVC səhifəsi.

## 1. Şübhəli əməliyyatı bildirmək (Reports)

**Məqsəd.** Müştəri tarixçədə əməliyyatın yanında "Report" düyməsini basır, səbəbini yazır. İşçi (Security, Admin, SuperAdmin) baxır və qərar verir.

**Cədvəl:** `TransactionReports` (`TransactionReport`): `UserId`, `TransactionId`, `Reason`, `Status` (`Pending`, `Reviewed`, `ActionTaken`, `Dismissed`), `ResolvedBy`, `ResolvedAt`, `ResolutionNote`, `ActionTaken`. Bir əməliyyat **yalnız bir dəfə** bildirilir (`TransactionId` üzrə unikal indeks).

**Qaydalar (`ReportService`):**
- Müştəri yalnız **öz** kartından keçən əməliyyatı bildirə bilər; səbəb ən azı 10 simvol.
- İşçi "Mark reviewed" (baxıldı), "Dismiss" (əsassız) və ya "Take action" (tədbir) seçir. Tədbir üçün səbəb məcburidir.
- **Tədbir nə edir:** pul başqa Caspian müştərisinə gedibsə, **alan hesabı dondurur** (`IUserAdminService.RestrictAsync`); başqa banka gedibsə, **bildirən müştərinin kartını bloklayır**. Cashback kartı bloklanmır.
- Qərardan sonra müştəriyə bildiriş (`Your report was reviewed`) göndərilir, audit-ə `ReportReviewed`, `ReportActionTaken` və ya `ReportDismissed` yazılır.

**Endpoint-lər:** `POST api/reports`, `GET api/reports` (müştəri); `GET api/admin/reports`, `POST api/admin/reports/{id}/resolve` (işçi; qərar yalnız SuperAdmin/Admin/Security).

**MVC:** `ReportController` (`/App/Report`), `Views/Report/Index` (forma) və `List` (müştərinin bildirişləri), admin tərəfdə `Areas/Admin/.../ReportsController` (siyahı, süzgəc, qərar düymələri).

## 2. Təkrarlanan köçürmə (Recurring transfers)

**Məqsəd.** "Hər ayın 5-i kirayəni göndər." Bölmə: `/App/Recurring` (Transfers səhifəsindən link).

**Cədvəl:** `RecurringTransfers`: mənbə kart, alıcı kart nömrəsi, məbləğ, ayın günü, `IsActive`, `LastPeriod` (ən son hansı ay üçün göndərilib), `LastError`.

**Qaydalar (`RecurringTransferRules`, `RecurringTransferService`):**
- Gün **1–28** (hər ayda var). İlk köçürmə növbəti belə gündə olur.
- Yaradanda köçürmə **yoxlanılır** (`PreviewAsync` ilə məbləğ 1): alıcı kart, limit, bloklanmış hesab və s. Səhv varsa yaradılmır.
- Dayandırmaq (`Pause`), davam etdirmək (`Resume`) və silmək olar.
- `BillingMaintenanceService` hər gün `RunDueAsync` çağırır. Vaxtı çatan köçürmə **adi köçürmə kimi** (`TransferService.TransferAsync`) icra olunur: komissiya, limit, təhlükəsizlik yoxlaması eynidir.
- **Təkrar göndərilməsin deyə:** `RequestId` = MD5(köçürmə id, ay). Eyni ay üçün ikinci cəhd idempotentlik qaydası ilə dayanır. Alınmasa `LastError` yazılır, sabah yenidən cəhd olunur, müştəriyə bildiriş gedir.
- Dərs: `TransferAsync` EF izləyicisini təmizləyir, ona görə sonra `LastPeriod`-u yazmazdan əvvəl sətir **yenidən oxunur** (əks halda dəyişiklik itirdi).

## 3. Xərc analitikası (Spending insights)

**Məqsəd.** Pulum hara getdi? `/App/Spending`, API: `GET api/analytics/spending?months=6`.

- `AnalyticsRepository` yalnız müştərinin kartlarından **çıxan** əməliyyatları verir (`OutgoingRow`); öz kartları arasındakı köçürmə **sayılmır**.
- `AnalyticsService` onları kateqoriyalara bölür: **Transfers, Bills, Loans, Fees (komissiya və kart haqqı), Deposits** (qənaət: xərc deyil, amma kartdan çıxır).
- Ay Bakı vaxtı ilə (UTC+4) hesablanır, məsələn 31 oktyabr gecəsi noyabra düşmür.
- Səhifədə: bu ayın cəmi, keçən ayla müqayisə, kateqoriya zolaqları (`.cat-1..5`), aylıq sütunlar. Dövr seçimi 3/6/12 ay.

## 4. Bankın qaydaları (Bank rules)

**Məqsəd.** Xəzinə və kredit qaydalarını koddan çıxarıb admin səhifəyə qoymaq: `/Admin/BankRules` (SuperAdmin, Admin, Accountant).

- Dəyişənlər: məcburi ehtiyat faizi, mənfəət vergisi, aylıq əməliyyat xərci, banklararası maksimum məbləğ, kredit (min/max məbləğ, illik faiz, sabit cərimə, gündəlik cərimə faizi, aktiv kredit sayı, təklifin müddəti).
- `BankRulesService` dəyərləri bankın öz sətrindən (`OwnBank`) oxuyur/yazır, `BankRulesDtoValidator` aralıqları yoxlayır (məsələn ehtiyat və vergi 0–100%, kredit faizi 0.1–100%, aktiv kredit sayı 1–20, təklif 1–60 gün). Hər dəyişiklik audit-ə `BankRulesChanged` kimi düşür (nəyin nədən nəyə dəyişdiyi ilə).
- **Tələsik dəyişməyin:** ReservePercent `decimal(5,2)` sütunudur; SQL ilə əl ilə dəyişəndə səssiz uğursuzluq ola bilər, ona görə həmişə səhifədən dəyişin.

## 5. Kiçik əlavələr (eyni gün)

- **Hissə-hissə kredit ödənişi (kodla):** `PayByCodeDto.Amount` (boş = bütün borc). Validator məbləği yoxlayır, `LoanService` əvvəl cərimə və faizi, sonra əsas borcu ödəyir. Bills səhifəsində kredit kodu yazanda "Amount" sahəsi və "All due" çipi çıxır.
- **Dəstək işçisi üçün müştəri axtarışı:** `UserAdminRepository` axtarışı FİN, telefon və **kart nömrəsi** üzrə də işləyir.
