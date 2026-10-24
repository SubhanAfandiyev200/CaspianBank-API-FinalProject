# 93. PLAN: saxta dövlət reyestri, FİN ilə qeydiyyat, bank BIN kataloqu

> **Status: plandır, hələ yazılmayıb.** İstifadəçi müəllimlə dəqiqləşdirir. Təsdiq gələndən sonra bu fayl həqiqi sənədləşməyə çevrilir (hazır funksiyanın izahı, sıfırdan yazma addımları). Plan 2026-10-10-da müzakirə olunub.

## 1. Məqsəd
Bank real görünməlidir: müştəri **vətəndaş reyestrində** olmalıdır; qeydiyyat FİN ilə yoxlanır; kredit qərarı vətəndaşın tarixçəsinə baxır; kartın ilk 8 rəqəmi hansı banka aid olduğunu göstərir.

## 2. Qərar verilənlər (istifadəçi ilə)
| Mövzu | Qərar |
|---|---|
| Vətəndaş: rol yoxsa model? | **Model (cədvəl).** `Customer` rolu icazə üçün qalır; vətəndaş qeydi hesabdan əvvəl mövcud olan data-dır. Əlaqə FİN ilə |
| Başqa banka köçürmə | **Tam işləyəcək**, **ayrı komissiya** tutulacaq (daxili köçürmənin komissiyasından fərqli) |
| Ölü vətəndaş | Qeydiyyatda mesaj: bu şəxs həyatda deyil |
| 18 yaşdan kiçik | Qeydiyyatda mesaj: 18 yaşdan kiçiksiniz |
| FİN reyestrdə yoxdur | Hesab **yaradılmır**, mesaj: bu FİN-ə uyğun vətəndaş tapılmadı |
| ID nömrəsi (ikinci faktor) | **Bəli**: qeydiyyatda FİN + ID kartın nömrəsi, ikisi eyni vətəndaşa aid olmalıdır |
| Saxta datanın bazaya düşməsi | Defolt: proqram işə düşəndə özü (seeder, FİN bazada yoxdursa əlavə edir). İstifadəçi başqa üsul demədi |
| "Kirli" status və səbəb | **Müştəriyə görünmür.** Yalnız işçi panelində (kredit baxışı və s.) görünür |
| Caspian kart nömrəsi | `4532` əvəzinə **8 rəqəmli BIN** ilə başlayır (məs. `2121 2134`; başqa bank: `4169 7388`) |
| Profil səhifəsi | Reyestr məlumatları (ad, soyad, ata/ana adı, doğum tarixi, FİN, ID nömrəsi...). Telefon **real**dır, istifadəçi özü yazır |
| WebDesigner rolu | Silindi (2026-10-10). Home məzmununu Admin və SuperAdmin idarə edir |

## 3. Nəzəriyyə

### 3.1 KYC və reyestr yoxlaması
Bank hesab açanda müştərinin kim olduğunu yoxlayır (**KYC**: Know Your Customer). Azərbaycanda bu dövlət məlumat bazasına (ASAN, ID kart) sorğu ilə edilir. Biz bunu **saxta reyestr cədvəli** ilə simulyasiya edirik: eyni sorğu və eyni cavab növləri (tapıldı / tapılmadı / yaş / status), amma data özümüzündür.

> Real insanların məlumatı yoxdur; bütün adlar uydurmadır.

### 3.2 Niyə ad, soyad, doğum tarixi artıq istifadəçidən gəlmir?
İndi qeydiyyatda `RegisterDto` ad, soyad, doğum tarixi qəbul edir; istifadəçi yalan yaza bilər (məs. yaşı böyük göstərmək). Yeni axında bu sahələri **server reyestrdən götürür**: istifadəçi yalnız FİN (və ID nömrəsi), telefon və parol göndərir. Beləliklə yaş yoxlaması saxtalaşdırıla bilməz.

### 3.3 Kart BIN-i (IIN)
Kart nömrəsinin ilk rəqəmləri **bank identifikasiya nömrəsidir** (BIN / IIN, standart ISO/IEC 7812). Köhnə standartda 6 rəqəm idi, indi **8 rəqəm**dir (bizim yanaşma düzgündür). Hər bank/şəbəkə üçün BIN siyahısı var. Nömrənin qalanı müştəri hesabı və sonda Luhn yoxlama rəqəmidir. Transfer səhifəsində ilk 8 rəqəm yazılanda `Banks` cədvəlindən bank tapılır.

### 3.4 Başqa banka köçürmə (interbank)
Real həyatda pul bankdan çıxır, emal mərkəzi (processing) vasitəsilə başqa banka gedir və komissiya tutulur. Biz simulyasiya edirik:
- Göndərənin balansı azalır (məbləğ + **banklararası komissiya**).
- Alıcı kart `ExternalCards` (saxta başqa bank kartları) cədvəlində axtarılır; alıcının adı maskalanmış göstərilir (daxili köçürmədəki kimi).
- Jurnala yeni əməliyyat növləri yazılır (məs. `InterbankOut`, komissiya sətri).
- Komissiya bankın gəlirinə düşür (gələcək xəzinə/treasury ilə birləşəcək).

## 4. Məlumat modeli (təklif)
**`Citizens`** (saxta dövlət bazası; ayrıca `gov` sxemi də ola bilər ki, SSMS-də ayrı görünsün):
`Fin` (unikal, 7 simvol), `Name`, `Surname`, `FatherName`, `MotherName`, `BirthDate`, `IdCardNumber` (məs. `AZE12345678`), `Gender`, `Address` (istəyə görə), `Status` (`Clean`, `Delays`, `Blacklisted`), `StatusReason`.
İstəyə görə **`CitizenCreditRecords`**: əvvəlki kreditlər (məbləğ, bank, gecikmə günləri, bağlanıb/açıq) — kredit baxışı üçün.

**`Banks`**: `Name` (ABB, Kapital Bank...), `IsCaspian`, `LogoUrl` + **`BankBins`** (8 rəqəmli BIN-lər, unikal). `Network` (Visa/Mastercard) saxlanmır.
**`ExternalCards`**: `CardNumber`, `BankId`, `HolderName` (saxta), `IsActive`.

**30 saxta vətəndaşın ssenariləri:** təmiz böyüklər (çoxu), gecikməsi olanlar, qara siyahı (fırıldaq/cinayət), 12 və 16 yaşlı uşaqlar, yeni azad olmuş biri, çox yaşlı biri, ölmüş biri. Hər xəta mesajı nümayiş etdirilə bilsin.

Status kimə nə təsir edir:
| Status | Qeydiyyat | Kredit |
|---|---|---|
| Clean | olar | olar |
| Delays | olar | ehtiyatla (rədd və ya şərtlə) |
| Blacklisted | **olar (hesab açılır)** | rədd (yalnız kredit verilmir) |
| Deceased | "həyatda deyil" mesajı | - |
| 18-dən kiçik | mesaj | - |

## 5. Yeni qeydiyyat axını
```
Home "Open account" → Welcome (email)
   → [email yoxdursa] OTP kodu → doğrula
   → Register: FİN + ID nömrəsi + telefon + parol
        server: reyestri yoxla (tapılmadı / uyğunsuz / 18-dən kiçik / ölü / qara siyahı)
        server: AppUser yarat (ad, soyad, doğum tarixi reyestrdən)
        server: Cashback + Standard kartları dərhal aç
   → Login → /App (kartlar hazırdır)
```
Dəyişəcək yerlər (təxmini): `RegisterDto` və validator (ad/soyad/doğum tarixi çıxır, FİN/ID gəlir), `AccountService.RegisterAsync`, `AppController.AddCard` (ilk dəfə FİN formu lazım olmur), MVC `Register.cshtml`, `CardNumberGenerator` (BIN), `TransferService` (BIN ilə bank tanıma + başqa bank köçürməsi), `TransactionType` (yeni növlər), profil səhifəsi.

## 6. Transfer səhifəsində bank tanıma
Kart nömrəsinin ilk 8 rəqəmi yazılanda (brauzer sorğusu: `GET api/banks/lookup?bin=...`) nömrə sahəsinin altında çip çıxır: "Kapital Bank · Visa". Caspian kartı üçün "Caspian Bank". BIN tapılmasa: "Bank tanınmadı". Başqa bank seçildikdə Review səhifəsində ayrıca komissiya göstərilir.

## 7. Açıq suallar və izahları

**Sual 1. ID nömrəsi ikinci təsdiq (second factor) olsun?** Qeydiyyatda yalnız FİN yazılsa, FİN başqasının da bilə biləcəyi koddur: kimsə sənin FİN-ni bilsə sənin adına hesab aça, hətta sənin adına kredit götürə bilər. ID kartın üstündəki seriya/nömrə (məs. `AZE12345678`) ikinci məlumatdır; ikisi eyni vətəndaşa aid olmalıdır. Datada bu nömrə onsuz da olacaq, əlavə iş yalnız formada bir sahə və server yoxlamasıdır. Təhlükəsizlik üçün xəta mesajı hansının səhv olduğunu demir ("FİN və ID uyğun gəlmir"). **Qərar: bəli.**

**Sual 2. "Kirli" status müştəriyə görünsün?** Qərar verildi: görünmür.

**Sual 3. Başqa banka köçürmə:** qərar verildi: tam işləyəcək, ayrı komissiya ilə.

**Sual 4. 30 vətəndaş bazaya necə düşsün?**
- **(A) Proqram özü:** kodda siyahı var, proqram işə düşəndə "bu FİN bazada yoxdursa əlavə et" edir (`AdminSeeder` kimi). Sən heç nə etmirsən, hər kompüterdə hazır olur. Datanı dəyişmək üçün kodda düzəliş edib yenidən işə salırsan.
- **(B) `.sql` skripti:** sən SSMS-də açıb run edirsən; oxumaq və əl ilə redaktə etmək rahatdır. Amma bu, "unudula bilən" addımdır və yalnız bizim üçün lazımdır (müəllimə baza export ilə gedir, data içində olur).
- Tövsiyə: (A). İstifadəçi bu sualı "FİN bazada yoxdursa hesab yaradılmasın" kimi cavablandırdı; bu artıq qeydiyyat qaydasıdır (bax bölmə 2). Seed üsulu üçün ayrıca etiraz olmadığından defolt (A) qəbul olunur, istəsə (B) əlavə olunar.

## 8. Mərhələ planı (təsdiqdən sonra)
1. `Citizens` (+ seed) + qeydiyyatın yenidən qurulması + avtomatik kartlar + profil.
2. `Banks` (8 rəqəmli BIN, Caspian BIN dəyişməsi) + Transfer-də tanıma çipi.
3. Başqa banka köçürmə (`ExternalCards`, komissiya, jurnal, xəzinəyə gəlir).
4. Sonra: Support, Kredit (reyestrə söykənərək).
Təxmini əlavə vaxt: 2–4 gün.

## 9. Köhnə data haqqında
Bazadakı mövcud test istifadəçiləri və `4532...` ilə başlayan kartlar yeni qaydalara uyğun deyil. Final DB export-dan əvvəl ya silinməlidir, ya da reyestrdəki vətəndaşlara bağlanmalıdır.

## 10. Sahələr siyahısı (2026-10-10, təsdiqlənməmiş plan)

**İki ayrı status (iki enum):**
- `LifeStatus`: `Alive`, `Deceased`. Ölüdürsə qeydiyyatda "bu şəxs həyatda deyil" mesajı.
- `CreditStatus`: `Clean` (təmiz), `Delays` (gecikmələri olub), `Blacklisted` (qara siyahı). "Kirli" tərəfin səbəbi `StatusReason` sahəsindədir. Bu status və səbəb müştəriyə görünmür, yalnız işçiyə.

**`Citizens` (dövlət reyestri, hesab olmayan insanlar da burada var):**
| Sahə | Qeyd |
|---|---|
| `Id`, `CreatedAt` | `BaseEntity`-dən |
| `Fin` | 7 simvol, unikal |
| `IdCardNumber` | məs. `AZE12345678`, unikal |
| `Name`, `Surname`, `FatherName`, `MotherName` | |
| `BirthDate` | yaş buradan hesablanır |
| `Gender` | istəyə görə |
| `Address` | istəyə görə (profildə göstərmək üçün) |
| `LifeStatus` | Alive / Deceased |
| `DeathDate` | yalnız ölü üçün (istəyə görə) |
| `CreditStatus` | Clean / Delays / Blacklisted |
| `StatusReason` | yalnız işçiyə görünür |

**`AppUser` (bankdakı hesab):** mövcud sahələr qalır: `Id`, `Email`, `UserName`, `PasswordHash`, `PhoneNumber` (istifadəçi özü yazır), `EmailConfirmed`, `Name`, `Surname`, `BirthDay` (qeydiyyatda reyestrdən köçürülür), `FinKod`, `IsRestricted`, `CreatedAt`. Əlavə (təklif): `CitizenId` (reyestrdəki insana bağlayan unikal açar; işçilərdə boşdur).

**Profil səhifəsində müştəri görür:** ad, soyad, ata adı, ana adı, doğum tarixi, cins, FİN, ID nömrəsi, ünvan, telefon, email, qeydiyyat tarixi. **Görmür:** `LifeStatus`, `CreditStatus`, `StatusReason`.
**İşçi görür (Admin, Security, CustomerSupport, kredit baxan):** hamısı, statuslar və səbəb də.

## 11. Dəqiqləşdirmə (2026-10-10): 30 nəfər statikdir, dəyişən yalnız gecikmələrdir
- 30 vətəndaşın əsas məlumatı (FİN, ad, ata/ana adı, doğum tarixi, ID nömrəsi, `LifeStatus`) **sabitdir**, heç vaxt dəyişmir.
- **Dəyişən yalnız gecikmələrdir** (kredit gecikmələri). Ona görə gecikmələr `Citizens` sətrində sabit mətn kimi yox, ayrıca cədvəldə (təklif: `CitizenCreditRecords`: bank, məbləğ, başlama tarixi, açıq/bağlı, gecikmə günü) saxlanılır. Faktiki `CreditStatus` buradan çıxarıla bilər: qara siyahı bayrağı varsa `Blacklisted`, açıq və gecikmiş kredit varsa `Delays`, yoxsa `Clean`.
- **Seed qaydası ("yoxdursa əlavə et, varsa toxunma"):** 30 sətrin bazaya ilk dəfə düşməsi işidir. Proqram hər işə düşəndə hər FİN üçün baxır: sətir yoxdursa yazır, **varsa toxunmur**. Nəticə: sonradan dəyişən gecikmələr yenidən başladanda sıfırlanmır.
- Bu qaydanın qeydiyyatdakı "FİN tapılmadı" davranışı ilə əlaqəsi **yoxdur**: o, istifadəçi baza yazılmış FİN-i yazmayanda işləyir.
- **Kredit ödənişi** ayrıca səhifədə yox, **Bills (kommunal ödənişlər) səhifəsindən** ediləcək: kateqoriyalardan biri "Loan payment" olacaq (istifadəçinin aktiv kreditləri və növbəti ödəniş). Ödəniş vaxtında olmazsa gecikmə qeydi yaranır və reyestrdəki `CreditStatus`-a təsir edir. Buna görə Bills kreditdən sonra və ya onunla birlikdə yazılır.

## 12. Tələblər dəqiqləşdi (2026-10-10, istifadəçinin sözləri ilə): müştəri və kredit tarixçəsi
**Müştəri məlumatı:** ad, soyad, FİN, ID seriya/nömrəsi, ana adı, ata adı, yaşayış yeri, doğulduğu yer, doğum tarixi.
**Kredit tarixçəsi (başqa banklar):** adamın adına nə qədər borc var, nə qədərini ödəyib, nə qədəri qalıb, nə qədərini gecikdirib; başqa banklardan nə qədər götürüb və s.
**Ödəniş statusu (hər kredit üçün):** vaxtında ödəyir / gec ödəyir / çox gec ödədi / ödənişə görə məhkəməlik oldu.
**Öz bankımız:** adamın Caspian-a olan borcu da eyni formada görünsün (qalıq, ödənilən, gecikmə, status).
Bu hesabat işçi tərəfdədir (kredit baxışı); müştəri statusları görmür.

Təklif olunan model:
- `Citizen`: + `BirthPlace` (doğulduğu yer), `Address` (yaşayış yeri).
- `CitizenCreditRecord` (yalnız **başqa banklar**): `BankName`, `Amount` (götürülən), `PaidAmount` (ödənilən), `OverdueAmount` (gecikmiş məbləğ), `DelayDays`, `StartDate`, `EndDate`, `IsClosed`, `PaymentStatus`, `Note`. Qalıq = `Amount - PaidAmount` (hesablanır).
- `PaymentStatus` enum: `OnTime`, `Late`, `Court` (`VeryLate` ləğv edildi).
- Caspian-dakı borclar ayrıca kopyalanmır: gələcək `Loans` cədvəlindən oxunur. Hesabat (credit report) ikisini birləşdirir: başqa banklar + Caspian.
- Vətəndaşın ümumi göstəriciləri hesablanır (saxlanmır): ümumi götürülən, ödənilən, qalıq, gecikmiş məbləğ, açıq kredit sayı, ən pis status.

## 13. Qərarlar (2026-10-10, istifadəçi)
1. **Qara siyahıdakılara da hesab açılır.** `Blacklisted` yalnız kredit verilməsinə təsir edir (kredit rədd); qeydiyyata mane olmur. Bölmə 4-dəki cədvəl buna görə düzəldilib. Yalnız `Deceased` (ölü), 18-dən kiçik və reyestrdə olmayan qeydiyyatdan keçə bilmir.
2. **`VeryLate` yoxdur.** `PaymentStatus`: `OnTime`, `Late`, `Court`.
3. **`CardNetwork` (Visa/Mastercard/Other) yoxdur.** Bank kataloqu real bank adları ilə olacaq (ABB, Kapital Bank və s.). Transferdə kartın ilk 8 rəqəmi (məs. `4169 7388`) kataloqdakı BIN-ə uyğun gəlsə, həmin bankın işarəsi (loqo, ad) çıxır.
4. **Banklar və saxta kartları istifadəçi özü doldurur** (həm 30 vətəndaşı, həm banklar, BIN-lər və `ExternalCard` sətirlərini).
5. Təklif (cavab gözlənilir): bir bankın bir neçə BIN-i ola bildiyi üçün ayrıca `BankBin` cədvəli.

## 14. Tam model və kredit qaydaları (2026-10-10, istifadəçinin təsviri əsasında). Bölmə 3-4 və 12-nin yerinə keçir

### 14.1 İstifadəçinin tələbləri (qısa)
- Müştəri sahələri: ad, soyad, ata adı, ana adı, doğum yeri, **iş yeri**, **maaş**, doğum tarixi, FİN, seriya nömrəsi, yaşayış yeri. Bəziləri giriş edəndən sonra profil işarəsində görünür.
- Müştəri həm Caspian-dan, həm başqa banklardan kredit götürə bilər; bir neçə kredit ola bilər; **hər kreditin səbəbi** olmalıdır.
- Başqa bank kreditləri üçün işçi bilməlidir: nə qədər götürüb, vaxtında ödəyibmi, gecikmə olubmu, gecikməni ödəyib qurtarıbmı, kredit bağlanıbmı, ödəməməyə görə məhkəməlik olubmu. Hesabat **bank-bank** (Caspian ayrı, Kapital ayrı...), aşağıda **ümumi məbləğ** (bütün banklar daxil).
- Caspian kreditləri **ayrı cədvəldə** (daha vacib). Kredit verəndə işçi bütün datanı görür (statuslar daxil). Məhkəməlik olubsa başqa bank tərəfindən "kirli" kimi görünür.
- Gecikmə çoxdursa kredit məbləği çox az verilir. Kredit məbləği aylara bölünür, faizlə. Hər ay ödəniş var; gecikərsə **10 AZN cərimə** + ödənilməli məbləğin hər gün **0,5%-i** (ödəyənə qədər). Müştərinin Caspian-dan keçmişi də görünür (gecikdirib, ödəyib, nə qədər).
- Qərarlar: Court avtomatik qara siyahıya salmır. BankBin = bank adı + şəkil + ilk 8 rəqəm (bir model). Kredit üçün minimum yaş 18. Faiz, banklararası komissiya və Caspian BIN-i sonra deyiləcək.

### 14.2 Enum-lar
`LifeStatus {Alive, Deceased}`, `CreditStatus {Clean, Delays, Blacklisted}` (Court avtomatik dəyişmir), `LoanPurpose {Consumer, Car, Mortgage, Education, Medical, Repair, Business, Other}`, `LoanStatus {Pending, Rejected, Active, Closed, Court}`. `PaymentStatus` və `CardNetwork` ləğv edildi (ödəniş vəziyyəti faktlardan hesablanır).

### 14.3 Modellər
- `Citizen`: `Fin`, `IdCardNumber`, `Name`, `Surname`, `FatherName`, `MotherName`, `BirthDate`, `BirthPlace`, `Address`, `WorkPlace`, `MonthlyIncome`, `LifeStatus`, `DeathDate?`, `CreditStatus`, `StatusReason?`, `CreditRecords`.
- `CitizenCreditRecord` (yalnız başqa banklar): `CitizenId`, `BankName`, `Purpose`, `Amount`, `PaidAmount`, `MonthlyPayment`, `StartDate`, `EndDate?`, `IsClosed`, `DelayCount`, `MaxDelayDays`, `OverdueAmount`, `WentToCourt`, `Note?`. Hesablanır: qalıq = `Amount - PaidAmount`; "vaxtında ödəyib" = `DelayCount == 0`; "gecikməni ödəyib" = `DelayCount > 0 && OverdueAmount == 0`.
- `BankBin`: `BankName`, `LogoUrl`, `Bin` (8 rəqəm, unikal), `IsCaspian`. `ExternalCard`: `CardNumber`, `HolderName`, `IsActive` (bank BIN-dən tapılır).
- `Loan` (Caspian): `UserId`, `Purpose`, `PurposeNote?`, `RequestedAmount`, `ApprovedAmount?`, `TermMonths`, `AnnualInterestRate`, `MonthlyPayment`, `MonthlyIncomeAtApply`, `Status`, `RejectReason?`, `ReviewedByUserId?`, `ReviewedAt?`, `DisbursedCardId?`, `DisbursedAt?`, `ClosedAt?`, `Installments`.
- `LoanInstallment`: `LoanId`, `Number`, `DueDate`, `PrincipalAmount`, `InterestAmount`, `PaidAmount`, `PaidAt?`, `FixedPenalty`, `AccruedPenalty`, `PaidPenalty`.
- `AppUser`: + `CitizenId?`. `TransactionType`: + `InterbankOut`, `InterbankCommission`, `LoanDisbursement`, `LoanPayment`.

### 14.4 Cərimə nümunəsi
Aylıq ödəniş 100 AZN, vaxtı keçib. Gecikmənin 1-ci günündən 10 AZN sabit cərimə yazılır; hər gün üstünə 100 × 0,5% = 0,50 AZN gəlir. 10-cu gün: 10 + 10 × 0,50 = **15 AZN** cərimə, ödəniləcək cəmi **115 AZN**. Ödəniş sırası: əvvəl cərimə, sonra faiz, sonra əsas borc. Cərimə faizi cərimənin üstünə gəlmir (mürəkkəb faiz yoxdur).

### 14.5 Açıq məsələlər
Profildə hansı sahələrin görünməsi; cərimənin yuxarı həddi (0,5%/gün ildə təxminən 182%); gecikmə günlərinə görə kredit məbləğinin azalma düsturu; faiz (annuitet); əlavə funksionallıq təklifləri (DTI limiti və s.).

## 15. Qərarlar (2026-10-10, istifadəçi)
**Qəbul olunan əlavə funksionallıq:**
1. **DTI limiti**: bütün aylıq ödənişlərin cəmi (başqa banklar + Caspian + yeni kredit) maaşın müəyyən faizini keçməsin. Faiz həddi sonra deyiləcək (təklif 40–50%). Modeldə yeni sahə lazım deyil (`MonthlyIncome`, `MonthlyPayment` var).
2. **Kredit kalkulyatoru** (müştəri tərəfdə): məbləğ + müddət → aylıq ödəniş və cədvəl.
4. **Gecikmə və cərimə "baxanda hesablanır"**: arxa plan xidməti yoxdur; cərimə oxunanda və ödəniş anında hesablanıb saxlanır.
5. **Məhkəməyə keçid**: 90 gün gecikəndə `Loan.Status = Court` (modelə `CourtAt` əlavə olunur).
6. **Erkən ödəniş**: krediti vaxtından əvvəl tam bağlamaq (modelə `Loan.ClosedEarly`, `LoanInstallment.IsCancelled` əlavə olunur).
8. **Ödəniş xatırlatması**: ən azı proqramın içində (bildirişlər funksiyası ilə; ayrıca `Notification` modeli o funksiya ilə gələcək).

**Qəbul olunmayan (lazım deyil):** 3. cərimənin yuxarı həddi (0,5%/gün qaydası olduğu kimi qalır), 7. işçinin baxışı üçün audit (bu bölmədə; ümumi Audit log ayrıca funksiyadır).

**Profildə müştəri görür:** yalnız **ad, soyad, FİN kod, telefon nömrəsi**. (Qalan sahələr, maaş, statuslar və kredit tarixçəsi müştəriyə görünmür.)

**Kredit məbləği/faizi qaydaları sonra deyiləcək** (model hazır olandan sonra): məsələn Caspian-dan çox kredit götürüb erkən ödəyənlərə faiz azalır. Bunun üçün saxlanılan fakt: `Loan.ClosedEarly` + kredit sayı (bunlar hesablanıb endirim tətbiq olunacaq).

**Açıq sual:** `CitizenCreditRecord.BankName` mətn olaraq yazılsın, yoxsa siyahıdan seçilsin (ayrıca `Bank` cədvəli)? Bu suala istifadəçi hələ cavab verməyib, izah olunub.

## 16. Modellərin son siyahısı (2026-10-10, nümunə dəyərlərlə göstərildi)
İstifadəçiyə bütün modellər hər sahənin yanında nümunə dəyər şərhləri ilə göstərildi. Siyahı: enum-lar `LifeStatus`, `CreditStatus`, `LoanPurpose`, `LoanStatus`; modellər `Citizen`, `CitizenCreditRecord`, `Bank`, `BankBin`, `ExternalCard`, `Loan`, `LoanInstallment`; əlavələr `AppUser.CitizenId`, `TransactionType` (4 yeni növ). Bank adı üçün **B variantı** (ayrıca `Bank` cədvəli, `BankBin` və `CitizenCreditRecord` ona `BankId` ilə bağlanır) göstərilib; istifadəçi A/B seçimini hələ deməyib. A seçilərsə: `Bank` və `BankBin` birləşib bir `BankBin` olur (`BankName`, `LogoUrl`, `Bin`, `IsCaspian`), `CitizenCreditRecord`-da `BankId` əvəzinə `BankName` mətni qalır.

## 17. Qərar: Bank A variantı (2026-10-10) və transfer axını
İstifadəçinin təsvirinə görə bank modeli **bir modeldir** (A variantı): `BankBin { BankName, LogoUrl, Bin (8 rəqəm), IsCaspian }`. Ayrıca `Bank` cədvəli **yoxdur**. `CitizenCreditRecord.BankName` mətn olaraq qalır; yazı səhvinin qarşısı üçün seeder işə düşəndə hər `BankName`-in `BankBin.BankName` siyahısında olub-olmadığını yoxlayır və uyğun gəlməyəni log-a yazır.

`ExternalCard { CardNumber, HolderName, IsActive }`: başqa bankların saxta müştəri kartları. Bank, kartın ilk 8 rəqəminə görə `BankBin`-dən tapılır (kartın özündə bank sahəsi saxlanmır). Seeder hər `ExternalCard.CardNumber`-in ilk 8 rəqəminin `BankBin`-də olduğunu yoxlayır.

**Transfer axını (başqa banka):**
1. Transfer səhifəsində "başqa müştəriyə" rejimində kart nömrəsi yazılır. İlk 8 rəqəm yazılanda brauzer `GET api/banks/lookup?bin=41697388` sorğusu göndərir. Tapılsa nömrə sahəsinin altında bankın **dizayn işarəsi** (loqo + ad, məs. "Kapital Bank") çıxır; Caspian BIN-idirsə "Caspian Bank"; tapılmasa "Bank tanınmadı".
2. Qalan 8 rəqəm yazılır (16 rəqəm, Luhn yoxlaması).
3. "Review"-da server alıcını tapır: Caspian BIN-idirsə `Cards` cədvəlindən (mövcud məntiq), başqa bank BIN-idirsə `ExternalCards`-dan. Tapılsa alıcının adı və bankı göstərilir, banklararası komissiya hesablanıb göstərilir. Tapılmasa "bu bankda belə kart tapılmadı".
4. "Confirm": göndərənin balansından məbləğ + banklararası komissiya çıxılır, jurnala `InterbankOut` və `InterbankCommission` yazılır; alıcı tərəfdə balans saxlanmır. Qəbzdə alıcının adı və bankı görünür.

## 18. Qərar və izah (2026-10-10)
- **Alıcının adı qısaldılmış göstərilir** (`Elçin M.`), həm daxili, həm banklararası köçürmədə (məxfilik).
- `Loan.DisbursedCardId` və `Loan.DisbursedAt` izah olundu: kredit təsdiq olunanda pul müştərinin kartlarından birinə yazılır. `DisbursedCardId` pulun düşdüyü kartın id-si, `DisbursedAt` nə vaxt düşdüyüdür (rədd olunanda və ya gözləyəndə ikisi də boşdur). Təklif: kartı müraciət zamanı müştəri seçsin; onda `DisbursedCardId` əvəzinə `TargetCardId` (seçilən kart) + `DisbursedAt` qalır. Kart bloklu və ya Cashback ola bilməz (Cashback kartına pul yatırılmır). İstifadəçinin qərarı gözlənilir.

## 19. Qərar (2026-10-10): kredit kartını müştəri müraciətdə seçir, kart nömrəsi ilə
- Müştəri krediti hansı karta almaq istədiyini **müraciət zamanı seçir**.
- Kart `int` id ilə yox, **kart nömrəsi ilə** saxlanılır. `Loan`-da `DisbursedCardId` əvəzinə: `string TargetCardNumber` (16 rəqəm, boşluqsuz) + `Card TargetCard` (nömrə ilə bağlanır; `Card.CardNumber` unikaldır, istinad `HasPrincipalKey(c => c.CardNumber)` ilə qurulur). `DisbursedAt` qalır.
- Müraciətdə yoxlama: kart müraciət edənin öz kartıdır, bloklu deyil, Cashback deyil. Ekranlarda nömrə maskalanır (məs. `4532 •••• •••• 1234`), tam nömrə bazada saxlanılır.

## 20. Yekun model siyahısı (2026-10-10)
Bölmə 14.3-ə nisbətən son dəyişikliklər: bank üçün **A variantı** (`BankBin { BankName, LogoUrl, Bin, IsCaspian }`, ayrıca `Bank` yoxdur), `CitizenCreditRecord.BankName` mətn, `Loan.DisbursedCardId` əvəzinə `TargetCardNumber` + `TargetCard`, `Loan.ClosedEarly`, `Loan.CourtAt`, `LoanInstallment.IsCancelled`, `PaymentStatus` və `CardNetwork` yoxdur. Alıcının adı qısaldılmış göstərilir. Tam kod şərhlərlə istifadəçiyə göstərildi; repoya hələ yazılmayıb.

## 21. İş tarixçəsi (2026-10-10, istifadəçinin tələbi)
İşçinin (müştərinin) əvvəlki iş yerləri də görünməlidir: vəzifə və maaş. Əlaqə **bir-çox** (one-to-many): bir vətəndaşın bir neçə iş yeri olur.
- Yeni model **`WorkPlace`**: `CitizenId`, `CompanyName`, `Position`, `MonthlySalary`, `StartDate`, `EndDate?` (boşdursa **hazırda işləyir**), `LeaveReason?`.
- `Citizen`-dən `WorkPlace` (mətn) və `MonthlyIncome` çıxarılır; əvəzinə `Citizen.WorkPlaces` kolleksiyası. Hazırkı maaş = `EndDate` boş olan sətrin `MonthlySalary`-si (işsizdirsə 0). `Loan.MonthlyIncomeAtApply` kredit müraciəti anında bu maaşın snapshot-ıdır; maaşa nisbət (DTI) limiti də bunu götürür.
- Qayda: bir vətəndaşın eyni vaxtda **yalnız bir** hazırkı iş yeri (`EndDate IS NULL`) ola bilər (bazada filtrlənmiş unikal indeks).
- İşçi kredit hesabatında iş tarixçəsi siyahısı görünür (hazırkı + əvvəlkilər).

## 22. Düzəliş (2026-10-10): iş yeri çox-çox əlaqədir. Bölmə 21-in modelini əvəz edir
İstifadəçi düz qeyd etdi: bir işçi bir neçə şirkətdə işləyə bilər, bir şirkətdə çox işçi ola bilər, yəni **many-to-many**. Vəzifə, maaş və tarixlər isə **əlaqənin özünə** aiddir. Ona görə EF Core-da əlaqə **açıq birləşdirici model** (join entity) ilə qurulur:

```
Citizen 1 ──< N Employment N >── 1 WorkPlace
```
- **`WorkPlace`** (şirkət/iş yeri özü): `Name` (məs. "SOCAR"), `Voen` (10 rəqəm, unikal), `Sector`, `Address`. Hər şirkət bir dəfə yazılır, ad təkrarlanmır.
- **`Employment`** (birləşdirici, iş fakti): `CitizenId`, `WorkPlaceId`, `Position`, `MonthlySalary`, `StartDate`, `EndDate?` (boş = hazırda işləyir), `LeaveReason?`.
- `Citizen.Employments` kolleksiyası; `WorkPlace.Employments` kolleksiyası (bu şirkətdə kim işləyir/işləyib).
- Qaydalar: eyni vətəndaşın eyni vaxtda yalnız bir hazırkı işi (`EndDate IS NULL`, filtrlənmiş unikal indeks); eyni `(CitizenId, WorkPlaceId, StartDate)` təkrar olmaz.
- EF-in avtomatik "skip navigation" çox-çox əlaqəsi burada **uyğun deyil**, çünki birləşdirici cədvəldə əlavə sahələr (vəzifə, maaş, tarix) var.
- Bölmə 21-dəki `WorkPlace { CitizenId, CompanyName, Position... }` modeli ləğv olundu (o, əslində `Employment` idi, şirkət adı isə mətn kimi təkrarlanırdı).

## 23. Başqa çox-çox (many-to-many) əlaqələr (2026-10-10, müzakirə)
Qiymətləndirmə (güclüdən zəifə):
1. **Vətəndaş ↔ Bank (kreditlər)**: bir vətəndaş çox bankdan kredit götürür, bir bankda çox borclu var; kredit məbləği, ödənilən, gecikmə kimi sahələr əlaqəyə aiddir. `CitizenCreditRecord` artıq birləşdirici modeldir, amma bank mətn (`BankName`) kimi yazılıb. Düzgün qurulması üçün real `Bank` varlığı lazımdır: `Bank 1──< BankBin`, `Citizen 1──< CitizenCreditRecord >──1 Bank`. (Bu, bölmə 17-dən əvvəl təklif olunan B variantıdır; A variantı mətn ilə işləyir.) Qərar gözlənilir.
2. **Müştəri ↔ Ödəniş təchizatçısı (Bills)**: müştəri çox təchizatçını (işıq, qaz, su, telefon) saxlayır, təchizatçının çox müştərisi var; əlaqəyə abonent/müqavilə nömrəsi və ləqəb aiddir (məs. `BillSubscription`). Bills funksiyası yazılanda.
3. **Kredit ↔ Vətəndaş (zaminçi)**: bir kreditin borcalanı və zaminçisi olur, bir vətəndaş çox kreditə zamin ola bilər (əlaqədə rol: `Borrower/Guarantor`). İstəyə bağlı.
4. **AppUser ↔ Role**: artıq mövcuddur (Identity: `AspNetUserRoles`).
5. **Role ↔ Bölmə icazəsi**: rol matrisi bazada dəyişdirilə bilsin istənilərsə (hazırda `AdminSections` kodda). Sonra.
Çox-çox olmayanlar (1-N): `Card→User`, `Loan→Installment`, `CardDesign→CardTierConfig`, bildirişlər (hər istifadəçiyə ayrı sətir), `ExternalCard→Citizen` (istəyə bağlı, boş ola bilən `CitizenId`).

## 24. Qərarlar və izahlar (2026-10-10)
1. **`LoanPurpose.Other` seçilərsə səbəb mətni məcburidir.** `Loan.PurposeNote` və `CustomerCreditRecord.PurposeNote`: `Purpose == Other` olanda boş ola bilməz (5–200 simvol). Müraciət formasında "Other" seçiləndə mətn sahəsi görünür. Başqa səbəblərdə qeyd istəyə bağlıdır.
2. **`Citizen` modelinin adı `Customer` olur** (istifadəçinin istəyi). Dəyişikliklər: `Citizen`→`Customer`, `CitizenCreditRecord`→`CustomerCreditRecord`, `CitizenId`→`CustomerId` (`Employment`, `CustomerCreditRecord`, `AppUser`), cədvəl `Customers`. Qarışıqlıq olmasın: `AppUser` = giriş hesabı, `Customer` = dövlət reyestrindəki şəxs (hesabı olmaya da bilər), `Roles.Customer` yalnız rol adıdır.
3. **Qəbul olundu:** Müştəri ↔ Ödəniş təchizatçısı (`BillSubscription`, Bills yazanda) və Kredit ↔ Müştəri zaminçi əlaqəsi.
4. **`LoanGuarantor`** (zaminçi, çox-çox birləşdirici): `LoanId`, `CustomerId`, `Relationship` (məs. "ata"), `GuaranteedAmount`. Qaydalar: zaminçi sağdır, 18+, borcalanın özü deyil, `CreditStatus != Blacklisted`; `(LoanId, CustomerId)` unikal. İşçi zaminçinin də tam hesabatına baxır.
5. **BIN izahı** (istifadəçi soruşdu): BIN = kartın ilk 8 rəqəmi, kartı çıxaran bankı göstərir. `BankBin` cədvəli "BIN → bank adı və loqo" lüğətidir. A/B variantları tam izah olundu, qərar gözlənilir.

## 25. Yekun adlar və əlaqələr (2026-10-10). Bölmə 14, 17, 22, 24-ün adlarını və bank modelini əvəz edir
**Qərarlar:** (1) Visa/Mastercard yoxdur; hər bankın **bir** sabit 8 rəqəmli BIN-i var (A variantı). (2) Model adları **Azərbaycanca** (hələlik, ASCII). (3) ID kartın nömrəsi sahəsi `SeriyaNomresi`. (4) Öz bankımız ayrı model (`OzBank`), `Kredit` ilə bir-çox. (5) `Musteri` ↔ `XaricBank` çox-çox (`XaricKredit` birləşdirici). (6) `Musteri` ↔ `Kredit` çox-çox (`Zaminci` birləşdirici). (7) `Musteri` ↔ `XaricKredit` arasında ayrıca çox-çox lazım deyil (`XaricKredit` onsuz da `Musteri`-`XaricBank` birləşdiricisidir).

**Adlar:** `Citizen`→`Musteri`; `CitizenCreditRecord`→`XaricKredit`; `BankBin`→`XaricBank` (`Name`, `LogoUrl`, `Bin`); öz bankımız `OzBank`; `ExternalCard`→`XaricKart`; `WorkPlace`→`IsYeri`; `Employment`→`IsQeydi`; `Loan`→`Kredit`; `LoanInstallment`→`KreditOdenisi`; `LoanGuarantor`→`Zaminci`. Enum-lar: `HeyatVeziyyeti {Diri, Olu}`, `KreditStatusu {Temiz, Gecikmeli, QaraSiyahi}`, `KreditMeqsedi {Istehlak, Avtomobil, Ipoteka, Tehsil, Mualice, Temir, Biznes, Diger}`, `KreditVeziyyeti {Gozleyir, Redd, Aktiv, Baglanib, Mehkeme}`. Sahə adları hələlik ingiliscə qalır (`SeriyaNomresi` istisna); `TransactionType` dəyərləri mövcud üslubda ingiliscə.

**`OzBank`** (tək sətir): `Name`, `LogoUrl`, `Bin` (8 rəqəm), kredit qaydaları `FixedPenalty` (10), `DailyPenaltyPercent` (0,5), `MinLoanAge` (18), `CourtAfterDays` (90), sonradan deyiləcək boş ola bilən `MaxPaymentToIncomePercent`, `InterbankFee...`; `Kreditler` kolleksiyası. `Kredit.OzBankId`.
**`XaricKredit`**: `MusteriId`, `XaricBankId` (siyahıdan seçilir, mətn yox), digər sahələr əvvəlki kimi (+ `PurposeNote`). **`XaricKart`**: `XaricBankId`, `CardNumber` (ilk 8 rəqəm bankın `Bin`-i ilə eyni), `HolderName`, `IsActive`.
**Əlaqələr:** `Musteri 1─< IsQeydi >─1 IsYeri`; `Musteri 1─< XaricKredit >─1 XaricBank`; `Musteri 1─< Zaminci >─1 Kredit`; `OzBank 1─< Kredit`; `XaricBank 1─< XaricKart`; `Musteri 1─0..1 AppUser` (`AppUser.MusteriId`), `AppUser 1─< Kredit` (borcalan).
BIN axtarışı: əvvəl `OzBank.Bin`, sonra `XaricBank.Bin`.
Açıq: "öz bank modelim ilə bir-çox" əlaqənin hansı modellə olduğu (mən `Kredit` götürdüm; kart və s. də ola bilər) — istifadəçinin təsdiqi gözlənilir.

## 26. Aydınlaşdırma (2026-10-10): kart modeldir, növü enum-dur
- **`Card`** = **model** (cədvəl `Cards`): müştərinin real kartı (nömrə, balans, bloklu və s.). Mövcud modeldir, adı **dəyişmir** (bölmə 25-də `Kart` yazılması səhv idi; `Kredit.TargetCard` tipi `Card`-dır).
- **`CardTier`** = **enum** (`Cashback`, `Standard`, `Silver`, `Gold`): kartın növü. Qaydaları `CardTierConfig` modelindədir; dizaynı `CardDesign` modelindədir.
- `OzBank 1─< N Card` əlaqəsi mümkündür (hər Caspian kartı `OzBank.Bin` ilə başlayır); istifadəçinin seçimi gözlənilir (`Kredit`, `Card` və ya ikisi).

## 27. Enum lazımdırmı? (2026-10-10, müzakirə, qərar gözlənilir)
Enum = adlandırılmış sabit dəyərlər (bazada rəqəm kimi saxlanır). Alternativlər: `bool`, mətn, ayrıca cədvəl. Qayda: dəyərlər az, sabit və kod onlara görə qərar verirsə enum yaxşıdır; dəyərləri admin dəyişməlidirsə cədvəl; yalnız iki hal varsa bool; başqa sahədən çıxarıla bilirsə heç biri.
Təklif:
- `CardTier` (mövcud): **qalır** (kodda `Cashback` qaydaları buna söykənir; ləğvi böyük refaktor).
- `HeyatVeziyyeti {Diri, Olu}`: **ləğv**, `DeathDate` kifayətdir (boş = diri). İki ayrı sahənin uyğunsuz olması riski yox olur.
- `KreditStatusu {Temiz, Gecikmeli, QaraSiyahi}`: **ləğv**, əvəzinə `bool QaraSiyahi` + `StatusReason`; "gecikməli" `XaricKredit` və `Kredit` məlumatından hesablanır, "kirli/təmiz" etiketi hesabatda hesablanır (məhkəmə, gecikmə, qara siyahı göstərilir).
- `KreditMeqsedi`: **qalır** (sabit siyahı, açılan siyahı üçün); admin dəyişəcəksə cədvələ çevrilə bilər.
- `KreditVeziyyeti {Gozleyir, Redd, Aktiv, Baglanib, Mehkeme}`: **qalır** (real iş axını, sorğular sadə qalır).
- `TransactionType` (mövcud): qalır.
Texniki məsləhət: bazada enum-lar mətn kimi saxlana bilər (`HasConversion<string>()`), onda SSMS-də `0/1/2` əvəzinə `Diri/Olu` görünür.

## 28. Yekun model siyahısı və çatışmazlıqlar (2026-10-10)
Bölmə 25-dən fərq: enum təklifi tətbiq olunmuş göstərilib (`HeyatVeziyyeti` və `KreditStatusu` ləğv; `Musteri`-də `DeathDate`, `QaraSiyahi`, `StatusReason`). Qalan enum-lar: `KreditMeqsedi`, `KreditVeziyyeti`. Model siyahısı: `Musteri`, `IsYeri`, `IsQeydi`, `XaricBank`, `XaricKredit`, `XaricKart`, `OzBank`, `Kredit`, `KreditOdenisi`, `Zaminci`; əlavələr `AppUser.MusteriId`, `TransactionType` (4 yeni dəyər **sonda** əlavə olunmalıdır, çünki bazada rəqəm kimi saxlanır).

**Çatışmazlıqlar və açıq məsələlər:**
A. Qərar gözləyənlər: enum ləğvi təsdiqi; `OzBank` ilə `Kredit` və/və ya `Card` əlaqəsi; sahə adlarının Azərbaycanca olması; Caspian BIN-i; banklararası komissiya; faiz və faiz endirimi/məbləğ azalma düsturu; maaşa nisbət limiti faizi; annuitet metodu.
B. Modeldə: (1) `AppUser`-də `Name/Surname/BirthDay/FinKod` reyestrlə təkrarlanır (iki həqiqət mənbəyi); (2) hər ödənişin ayrıca qeydi yoxdur (`KreditOdenisi.PaidAmount` cəmdir; ayrıca `KreditPayment` istəyə bağlı); (3) `AccruedPenalty` saxlanılan dəyər köhnələ bilər, hesablanan dəyər əsasdır; (4) yalnız tam erkən ödəniş modelləşib, hissəvi erkən ödəniş yoxdur; (5) `XaricKart` müştəriyə bağlı deyil (ad mətn), istəyə bağlı `MusteriId?`; (6) zaminçidə razılıq/təsdiq və zaminlik limiti qaydası yoxdur; zaminlik zaminçinin öz maaşa nisbətinə daxil edilməlidir; (7) gecikmə günü "ən köhnə ödənilməmiş ödənişin vaxtından keçən gün" kimi təyin olunmalıdır; (8) işçi hesabları üçün `Musteri` yoxdur (işçi kredit ala bilməz qaydası lazımdır).
C. Hələ olmayan modellər: `Notification` (xatırlatma), `BillProvider`/`BillSubscription` (Bills).
D. Mövcud sistemə təsir: qeydiyyat axını və `RegisterDto`; `CreateInitialCards` (FİN formu) ləğv olunur, kartlar qeydiyyatda açılır; mövcud 18 test istifadəçisi və `4532` kartları təmizlənməlidir; `CardNumberGenerator` BIN; transfer BIN axtarışı; rol matrisində kredit hesabatını hansı rolların görəcəyi qərarı (təklif: SuperAdmin, Admin, Accountant, Security).

## 29. Bank sisteminə uyğun yenidən qurulmuş model (Azərbaycanca ad və sahələrlə, 2026-10-10)
İstifadəçi modellərin bank sisteminə mümkün qədər düzgün qurulmasını və hələlik Azərbaycanca adlanmasını istədi (sonra ingiliscəyə çevirəcək). Əvvəlki çatışmazlıqlar düzəldildi: (1) kredit şərtləri **müqavilə anına köçürülür** (snapshot: faiz, cərimə dərəcələri), (2) **kredit məhsulu** (`KreditMehsulu`): faiz, məbləğ/müddət hədləri, min yaş, cərimə dərəcələri; `OzBank 1─< KreditMehsulu 1─< Kredit`, (3) hər ödənişin ayrıca qeydi (`KreditOdenisQeydi`: cərimə/faiz/əsas borc hissələri, kart, əməliyyat mərcəyi), (4) zaminçidə razılıq və limit (`OzBank.MaxZaminlikSayi`), (5) `XaricKart.MusteriId?`, (6) `Bildiris` modeli, (7) `AppUser.MusteriId` və `AppUser.FinKod`-un ləğvi təklifi.
Ad uyğunluğu (sonra ingiliscəyə çevirmək üçün): `Musteri`=Customer, `IsYeri`=Workplace, `IsQeydi`=Employment, `XaricBank`=ExternalBank, `XaricKredit`=ExternalLoan, `XaricKart`=ExternalCard, `OzBank`=OwnBank, `KreditMehsulu`=LoanProduct, `Kredit`=Loan, `AylikOdenis`=LoanInstallment, `KreditOdenisQeydi`=LoanPaymentRecord, `Zaminci`=LoanGuarantor, `Bildiris`=Notification, `KreditMeqsedi`=LoanPurpose, `KreditVeziyyeti`=LoanStatus.
Tam kod istifadəçiyə şərhlərlə göstərildi; repoya yazılmayıb. `BaseEntity`-nin `Id` və `CreatedAt` sahələri dəyişmir.

## 30. İngilis adları ilə yekun model (2026-10-10)
İstifadəçi modelləri ingiliscə adlarla, Azərbaycanca izahla istədi. Ad uyğunluğu bölmə 29-dakı cədvəl üzrədir; əlavə: `SeriyaNomresi`=`IdCardNumber`, `Voen`=`TaxId`, `QaraSiyahi`=`IsBlacklisted`, `QaraSiyahiSebebi`=`BlacklistReason`, `YasayisUnvani`=`ResidenceAddress`, `BorcalanHesabId`=`BorrowerUserId`, `MuracietAndaMaas`=`IncomeAtApply`, `HedefKartNomresi`=`TargetCardNumber`, `OdenisGunu`=`PaymentDay`, `MaxZaminlikSayi`=`MaxGuaranteeCount`, `MehkemeyeKecidGunu`=`CourtAfterDays`, `RazilasdiMi`=`HasConsented`, `EmeliyyatMerceyi`=`TransactionReference`. Class adları: `Customer`, `Workplace`, `Employment`, `ExternalBank`, `ExternalLoan`, `ExternalCard`, `OwnBank`, `LoanProduct`, `Loan`, `LoanInstallment`, `LoanPaymentRecord`, `LoanGuarantor`, `Notification`; enum-lar `LoanPurpose`, `LoanStatus`. Repoya hələ yazılmayıb.

## 31. `LoanGuarantor` silinsin? (2026-10-10, müzakirə, qərar gözlənilir)
Təklif: **ilk versiyadan çıxarmaq, planda II mərhələ kimi saxlamaq.** Səbəblər: kredit, qeydiyyat, Bills, Support, bildirişlər hələ yazılmayıb və son tarix yaxındır; heç bir başqa funksiya zaminçiyə söykənmir; sonradan əlavə etmək yalnız bir migration və kiçik işçi ekranıdır. Çıxarılarsa: `LoanGuarantor`, `Customer.Guarantees`, `Loan.Guarantors`, `OwnBank.MaxGuaranteeCount`. `Customer ↔ Loan` çox-çox əlaqəsi qalmır (iki çox-çox əlaqə qalır: `Employment`, `ExternalLoan`). Qalarsa: yalnız işçi ekranı (kredit baxışında FİN ilə zaminçi əlavə etmək), müştəri tərəfdə forma yoxdur.

## 32. Qərar (2026-10-10): zaminçi silindi
`LoanGuarantor` planın **hamısından çıxarıldı** (II mərhələ də deyil). Çıxanlar: `LoanGuarantor` modeli, `Customer.Guarantees`, `Loan.Guarantors`, `OwnBank.MaxGuaranteeCount`, zaminçi qaydaları və bölmə 24-dəki zamin bəndi. Bölmə 14, 24, 25, 28, 29, 30-dakı zaminçi qeydləri **ləğv olunub**. Çox-çox əlaqələr iki qalır: `Customer ↔ Workplace` (`Employment`) və `Customer ↔ ExternalBank` (`ExternalLoan`). Yekun model siyahısı: `Customer`, `Workplace`, `Employment`, `ExternalBank`, `ExternalLoan`, `ExternalCard`, `OwnBank`, `LoanProduct`, `Loan`, `LoanInstallment`, `LoanPaymentRecord`, `Notification` (+ enum-lar `LoanPurpose`, `LoanStatus`; `AppUser.CustomerId`; `TransactionType` yeni dəyərləri).

## 33. Qərarlar (2026-10-10): `LoanProduct` silindi, hissəvi erkən ödəniş əlavə olundu
1. **`LoanProduct` ləğv.** Kredit qaydaları (`MinLoanAmount`, `MaxLoanAmount`, `MinTermMonths`, `MaxTermMonths`, `AnnualInterestRate`, `MinAge`, `FixedPenalty`, `DailyPenaltyPercent`, `CourtAfterDays`) yenidən `OwnBank`-dadır. `Loan.LoanProductId` əvəzinə `Loan.OwnBankId`; əlaqə `OwnBank 1─< Loan`. Kreditin üzərində faiz və cərimə dərəcələrinin surəti saxlanılır (`Loan.AnnualInterestRate`, `FixedPenalty`, `DailyPenaltyPercent`).
2. **`AppUser.FinKod` hələlik qalır**, sonra ləğv olunur.
3. **`LoanPaymentRecord` qalır** və yeni sahə alır: `Type` (`LoanPaymentType { Installment, PartialEarly, FullEarly }`).
4. **Hissəvi erkən ödəniş** əlavə olundu, tam erkən bağlama onun xüsusi halıdır.
   Qaydalar (sadə, defolt): (a) yalnız aktiv kredit və gecikmiş ödəniş/cərimə yoxdursa; (b) məbləğ **yalnız əsas borca** gedir; (c) qalan ödənilməmiş aylar yenidən hesablanır: müddət eyni, aylıq ödəniş azalır (`LoanInstallment` sətirləri və `Loan.MonthlyPayment` yenilənir); (d) qalan əsas borcdan çox ödəmək olmaz, bərabərdirsə tam bağlamadır; (e) tam erkən bağlamada ödənəcək = qalan əsas borc + cari dövrün yığılmış faizi (`qalıq × illik faiz / 365 × son ödəniş tarixindən keçən gün`) + ödənilməmiş cərimələr; qalan aylar `IsCancelled`, `Status = Closed`, `ClosedEarly = true`, `ClosedAt` yazılır. (f) Sadələşdirmə: hissəvi ödənişdə ayın ortasında gündəlik faiz hesablanmır, azalan qalıq növbəti ayın faizinə təsir edir. (g) Faiz endirimi qaydaları üçün sonra `ClosedEarly` və `PartialEarly` qeydlərinin sayı istifadə olunacaq.
Yekun modellər (11): `Customer`, `Workplace`, `Employment`, `ExternalBank`, `ExternalLoan`, `ExternalCard`, `OwnBank`, `Loan`, `LoanInstallment`, `LoanPaymentRecord`, `Notification`.

## 34. Dəqiq dəyərlər və bütün layihə üçün açıq suallar (2026-10-10)
**Dəqiq dəyərlər (istifadəçi):** Caspian BIN = **`11223456`** (`1122 3456`), banklararası komissiya = **1%** (`OwnBank.InterbankFeePercent = 1.00`). Qeyd: `1` ilə başlayan kartlar real həyatda aviaşirkətlərə ayrılıb, simulyasiya üçün problem deyil.
**Açıq suallar (defolt cavab mənim təklifimdir; "defolt" yazmaq kifayətdir):**
A. Qeydiyyat: A1 telefon unikal (defolt bəli); A2 saxta data şablonu: mən boş seeder faylı hazırlayıram, istifadəçi doldurur (defolt).
B. Kredit: B1 faiz (sabit, yoxsa müddətə görə); B2 faiz endirimi qaydası; B3 gecikməyə görə məbləğin azalması (maaş × qat: gecikməsiz 12, 1–2 gecikmə 6, 3+ 2, məhkəməlik rədd); B4 maaşa nisbət limiti (45%); B5 min/max məbləğ və müddət (500–20000 AZN, 6–60 ay); B6 ödəniş günü (pul verilən günün nömrəsi, max 28); B7 annuitet (bəli); B8 eyni vaxtda max 2 aktiv kredit, gecikmə varsa yeni müraciət olmaz; B9 təsdiq edənlər (Accountant, Admin, SuperAdmin), hesabatı görənlər (SuperAdmin, Admin, Accountant, Security); B10 rədd səbəbi müştəriyə ümumi mətn; B11 pul avtomatik hədəf karta düşür; B12 90 gün gecikəndə Court avtomatik; B13 hissəvi ödənişdə aylıq azalır, müddət eyni; B14 ödəniş istənilən aktiv öz kartından.
C. Banklararası: C1 1% məbləğin üstünə əlavə olunur; C2 minimum komissiya yoxdur; C3 yalnız 1% (kart növü komissiyası tutulmur); C4 limit: mövcud 100 000/köçürmə; C5 top-up hələlik olduğu kimi.
D. Bills: D1 təchizatçı siyahısını mən hazırlayıram; D2 abonent nömrəsi → saxta borc cədvəli; D3 bill ödənişindən Cashback; D4 komissiya yoxdur; D5 "Loan payment" kateqoriyası.
E. Support: E1 ticket + işçi cavabı + müştəri yazışması; E2 fayl yoxdur; E3 statuslar Açıq/İşdə/Bağlı; E4 cavab verən CustomerSupport və Admin.
F. Bildirişlər: F1 səhifə açılanda (SignalR sonra); F2 hadisələr: gələn köçürmə, kredit qərarı, ödənişə 3 gün qalıb, gecikmə, kart bloku; F3 email xatırlatması yoxdur.
G. Bank/admin: G1 xəzinə ilkin 1 000 000 AZN, çatmasa kredit rədd; G2 depozit planda qalır (6 ay 5%, 12 ay 7%, 24 ay 8%); G3 hesabatlar/audit; G4 hesab dondurma (Security, Admin, SuperAdmin); G5 işçi yaratma və rol matrisi istifadəçidədir.
H. Digər: H1 Trust indicators və footer sonda; H2 müəllimin cavabı; H3 OTP bloku və WebDesigner silinməsi commit olunsun?; H4 mövcud test istifadəçilərinin silinməsi; H5 hansı MVC səhifələrini istifadəçi özü yazır.
İlk mərhələ (qeydiyyat + reyestr + avtomatik kartlar + profil + BIN + banklararası köçürmə) üçün blokerlər: A1, A2, C1–C3 və müəllimin təsdiqi.

## 35. Qərarlar (2026-10-10, istifadəçinin cavabları)
**Qəbul olunanlar** ("he" = bəli kimi başa düşülüb):
- **C3**: başqa banka köçürmədə **hər iki komissiya** tutulur: kartın növünə görə (limiti aşan hissənin faizi) + 1% banklararası. Nümunə (Standard, limit 500, komissiya 1%): 700 AZN → növ komissiyası (700−500)×1% = 2.00, banklararası 1% = 7.00, göndərəndən çıxan 709.00.
- **B1**: illik faiz **10%** (azaldılmır, bazar üçün aşağıdır).
- **B3**: məbləğ = maaş × qat, qatlar 2-yə vurulub: gecikməsiz **24**, 1–2 gecikmə **12**, 3+ gecikmə **4**. Əlavə hədlər: `OwnBank.MaxLoanAmount`, maaşa nisbət limiti.
- **B5**: maksimum kredit **50 000 AZN** (minimum 500, müddət 6–60 ay).
- **B6**: ödəniş günü **hər ayın son günü** (28/29/30/31). `Loan.PaymentDay` ləğv.
- **B8**: eyni vaxtda ən çox **3 aktiv** Caspian krediti (`OwnBank.MaxActiveLoans = 3`). Gecikməsi varsa avtomatik qadağa yox, admin baxıb qərar verir.
- **Aylıq ödəniş sistemi** (bill-payment.html dizaynında "Recurring payments"): kredit də orada görünür.
- **B13**: hissəvi erkən ödənişdə **faiz dərəcəsi azalır** (məs. 10% → 9%). `Loan.InitialAnnualInterestRate` əlavə olunur (ilkin müqavilə faizi), `AnnualInterestRate` cari faizdir.
- **C4**: banklararası köçürmə bir dəfəyə ən çox **10 000 AZN** (`OwnBank.MaxInterbankAmount`).
- **C5**: top-up yalnız `ExternalCard` cədvəlindəki kartlardan; ona görə `ExternalCard`-a `ExpiryDate`, `Cvv`, `Balance` (saxta) əlavə təklif olunur (təsdiq gözlənilir).
- **D1**: təchizatçılar dizayndakılardır: Azərişıq, Azərsu, Azəriqaz, Azeristilik, Azercell, Bakcell, CityNet.
- **D4**: kommunal ödənişdə komissiya **olacaq** (dəyəri soruşulacaq).
- **Support** hələ qalır; istifadəçi sonra silib onlayn çat qoya bilər (ticket sistemi yazılmır).
- **F1**: SignalR **məcburidir**.
- **G1**: kredit xəzinə çatışmazlığına görə **rədd olunmur**.
**Nəticə:** SignalR və təkrarlanan ödənişlər üçün gündəlik arxa plan xidməti (BackgroundService) lazım olacaq: təkrarlanan ödənişləri icra edir, gecikməni yeniləyir, xatırlatmaları SignalR ilə göndərir.
**Yenidən soruşulanlar** (cavab verilməyənlər + yeniləri): A1, A2, C1, C2, B2, B4, B7, B9, B10, B11, B12, B14, D2, D3, D5, F2, F3, G2, G3, G4, G5, H1–H5; yeni: N1 faiz azalma qaydası (hissəvi ödənişdə), N2 ilk ödəniş ayı, N3 kommunal komissiya dəyəri, N4 kredit avtomatik çıxılsın?, N5 kredit/kommunal gün fərqi, N6 arxa plan xidməti, N7 `ExternalCard` sahələri, N8 məhkəməlik müştəri.

## 36. Tamlıq yoxlaması (2026-10-10): 11 model bütün layihə üçün tam deyil
Qeydiyyat/reyestr, xarici banklar və kredit (əsas hissə) üçün modellər yetərlidir. Bütün layihə üçün çatışmayanlar:
**Mütləq lazım (qəbul olunmuş funksiyalar):**
1. **Bills/təkrarlanan ödənişlər:** `BillProvider`, `BillAccount` (saxta abonent və borc), `RecurringPayment`, `BillPayment`; enum `BillCategory`.
2. **`InterbankTransfer`** (başqa banka köçürmənin qeydi: göndərən kart, `ExternalCard`, məbləğ, növ komissiyası, banklararası komissiya). Onsuz qəbz və təkrar sorğu (idempotency) üçün alıcı məlumatı bərpa olunmur, çünki alıcı `Card` deyil.
3. **`TransactionType`**-a əlavə dəyərlər: `BillPayment`, `BillCommission`, `Cashback` (depozit olarsa `DepositOpen`, `DepositPayout`).
4. **`Loan`**: `ContractNumber` (məs. `KR-2026-000123`), `RepaymentCardNumber` (avtomatik çıxılan kart, N4); `LoanPaymentRecord.InterestRateAfter?`; **`OwnBank.BillCommissionPercent`** (D4).
**Bank/admin səhifələri üçün (planda):**
5. **`TreasuryEntry`** + enum `TreasuryEntryType` (Bank dashboard: gəlir, xərc, mənfəət vergisi 20%, ehtiyat 10%).
6. **`AuditLog`**; 7. **`TransactionReport`** (müştəri əməliyyatı şikayət edir); 8. **`Deposit`** (G2 təsdiqlənərsə).
9. Kiçik: `AppUser.RestrictionReason`, `RestrictedAt`; `Card.BlockedByBank` (bank bloklayıb, müştəri aça bilməz).
**Sonra/istəyə bağlı:** onlayn çat modelləri (`ChatConversation`, `ChatMessage`), `Notification.Type`.
Dizayn uyğunsuzluğu: `loan-application.html`-də müddət seçimləri 6–48 ay, `OwnBank.MaxTermMonths = 60`; ya 48-ə endirilir, ya dizayna 60 əlavə olunur.

## 37. Xatırlatma (2026-10-10)
Aylıq kredit ödənişi **Bill Payments yazılanda** əlavə olunacaq ("Recurring payments" cədvəlində, "Loan payment" kateqoriyası ilə). Bills başlayanda ilk bunu xatırlatmaq lazımdır. O vaxt həll olunacaq suallar: N4 (avtomatik çıxılsın, yoxsa müştəri özü ödəsin; `Loan.RepaymentCardNumber`), N5, D2–D5, N6 (gündəlik arxa plan xidməti), hissəvi/tam erkən ödənişin də Bills-dən edilib-edilməməsi.

## 38. Qərarlar (2026-10-10): maksimum müddət 48 ay, azaldılmış məbləğə müştəri razılıq verir
- `OwnBank.MaxTermMonths = 48` (müddət seçimləri dizayndakı kimi: 6, 12, 18, 24, 36, 48 ay).
- **Azaldılmış məbləğ müştərinin razılığı ilə:** işçi istənilən məbləğdən az təsdiq edəndə pul avtomatik düşmür; müştəri "4000 AZN-ə razıyam" deyir. Yeni statuslar: `Offered` (təklif edilib, müştərinin cavabı gözlənilir) və `Declined` (müştəri imtina edib və ya təklifin vaxtı bitib).
- `Loan`-a yeni sahələr: `OfferExpiresAt` (təklifin etibarlılıq vaxtı, defolt 7 gün), `AcceptedAt` (müştəri razılaşdığı vaxt).
- İş axını: `Pending → Rejected | Active` (tam məbləğ təsdiq olunsa) `| Offered` (azaldılmış məbləğ); `Offered → Active` (müştəri razılaşır: cədvəl yaranır, pul düşür) `| Declined`; `Active → Closed | Court`; `Court → Closed`.
- Təklif zamanı aylıq ödəniş təsdiq olunan məbləğə görə hesablanıb müştəriyə göstərilir; müştəriyə SignalR bildirişi gedir. Faiz və cərimə dərəcələri (müqavilə surəti) təklif anında yazılır.

## 39. YEKUN KREDİT HİSSƏSİ (2026-10-10). Bu bölmə kredit modelləri üçün əsas mənbədir, əvvəlki bölmələri əvəz edir
Enum-lar: `LoanPurpose {Consumer, Car, Mortgage, Education, Medical, Repair, Business, Other}` (Other olanda `PurposeNote` məcburi, 5–200 simvol); `LoanStatus {Pending, Rejected, Offered, Declined, Active, Closed, Court}`; `LoanPaymentType {Installment, PartialEarly, FullEarly}`.
Modellər (9): `Customer`, `Workplace`, `Employment`, `ExternalBank`, `ExternalLoan`, `OwnBank`, `Loan`, `LoanInstallment`, `LoanPaymentRecord`. Əlavələr: `AppUser.CustomerId` (int?, unikal) + `Customer`; `TransactionType`-a (sonda) `LoanDisbursement`, `LoanPayment`.
- `Customer`: `Fin`, `IdCardNumber`, `Name`, `Surname`, `FatherName`, `MotherName`, `BirthDate`, `BirthPlace`, `ResidenceAddress`, `DeathDate?`, `IsBlacklisted`, `BlacklistReason?`, `Employments`, `ExternalLoans`.
- `Workplace`: `Name`, `TaxId`, `Sector`, `Address`. `Employment`: `CustomerId`, `WorkplaceId`, `Position`, `MonthlySalary`, `StartDate`, `EndDate?`, `LeaveReason?`.
- `ExternalBank`: `Name`, `LogoUrl`, `Bin` (8 rəqəm). `ExternalLoan`: `CustomerId`, `ExternalBankId`, `Purpose`, `PurposeNote?`, `BorrowedAmount`, `PaidAmount`, `MonthlyPayment`, `StartDate`, `EndDate?`, `IsClosed`, `DelayCount`, `MaxDelayDays`, `OverdueAmount`, `WentToCourt`, `Note?`.
- `OwnBank`: `Name`, `LogoUrl`, `Bin` (11223456), `MinLoanAmount` (500), `MaxLoanAmount` (50000), `MinTermMonths` (6), `MaxTermMonths` (48), `AnnualInterestRate` (10), `MinAge` (18), `FixedPenalty` (10), `DailyPenaltyPercent` (0,5), `CourtAfterDays` (90), `MaxActiveLoans` (3), `MaxPaymentToIncomePercent?` (45), + köçürmə hissəsi: `InterbankFeePercent` (1), `InterbankMinFee?`, `MaxInterbankAmount` (10000).
- `Loan`: `OwnBankId`, `BorrowerUserId`, `Purpose`, `PurposeNote?`, `ContractNumber`, `RequestedAmount`, `ApprovedAmount?`, `TermMonths`, `MonthlyPayment`, `TotalRepayment`, `IncomeAtApply`, `InitialAnnualInterestRate`, `AnnualInterestRate`, `FixedPenalty`, `DailyPenaltyPercent`, `TargetCardNumber` (+`TargetCard`), `DisbursedAt?`, `Status`, `RejectReason?`, `ReviewedByUserId?`, `ReviewedAt?`, `OfferExpiresAt?`, `AcceptedAt?`, `ClosedAt?`, `ClosedEarly`, `CourtAt?`, `Installments`, `PaymentRecords`.
- `LoanInstallment`: `LoanId`, `Number`, `DueDate` (ayın son günü), `PrincipalAmount`, `InterestAmount`, `PaidAmount`, `FullyPaidAt?`, `FixedPenaltyAmount`, `AccruedPenalty`, `PaidPenalty`, `IsCancelled`.
- `LoanPaymentRecord`: `LoanId`, `InstallmentId?`, `Type`, `Amount`, `PenaltyPart`, `InterestPart`, `PrincipalPart`, `InterestRateAfter?`, `PaidAt`, `CardNumber`, `TransactionReference`.
Qaydalar: məbləğ = min(`MaxLoanAmount`, maaş × qat), qat 24 (gecikməsiz) / 12 (1–2 gecikmə) / 4 (3+); max 3 aktiv kredit, gecikmə varsa admin baxır; faiz 10%; hissəvi erkən ödənişdə faiz azalır; annuitet, ödəniş hər ayın son günü; cərimə 10 AZN + gündə 0,5%, sıra cərimə → faiz → əsas borc; 90 gündən sonra Court; azaldılmış məbləğdə müştəri razılığı (Offered). Hələ cavab gözləyənlər: B2, B4, B7, B9–B12, B14, N1, N2, N8.
Qeyd: `ExternalCard`, `InterbankTransfer` və `RepaymentCardNumber` bu hissəyə daxil deyil (köçürmə və Bills ilə gəlir).

## 40. 12 oktyabr 2026: ssenari yoxlaması (yenə yalnız dizayn)
- Reyestrdə 30-40 saxta vətəndaş (loginsiz). Onlardan 5-6-sı Caspian-da artıq qeydiyyatlıdır (login var, kartları və Caspian kredit tarixçəsi toxumlanır).
- Hər kreditin öz ödəniş kodu var (`ContractNumber`); Bills-də həmin kod yazılıb kredit ödənir.
- Gecikmə və "ödənişə az qaldı" bildirişləri məcburidir (Notification + SignalR + gündəlik arxa plan xidməti).
- Kredit müştərinin digər öz kartlarından da ödənə bilər; vaxtından əvvəl ödəniş faizi azaldır (`InterestRateAfter`).
- SMS OTP sonra email-in yanına əlavə olunacaq (telefonu istifadəçi qeydiyyatda özü yazır, reyestrdə telefon və email yoxdur). Depozit sonra gəlir, kreditə təsir edə bilər.
