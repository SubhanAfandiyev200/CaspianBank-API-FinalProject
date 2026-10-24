# 106. Testlər (xUnit)

## Necə işlədilir
```
cd Tests
dotnet test
```
Testlər həqiqi SQL Server-də müvəqqəti baza yaradır (EnsureCreated). Server adı fərqlidirsə `CASPIAN_TEST_SQL` mühit dəyişəni ilə connection string verilir.
API işləyirsə (bin faylları kilidlidir) çıxışı başqa qovluğa yönəldin: `dotnet test -p:OutDir=<başqa qovluq>\`.

## Quruluş
- `Tests/Domain` — kredit annuiteti, cərimə, depozit, komissiya qaydaları (saf hesablama).
- `Tests/Validators` — bütün FluentValidation qaydaları.
- `Tests/Services` — hər servis real bazada (Transfer, Card, Auth, Loan, Deposit, Bill, Recurring*, Security, Staff, Report, Support, CMS və s.).
- `Tests/Api` — ExceptionHandlingMiddleware (404/400/500) və controller təhlükəsizlik qaydaları (reflection ilə).
- `Tests/Infrastructure` — TestApp (DI konteyneri), TestData, paylaşılan və təcrid olunmuş baza.

## Paylaşılan və təcrid olunmuş baza
Adi testlər paylaşılan bazada hər biri öz unikal məlumatı ilə işləyir. Qlobal cəmlər, fon işlər və tək-sətirli CMS üçün ayrıca baza (`IsolatedApp`).

## Testlərin tapdığı real xətalar (düzəldildi)
1. Uğurlu girişdən sonra kilid (lockout) tam sıfırlanmırdı — `AccountRepository.ResetFailedAttemptsAsync`.
2. Kodla kredit ödənişində müqavilə nömrəsi kiçik hərflə yazılanda tapılmırdı — `LoanService.PayByCodeAsync`.
3. Enum sahələrinə rəqəm (məs. "7") göndərmək keçərli sayılırdı — `EnumRules.IsName`.
4. `POST api/account/register` rate limiting-siz idi — `[EnableRateLimiting("auth")]` əlavə edildi.

## Sonradan əlavə olunanlar (2026-10-24)
- **Sayı:** 1077 test. Normal işləmə ~50 saniyə. SQL Server yüklü olanda 1.5 dəqiqəyə qədər uzana bilir və ara-sıra 1 test vaxt aşımına düşə bilir: təkrar işlət.
- `Tests/Services/TopUpFromCardTests.cs`: top-up Caspian kartından (pul çıxır, balans çatmasa dəyişən heç nə olmur, yanlış bitmə tarixi, bloklu kart, eyni kart, Cashback mənbə, tapılmayan kart, bildiriş).
- `Tests/Services/BlacklistedLoanApprovalTests.cs`: qara siyahıda iki təsdiq (birinci təsdiq pulu göndərmir, Super Admin son təsdiqi verir, rolların qadağaları, müştəri `Pending` görür, siyahı nişanı).
- `Tests/Api/`: `ExceptionHandlingMiddleware` və controller təhlükəsizliyi (reflection: hər Admin controller rolludur, Client controller-lər açıq siyahıdan başqa girişlidir, pul köçürən POST-lar rate limit altındadır).
- Testlər API-ni də qurur (`ProjectReference`), ona görə API işləyəndə `bin` kilidlənir: `-p:OutDir=<başqa qovluq>\` işlət.
- Tapılan xətalar: lockout tam sıfırlanmırdı, kodla kredit ödənişində kiçik hərf, enum sahəsinə rəqəm, `register` rate limit-siz idi.
