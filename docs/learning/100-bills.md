# 100. Ödənişlər (Bills): mobil, kommunal, internet, kredit kodu və aylıq təkrarlanan ödənişlər

## 1. Nəzəriyyə

**Məqsəd:** müştəri öz kartından telefon nömrəsini artırır, işıq/qaz/su/internet borcunu ödəyir və ya başqasının (və ya özünün) kreditini kodla ödəyir. Real operatora qoşulmaq mümkün olmadığı üçün **təchizatçılar və abunəçilər saxta məlumatdır, amma bazadadır** (`Repository/Data/Seed/bills.sql`). Beləliklə ödəniş etdikdə həmin abunəçinin balansı bazada həqiqətən dəyişir və sonra yenidən axtaranda görünür.

**Əsas ideya: işarəli balans.** Hər abunəçinin bir rəqəmi var: `BillSubscriber.Balance`.

| Balans | Mənası |
|---|---|
| mənfi (məs. -132.77) | **borc** (müştəri kommunala borcludur) |
| 0 | borc də yoxdur, qabaqcadan ödənilmiş pul da |
| müsbət (məs. +12.50) | qabaqcadan ödənilmiş pul (mobil nömrənin balansı, kommunalda avans) |

Ödəniş balansı artırır. Əvvəl borc bağlanır, artıq qalan hissə balansda qalır (`DebtCleared` və `CreditAdded`). Mobil nömrədə borc olmur, ödəniş birbaşa nömrənin balansına əlavə olunur.

**Aylıq hesab:** internet, işıq, qaz və su abunəçilərinə hər ay avtomatik hesab yazılır: balansdan çıxılır. Pul (balans) çatmasa balans mənfiyə düşür, yəni **borc yaranır**. Mobil nömrələrə aylıq hesab yoxdur.

## 2. Təchizatçılar və qaydalar

| Təchizatçı | Növ | Nömrə / kod | Qayda |
|---|---|---|---|
| Nar | Mobile | telefon | prefikslər `070`, `077` |
| Azercell | Mobile | telefon | `010`, `050`, `051` |
| Bakcell | Mobile | telefon | `055`, `099` |
| Azərişıq | Electricity | 10 rəqəmli abunəçi kodu | işlətmə (kWh) x tarif 0.09 |
| Azəriqaz | Gas | 9 rəqəmli abunəçi kodu | işlətmə (m³) x tarif 0.22, qışda çox, yayda az |
| Azərsu | Water | 8 rəqəmli abunəçi kodu | işlətmə (m³) x tarif 0.43 |
| CityNet | Internet | 7 rəqəmli abunəçi kodu | paket: Start 20, Comfort 30, Premium 50 AZN |

- Telefon `055 123 45 67`, `0551234567`, `+994 55 123 45 67`, `551234567` yazılışlarının hamısı qəbul olunur (`BillingEngine.NormalizePhone`).
- **Səhv operator:** Nar altında `099...` yazılsa "The operator is not correct. This number belongs to Bakcell, not Nar." Heç bir operatora aid olmayan prefiksdə "Nar numbers start with 070 or 077." deyilir.
- **Olmayan nömrə:** düzgün operator, amma nömrə bazada yoxdursa "This number doesn't exist." Kommunalda "This subscriber code doesn't exist."
- **Abunəçi kodu** hər təchizatçıda fərqli uzunluqdadır (10, 9, 8, 7 rəqəm), ona görə bir təchizatçının kodu başqasında işləmir.
- Kommunal axtarışında **abunəçinin tam adı**, borcu, son hesab (ay, işlətmə, məbləğ) göstərilir. Mobil nömrədə sahibin adı maskalanır ("Rəşad M.").
- Məbləğ hədləri: mobil 1 - 200 AZN, kommunal 1 - 2000 (su 1000), internet 1 - 500.
- Saxta məlumat: 30 mobil nömrə (hər operatordan 10), Azərişıq 10, Azəriqaz 10, Azərsu 8, CityNet 12 abunəçi.

### Aylıq hesabın düsturu (`BillingEngine.ComputeCharge`)
- **İnternet:** paketin aylıq qiyməti (20 / 30 / 50).
- **İşıq, qaz, su:** `işlətmə = orta aylıq işlətmə x fəsil əmsalı x fərdi dəyişmə (0.90 - 1.10)`, `hesab = işlətmə x tarif`. Paket yoxdur: istifadəyə görə pul istənir.
- Fəsil əmsalı: qaz qışda 1.8, yayda 0.35; işıq yayda və qışda bir qədər çox; su yayda bir qədər çox.
- Fərdi dəyişmə təsadüfi deyil: `(abunəçi nömrəsi, il, ay)`-dan hesablanır. Eyni abunəçi və ay həmişə eyni nəticə verir (test təkrarlana bilir).
- Ay Bakı vaxtı ilə sayılır (UTC + 4).

## 3. Fayllar

**API**
- `Domain/Entities`: `BillProvider`, `BillSubscriber` (RowVersion), `BillCharge` (aylıq hesab), `BillPayment` (müştərinin ödənişi, ad və loqo həmin anın surətidir), `RecurringPayment`.
- `Domain/Enums`: `BillCategory`, `TransactionType.BillPayment` və `Cashback`, `NotificationCategory.Bill`.
- `Domain/Bills/BillingEngine.cs`: saf hesab funksiyaları (telefon, prefiks, aylıq hesab, fəsil). Bazaya toxunmur, ona görə test etmək asandır.
- `Domain/Configurations`: unikal indekslər: `(ProviderId, Identifier)`, `(SubscriberId, Period)`, `Reference`.
- `Repository/Repositories/BillRepository.cs`: `PayAsync` (tranzaksiya), `RunMonthlyBillingAsync`, axtarış və xülasə. `RecurringPaymentRepository.cs`.
- `Service/Services`: `BillService` (axtarış, ödəniş, xülasə), `BillProviderService` (admin: ad, aktivlik, loqo), `RecurringPaymentService` (aylıq ödənişlər və iş).
- `Service/Helpers`: `BillMapper`, `BillMessages` (xəta mətnləri), `RecurringMapper`, validatorlar (`Validators/Bills`).
- `Controllers/Client/BillsController.cs` (`api/bills`), `RecurringPaymentsController.cs` (`api/bills/recurring`), `Controllers/Admin/BillProviderController.cs` (`api/admin/bill-providers`).
- `Helpers/BillingMaintenanceService.cs`: hər saat işləyən arxa plan xidməti.
- Migration-lar: `AddedBills`, `AddedRecurringPayments`. Saxta məlumat: `Repository/Data/Seed/bills.sql`.
- Loqolar: `wwwroot/images/providers/*` (admin yenisini yükləyəndə təsadüfi adlı fayl yaranır, köhnə seed faylına toxunulmur).

**MVC**
- `Controllers/BillsController.cs` (`App/Bills`), `ViewModels/Bills/*`, `Views/Bills/Index.cshtml` və `Receipt.cshtml`, `wwwroot/js/bills.js`.
- Admin: `Areas/Admin/Controllers/BillProvidersController.cs`, `Views/BillProviders/Index` və `Edit`, menyuda "Bill providers" (yalnız Admin və SuperAdmin görür).
- Müştəri səhifəsi və tarixçə: `_ActivityRow` (ikonlar), History filtrində "Bill payment" və "Cashback".

## 4. Ödəniş necə işləyir (`BillRepository.PayAsync`)

Hamısı **bir tranzaksiyada** (kartdan çıxış, abunəçi balansı, cashback, qeydlər): ya hamısı olur, ya heç biri.

1. Təchizatçı, abunəçi və kart təzədən oxunur (formada gördüyün köhnə ola bilər). Kart ödəyənin özünündür və bloklanmayıb.
2. Məbləğ təchizatçının hədləri daxilindədir, kartda pul var.
3. Abunəçi balansı: əvvəl borc bağlanır (`DebtCleared`), artıq hissə balansa gedir (`CreditAdded`).
4. Kart balansı azalır, jurnala `BillPayment` sətri yazılır (`Reference = BP-<16 hex>`).
5. **Cashback:** ödəyən kartın növünün faizi (Card types səhifəsində `CashbackPercent`: Standard 0.5%, Silver 1%, Gold 1.5%) müştərinin bloklanmamış **Cashback kartına** düşür (`Cashback` sətri, `CB-` kodu). Cashback kartı ilə ödəyəndə cashback yığılmır.
6. `BillPayment` sətri yazılır (qəbz və tarixçə üçün).

**İkiqat klik qorunması (idempotency):** forma hər açılanda yeni `RequestId` (GUID) alır. Referans bundan hesablanır və `(Reference, Type)` unikaldır. Eyni sorğu təkrar gəlsə pul ikinci dəfə çıxmır, əvvəlki qəbz qaytarılır.

**Eyni anda iki ödəniş/hesab:** `RowVersion` (abunəçi və kart). Toqquşma olsa 8 dəfəyə qədər yenidən cəhd edilir.

## 5. Aylıq hesab xidməti (`BillingMaintenanceService`)

- API açıldıqdan 45 saniyə sonra, sonra hər saat işləyir.
- 1) Bu ay hesabı yazılmamış abunəçilərə hesab yazır (`RunMonthlyBillingAsync`). `(SubscriberId, Period)` unikal olduğu üçün təkrar işləsə ikinci hesab yazılmır.
- 2) Sonra təkrarlanan ödənişləri edir (`RecurringPaymentService.RunDueAsync`).
- Xəta olsa proqram dayanmır, növbəti saatda təkrar yoxlanır.

## 6. Təkrarlanan (aylıq) ödənişlər

Ödəniş formasında "Make this a monthly payment" qutusu: ödəniş uğurlu olandan sonra `POST api/bills/recurring` çağırılır.

- Mobil/kommunal/internet: **sabit məbləğ** hər ayın 1-i seçilmiş kartdan çıxır. Kredit kodu ilə: **həmin ayın aylıq ödənişi** (məbləğ yoxdur, hər ay özü hesablanır) və **ayın SON günü** ödənilir. Səbəb: kredit taksiti ayın son günü vaxtı çatır. Ayın 1-i ödənilsəydi taksit artıq 1 gün gecikmiş olardı və 10 AZN cərimə + gündə 0.5% düşərdi. Qaydalar `Domain/Bills/RecurringRules.cs`-dədir (təmiz funksiyalar).
- Yaradılanda mobil/kommunal/internet üçün `LastPeriod = cari ay` yazılır (ilk ödəniş elə indi edilib), növbəti ödəniş **gələn ayın 1-ində** olur. Kreditdə `LastPeriod = keçən ay` yazılır ki, bu ayın son günü də işləsin (həmin gün borc yoxdursa, məsələn müştəri özü ödəyibsə, səssiz keçilir).
- Eyni təchizatçı və nömrə üçün yenidən seçilsə yeni qeyd açılmır, mövcud olanın məbləği və kartı yenilənir. Hər müştəri üçün ən çox **10** aylıq ödəniş.
- Müştəri **dayandıra** (Pause), davam etdirə və silə bilər. Başqasının qeydinə toxunmaq olmur (404).
- **İş:** `LastPeriod` cari aydan fərqli olan aktiv qeydlər üçün ödəniş edilir. `RequestId = MD5("recurring:{id}:{ay}")`: iş yarımçıq qalıb təkrar işləsə pul ikinci dəfə çıxmır.
- **Uğursuz olsa** (pul çatmır, kart bloklanıb, təchizatçı söndürülüb): səbəb `LastError`-a yazılır, qeyd "Failed" göstərilir. Hər gün bir dəfə təkrar cəhd olunur (`LastAttemptDay`), amma müştəriyə ay boyu **bir dəfə** bildirilir (`rec-fail:{id}:{ay}`).
- **Kredit bağlanıbsa:** aylıq ödəniş avtomatik dayandırılır və müştəriyə bildirilir. Bu ay ödəniləsi bir şey yoxdursa (müştəri özü ödəyib) səssiz keçilir.
- Uğurlu ödənişdə də bildiriş gedir ("Monthly payment made").

## 7. Admin: loqo və ad

Admin və SuperAdmin "Bill providers" səhifəsində təchizatçının **adını, aktivliyini və loqosunu** dəyişir (kod, kateqoriya, qaydalar dəyişmir). Söndürülən təchizatçı müştəri siyahısından yox olur və onunla axtarış/ödəniş 404 verir. Loqo PNG/JPEG/WebP, 2 MB-a qədər (fayl məzmunu yoxlanır, `07-files-upload.md`). Yeni loqo əvvəl yüklənir, bazaya yazılandan sonra köhnəsi silinir. Silmə yalnız admin yüklədiyi təsadüfi adlı fayllara aiddir: seed loqoları (`nar.jpg` və s.) heç vaxt silinmir.

## 8. MVC səhifəsi

- Təchizatçı qutuları qruplara bölünür: Mobile, Utilities, Internet, Loan. "Caspian loan" API-də təchizatçı deyil: `api/loans/lookup/{kod}` və `api/loans/pay-by-code` işlədilir (kod `KR-2026-000123`, kiçik hərflə də yazıla bilər).
- **Check** düyməsi (və ya Enter) JS ilə `POST App/Bills/Lookup`-a sorğu göndərir (antiforgery tokeni ilə), JSON qayıdır: ad, borc, son hesab. Nömrə dəyişsə köhnə nəticə silinir.
- Ödəniş addımı: məbləğ (borc varsa avtomatik yazılır), qısa yollar (Debt, 5, 10, 20, 50), kart seçimi, təkrar qutusu, təsdiq pəncərəsi. Sonra PRG: qəbz ayrıca səhifədə (yeniləmə ödənişi təkrar etməsin).
- Xəta olsa səhifə yenidən açılır, seçim və yazılanlar qalır.
- Sağ tərəf: "Monthly payments" cədvəli (Pause/Resume/Remove) və "Recent payments".

## 9. Yoxlama (necə sınandı)

- **API (50 yoxlama):** səhv operator, olmayan nömrə, bütün prefikslər, kod uzunluğu, kommunalda ad və borc, tam və hissəvi ödəniş, artıq ödənişin balansda qalması, eyni `RequestId` ilə təkrar (pul bir dəfə), minimum/maksimum, çatışmayan balans, başqasının kartı, cashback 0.05 (10 AZN-in 0.5%-i), tarixçədə sətirlər, rollar (Accountant və müştəri 403), loqo yükləmə, söndürmə.
- **Aylıq hesab:** 40 abunəçiyə bir ay üçün bir hesab, təkrar yoxdur.
- **Təkrarlanan ödənişlər:** yaratma qaydaları, 10 limit, dayandır/davam, başqasının qeydi 404, iş (uğurlu, pul çatmayan, kredit, dayandırılmış, bağlanmış kredit), eyni gündə ikinci işə düşmədə pul və bildirişin təkrarlanmaması.
- **Brauzerdə:** kommunal ödəniş, kredit kodu ilə ödəniş, mobil nömrə, telefon ölçüsü, admin təchizatçı səhifəsi.

## 10. Başqa kompüterdə qurmaq

Boş bazada API açılanda migration-lar tətbiq olunur, amma saxta təchizatçı/abunəçi məlumatı koddan yox **SQL skriptindən** gəlir: `Repository/Data/Seed/bills.sql`-i bir dəfə işlət (təkrar işlətsə də zərər yoxdur: artıq olanlara toxunmur). Bazanı `.bak`/`.bacpac` ilə köçürürsənsə məlumatlar onsuz da içindədir.

## 11. Sonradan əlavə olunanlar (2026-10-24)
- "Caspian loan" seçimi API-də təchizatçı deyil (MVC `BillsController`-də `-1` nömrəli xüsusi seçimdir). Loqosu artıq `KR` işarəsi yox, **Settings-dəki bankın öz loqosudur** (`SiteSettingsLoader`), admin loqonu dəyişsə burada da dəyişir (seçim düyməsi, aylıq ödənişlər cədvəli və qəbz).
- Kateqoriya başlıqları (Mobile, Utilities, Internet, Loan), identifikator yazıları (Loan code, Phone number...) və təchizatçı adı tərcümə olunur (`T[...]`); `bills.js`-dəki izah mətni `cbt(...)` ilədir.
