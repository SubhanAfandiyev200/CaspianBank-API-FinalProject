# 101. Müddətli depozitlər və xəzinə (treasury)

Bu fayl depozit funksiyasını və onun bankın rəqəmlərinə (dashboard) necə təsir etdiyini izah edir. Layihəni sıfırdan yenidən yazanda bu sıra ilə get: Domain → Repository → Service → Controller → MVC.

## 1. Qaydalar (qərarlar)

- Müştəri pulu **müddətli** qoyur: 3, 6, 12 və ya 24 ay. İllik faiz sabitdir (başlanğıc: 3%, 4%, 5%, 6%). Kredit faizi 10% olduğu üçün bank fərqdən qazanır.
- **Sadə faiz:** `faiz = məbləğ x illik faiz x ay / 12` (`Domain/Deposits/DepositRules.cs`). Məs. 1000 AZN, 12 ay, 5% = 50 AZN.
- Hədlər: ən az 100, ən çox 100 000 AZN (admin dəyişə bilər).
- Pul **bloklanmamış istənilən kartdan** (Cashback da) çıxır, müddət bitəndə **Cashback olmayan** seçilmiş karta qayıdır (əsas məbləğ + faiz). Seçilmiş kart bloklanıbsa, müştərinin başqa yararlı kartına qayıdır; heç biri yoxdursa növbəti işə qədər gözləyir.
- **Müddətdən əvvəl çıxarmaq olmaz.** Bunun üçün endpoint və ya metod yoxdur: qayda "yoxlama" deyil, əməliyyatın olmamasıdır. Müştəri dondurulmuş hesabla (IsRestricted) depozit aça bilmir.
- Açılan depozitdə faiz və müddət **köçürülür** (`Deposit.AnnualRate`, `TermMonths`). Admin sonra variantın faizini dəyişsə, açılmış depozitlərə təsir etmir.
- Faiz müddət bitəndə ödənilir (kassa üsulu) və o ayın xərci sayılır.

## 2. Domain

- `DepositPlan`: variant (müddət, illik faiz, ən az/çox məbləğ, aktivlik). `TermMonths` unikaldır.
- `Deposit`: müştəri, müddət, faiz, məbləğ, `ExpectedInterest` (açılışda hesablanır), `StartedAt`, `MaturesAt`, mənbə kart, qayıdacaq kart, vəziyyət (`Active` / `Paid`), `Reference` (DP-...; unikal).
- Yeni `TransactionType`: `DepositOpen` (kartdan çıxış), `DepositPayout` (əsas məbləğ qayıdır), `DepositInterest` (faiz düşür). Yeni `NotificationCategory.Deposit`, yeni audit hadisəsi `DepositPlanChanged`.
- Migration `AddedDeposits`: iki cədvəl və 4 başlanğıc variant (SQL ilə əlavə olunur, ona görə yeni bazada da hazırdır).

## 3. Repository (`DepositRepository`)

- `OpenAsync`: bir tranzaksiyada variantı, mənbə və qayıdış kartlarını **təzədən oxuyur** (servisdən gələn rəqəmlərə güvənmir), balansı azaldır, jurnal sətrini və depoziti yazır. `RowVersion` toqquşmasında təkrar cəhd edir (kart və köçürmələrlə eyni qayda). `Reference` unikal indeksi eyni sorğunun ikinci dəfə pul çıxarmasının qarşısını alır.
- `PayMaturedAsync`: müddəti bitmiş `Active` depozitlər (bir işdə ən çox 200) tək-tək, hər biri öz tranzaksiyasında ödənilir: iki jurnal sətri (`DM-...` referansı ilə), vəziyyət `Paid`. Eyni depozit ikinci dəfə ödənmir (vəziyyət yoxlaması və `(Reference, Type)` unikal indeksi).

## 4. Service və controller

- `DepositService`: yoxlama (FluentValidation), hesab dondurulmasının yoxlanması, `RequestId`-dən `DP-` referansı, bildiriş (açılış, ödəniş; `dedupeKey` ilə təkrar yazılmır), admin siyahısı, variantın yenilənməsi.
- Müştəri: `GET api/deposits` (variantlar, depozitlər, cəmlər), `POST api/deposits` (açmaq, sürət limiti `transfer`).
- Admin: `GET api/admin/deposits` (axtarış, vəziyyət, səhifələmə, cəmlər, variantlar; bütün işçilər), `PUT api/admin/deposits/plans/{id}` (yalnız SuperAdmin, Admin, Accountant; audit-ə yazılır).
- `DepositMaintenanceService` (arxa plan): proqram açılandan 60 saniyə sonra və hər saat `RunMaturityAsync` çağırır. Proqram bağlı qalıbsa, ilk açılışda keçmiş müddətlər ödənilir.

## 5. Xəzinə (Bank dashboard) necə dəyişdi

- **Müştəri pulu** artıq kart balansları + müddətli depozitlərdir. Dashboard hər ikisini ayrıca göstərir və depozitlər üzrə "hələ ödəniləcək faiz"i yazır.
- **Məcburi ehtiyat** = (kart balansları + depozitlər) x `ReservePercent`.
- **Xərc** artıq ödənilən depozit faizini də daxil edir; mənfəət vergisinin bazası: `gəlir - cashback - depozit faizi - əməliyyat xərci`.
- Hələlik faiz yalnız ödənəndə xərc yazılır (kassa üsulu). Real bankda faiz hər ay yığılır (accrual); bu, sonradan dəyişə bilər.

## 6. MVC

- Müştəri: `/App/Deposits` (`DepositsController`, `Views/Deposits/Index.cshtml`, `wwwroot/js/deposits.js`): müddət seçimi, məbləğ, iki kart, canlı faiz və tarix, təsdiq dialoqu, depozitlərin siyahısı (irəliləyiş xətti, "Earned so far"). Hesab göstərişdir, həqiqisini API edir. Açma adi form + PRG (yeniləmə depoziti təkrar açmasın).
- Admin: `/Admin/Deposits` (`Areas/Admin/Controllers/DepositsController.cs`): cəm kartları, variantların redaktəsi (Save), depozit siyahısı (axtarış, süzgəc, səhifələmə). Dashboard-da "View deposits" linki, Audit süzgəcində "Deposit term changed".
- History-də üç yeni növ (ikon və süzgəc), bildirişlərdə `Deposit` ikonu.

## 7. Test olunanlar

Açılış (1000 AZN, 12 ay → 50.00 faiz), kartdan çıxma, təkrar sorğunun ikinci dəfə çıxarmaması, minimumdan az, Cashback karta qayıdış, pul çatmaması, müddəti bitmiş depozitin iş tərəfindən ödənilməsi (əsas məbləğ + faiz, iki jurnal sətri, bildiriş), admin siyahısı və variantın yenilənməsi, dashboard rəqəmləri.

## 8. Xəzinə jurnalı (Treasury) və kredit yoxlaması

**Xəzinə nədir.** Bankın öz vəsaiti: balans = `TreasuryEntries` cədvəlindəki bütün `Amount`-ların cəmi. Başlanğıc sətir `Capital` (OwnBanks.OwnFunds = 2 400 000). Giriş müsbət, çıxış mənfidir. Müştəri kartlarındakı pul xəzinə deyil, bankın müştəriyə borcudur (ehtiyat bunun faizidir).

**Nə xəzinəyə yazılır** (kart jurnalındakı `Transaction` sətri ilə eyni saxlamada):
| Hadisə | Xəzinə |
|---|---|
| Köçürmə komissiyası, kart açılış haqqı | + |
| Kredit ödənişində əsas borc, faiz, cərimə | + (üç ayrı sətir, eyni kodla) |
| Müştəri depozit qoyur (`DepositOpen`) | + |
| Kredit verilir (`LoanDisbursement`) | − |
| Depozitin əsas məbləği qayıdır (`DepositPayout`) | − |
| Depozit faizi ödənir (`DepositInterest`) | − |
| Cashback ödənir | − |

**Necə yazılır.** `AppDbContext.SaveChanges` override edilib: eyni saxlamada əlavə olunan `Transaction` və `LoanPaymentRecord` sətirlərinə baxıb müvafiq `TreasuryEntry` sətrini əlavə edir. Üstünlüyü: pul yazan heç bir repository-yə toxunmaq lazım gəlmir və əməliyyat yazılmasa xəzinə sətri də yazılmır (ya ikisi də, ya heç biri). Əl ilə xəzinə sətri yazmaq üçün kod yoxdur.

**Keçmiş.** Migration `AddedTreasury` köhnə əməliyyatlardan sətirləri vaxt sırası ilə doldurur (başlanğıc kapital ən əvvəldə).

**Kredit yoxlaması.** `LoanRepository.DisburseAsync` (həm işçi tam məbləği təsdiqləyəndə, həm də müştəri azaldılmış təklifi qəbul edəndə): `xəzinə - kredit < məcburi ehtiyat` olarsa kredit verilmir (`LoanOutcome.TreasuryLow`), müştəriyə ümumi mesaj göstərilir ("The bank can't pay out this loan right now"), rəqəmlər göstərilmir. Məcburi ehtiyat = (kart balansları + aktiv depozitlər) x `ReservePercent`.

**Depozit ödənişi xəzinəyə bağlı deyil:** müddət bitəndə bank müştəriyə borcludur, xəzinədə pul azdırsa da ödəniş edilir (balans mənfiyə düşə bilər, jurnalda görünür).

**Faiz xərci.** Depozit faizi ödənəndə (müddət bitəndə) çıxış kimi yazılır və həmin ayın xərcidir. Faiz dəyişikliyi (admin səhifəsi) yalnız yeni depozitlərə aiddir.

**Səhifələr.** API `GET api/admin/treasury` (SuperAdmin, Admin, Accountant; filtr: növ, tarix aralığı, səhifələmə; hər sətirdən sonrakı balans), MVC `/Admin/Treasury`. Dashboard-da "own funds" artıq xəzinə balansıdır və "history" linki var.

## 9. Aylıq faiz yazılışı, ayın bağlanması və bank borcu (yeniləmə)

**Faiz ay-ay yazılır.** `DepositInterestPosting` cədvəli: hər ay bir sətir (6 aylıq depozitdə 6 sətir). Vaxtı çatmış (`StartedAt + N ay`) hər ayın sətri arxa plan işi tərəfindən yazılır (`PostDueInterestAsync`; proqram bağlı qalıbsa geriyə dönük yazılır, sətrin tarixi faizin düşməli olduğu gündür). Aylıq məbləğ = gözlənilən faiz / müddət, son ay yuvarlaqlaşdırma fərqini götürür (cəm həmişə `ExpectedInterest`-ə bərabərdir, məs. 10.00 / 6 = 1.67 x 5 + 1.65). Müştəri səhifədə "Interest added so far" və aylar üzrə siyahını görür. **Pul yenə müddət bitəndə ödənilir** (müddətdən əvvəl çıxarmaq olmaz).

**Bankın xərci aya düşür.** Dashboard-un "deposit interest" xərci artıq ödəniş gününə yox, faizin yazıldığı aya aiddir (`DepositInterestPostings` cəmi). Beləcə 12 aylıq depozitin xərci 12 aya bölünür. Mənfəət vergisinin bazası da buna görə hesablanır. Xəzinə isə **real pul hərəkətini** göstərir: depozit qoyulanda +, müddət bitəndə əsas məbləğ və faiz −.

**Ayın bağlanması (xəzinə).** Hər tamamlanmış ayın əməliyyat xərci (OwnBanks.OperatingCostMonthly) və mənfəət vergisi (yalnız mənfəət olanda) xəzinədən çıxış kimi yazılır (`OperatingCost`, `ProfitTax`, kod `CLOSE-yyyy-MM-OPEX/TAX`). `TreasuryMaintenanceService` hər saat əvvəlki ayı bağlayır (2026-10-dan başlayaraq, köhnə aylara geriyə dönük xərc yazılmır); eyni ay ikinci dəfə bağlanmır. Əl ilə: `POST api/admin/treasury/close-month?month=2026-09` (yalnız tamamlanmış ay; audit-ə `TreasuryMonthClosed` yazılır).

**Bank borcu.** Xəzinədə pul azdırsa depozit ödənişi yenə də edilir (bank müştəriyə borcludur), xəzinə mənfiyə düşə bilər. Admin xəzinə səhifəsində qırmızı xəbərdarlıq çıxır ("The bank is in debt"), yeni kredit verilmir (məcburi ehtiyat yoxlaması). Balans müsbət olanda yenə kredit verilir.
