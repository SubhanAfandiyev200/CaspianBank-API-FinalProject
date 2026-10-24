# 99. Sənəd changelog-u

Bu fayl **hər yeni funksiyadan sonra** yenilənir. Hər qeyd: nə vaxt, hansı funksiya, hansı sənəd fayllarına əlavə olundu/dəyişdi.

## Qayda (kim nə edir)
- Yeni funksiya yazılanda aid nömrəli fayla bölmə əlavə olunur (və ya yeni nömrəli fayl yaradılır).
- Burada bir sətir qeyd yazılır: tarix, funksiya, fayllar.
- `00-README.md`-dəki "Hazırkı vəziyyət" və fayl cədvəli yenilənir.
- Sənədləşməmiş funksiyalar aşağıdakı "Gözləyən" siyahısında qalır.

## Qeydlər

### 2026-10-10: ilk tam versiya (bu günə qədər yazılan hər şey)
Sənədləşdirilən funksiyalar:
- Arxitektura, Domain, EF Core, migration-lar: `01`, `02`
- Repository qatı: `03`
- Service qatı, DTO, validator, exception: `04`
- Qeydiyyat/OTP/login/JWT/rollar/şifrə bərpası: `05`
- API controller-lər, middleware, Program.cs, rate limiting: `06`
- Fayl yükləmə (şəkil/video): `07`
- Home məzmun idarəsi (Brand, Ticker, Hero, About, Services, Benefits, CardDesign, CardTier, Settings): `08`
- Kartlar (ilk kartlar, ödənişli kart, top-up, bloklama): `09`
- Köçürmə (qaydalar, komissiya, idempotency, concurrency, qəbz): `10`
- Tarixçə və çıxarış: `11`
- MVC arxitekturası, ApiControllerBase, BearerTokenHandler, ViewComponent-lər: `12`
- MVC giriş axını (cookie içində JWT): `13`
- Admin Area: `14`
- Müştəri səhifələri, JS, CSS: `15`
- Lüğət, yenidən yazma planı, müsahibə sualları: `90`, `91`, `92`

## Gözləyən (hələ yazılmayıb, yazılanda sənədləşəcək)
| Funksiya | Sənəd | Qeyd |
|---|---|---|
| 8 rəqəmli BIN, bank tanıma, başqa banka köçürmə | `93` | plan, müəllimlə dəqiqləşir (reyestr və FİN ilə qeydiyyat artıq `94`, `95`-dədir) |
| Dəstək / Contact / Tickets | yeni fayl | `/App/Support` hələ yoxdur |
| Bank xəzinəsi (treasury) və Bank dashboard | yeni fayl | |
| Depozit (müddətli, faizli) | yeni fayl | |
| Serilog ilə loglama (fayla yazma, səviyyələr) | yeni fayl | ən sonda yazılacaq |
| Users admin, hesab dondurma, işçi idarəsi, rol matrisi | `05`, `14` genişlənəcək | rol matrisini istifadəçi yazır |
| Reports və Audit log | yeni fayl | |
| Footer | `12`, `08` | sonda tamamlanacaq |
| Unit testlər | `91` | |

### 2026-10-10 (axşam): kart bloku OTP ilə, WebDesigner silindi, plan faylı
- Kart bloklama və **blokdan çıxarma** emailə gələn kodla (`EmailOtp.Purpose`, `OtpPurposes`, `IOtpService.SendCodeAsync/ConfirmCodeAsync`, 4 yeni endpoint, MVC `_CardCodeModal`): `09` (bölmə 3.4 yenidən yazıldı), `05` (bölmə 4), `02` (migration `AddedPurposeToEmailOtp`, EmailOtp), `15` (modal).
- **WebDesigner rolu silindi** (kod, `appsettings.Local.json`, bazadakı rol və `webdesigner@caspianbank.az` hesabı). Home məzmununu Admin və SuperAdmin idarə edir. Yenilənən fayllar: `02`, `05`, `06`, `08`, `13`, `14`, `92`.
- Yeni plan faylı: `93-planned-registry-and-bank-directory.md` (saxta dövlət reyestri, FİN ilə qeydiyyat, BIN kataloqu, başqa banka köçürmə). Hələ yazılmayıb, müəllimin cavabı gözlənilir.
- "Gözləyən" cədvəldə "Kartı OTP ilə bloklama" artıq hazırdır; yeni gözləyənlər: reyestr/qeydiyyat dəyişikliyi, BIN kataloqu, başqa banka köçürmə.

### 2026-10-14: reyestr, qeydiyyat, profil, Home rəqəmləri, kreditlər
- `94-registry-seed-draft.md`: reyestrin nümunə məlumatları. `95-registration-with-registry.md`: FİN ilə qeydiyyat, "Bu sizsiniz?", profil, hazır müştərilər, avtomatik ilk kartlar. `96-home-stats.md`: Home "Trust indicators" bazadan canlı (əvvəlki 97/98/99 uydurma rəqəmlər silindi). Bu üç sənəd əvvəlki günlərdə yazılmışdı, indi siyahıya salındı.
- **Yeni: `97-loans.md`** (kreditlər): qaydalar (10%, annuitet, ayın son günü, cərimə, Court), müraciət, işçi baxışı (reyestr, iş yerləri, xarici kreditlər, qaydalar paneli), azaldılmış təklif, aylıq/hissəvi/tam ödəniş, idempotentlik, fon xidməti, nümunə kreditlər, MVC müştəri və işçi səhifələri.
- Lazımdırsa genişlənəcək fayllar: `02` (yeni entity-lər və migration `AddedLoans`), `10` (kredit ödənişi eyni idempotentlik qaydasından istifadə edir), `11` (History-də iki yeni əməliyyat növü), `15` (müştəri kredit səhifələri), `14` (işçi kredit səhifələri). Hələ bu fayllara əlavə olunmayıb, yalnız `97`-də izah olunur.
- Gözləyən: Bills yazılanda aylıq kredit ödənişi (`lookup`, `pay-by-code` API-də hazırdır) təkrarlanan ödənişlərə əlavə olunacaq.

### 2026-10-16: köhnə hesablar silindi, gündəlik iş sınandı, bildirişlər və SignalR, köməkçi düyməsi
- 12 köhnə test istifadəçisi (kartları, əməliyyatları və OTP qeydləri ilə) bazadan silindi: 11 hesab qaldı (5 işçi, 6 hazır müştəri). Final export üçün təmiz baza.
- Kreditin gündəlik işi real bazada sınandı (vaxtı bitən təklif, cərimə, Court, təkrar yazılmama) və Court günü hesabı Bakı gününə uyğunlaşdırıldı: `97` (bölmə 7-8). Admin, SuperAdmin və Accountant-ın qərar verə bilməsi, CustomerSupport/Security-nin 403 alması yoxlandı.
- **Yeni: `98-notifications-signalr.md`** (bildirişlər, SignalR hub, qısa ömürlü hub bileti, kim kimə nə bildirir). Yenilənən fayllar: `97` (gündəlik iş bildirişləri).
- `15` (əlavə bölmə): köməkçi (chat) düyməsi və sidebar, şübhəli əməliyyatı bildir (statik), admin Staff (statik), admin menyusunun telefon düzəlişi, kreditlər cədvəlinin düzəlişi.
- SMTP qurulması üçün `appsettings.Local.json`-a boş `Smtp` bölməsi əlavə olundu (UserName, Password, FromAddress). Doldurulanda OTP kodu real email ilə gedir.
- Gözləyən: köməkçinin real cavabları, Bills və aylıq kredit ödənişi, depozit, treasury, hesab dondurma, unit testlər, Home footer-in tamamlanması.

### 2026-10-17: ödənişlər (Bills), cashback və aylıq təkrarlanan ödənişlər
- **Yeni: `100-bills.md`**: mobil operatorlar (Nar, Azercell, Bakcell, səhv operator xəbərdarlığı), kommunal (Azərişıq, Azəriqaz, Azərsu: abunəçi kodu, ad, borc, istifadəyə görə aylıq hesab), internet (20/30/50 AZN paket), Caspian kredit kodu ilə ödəniş, işarəli balans (mənfi = borc), saxta məlumatın SQL-də saxlanması (`bills.sql`), aylıq hesab xidməti, cashback (ödəyən kartın növünün faizi, Cashback kartına), idempotentlik, **aylıq təkrarlanan ödənişlər** (ayın 1-i, dayandır/davam/sil, uğursuzluqda gündə bir cəhd və bir bildiriş, bağlanmış kredit avtomatik dayanır), admin tərəfindən təchizatçının adı, aktivliyi və loqosunun idarəsi.
- Yeni əməliyyat növləri `BillPayment` və `Cashback` (History filtri və ikonları), yeni bildiriş kateqoriyası `Bill`. Migration-lar: `AddedBills`, `AddedRecurringPayments`.
- Lazımdırsa genişlənəcək fayllar: `02` (yeni entity-lər), `10` (ödəniş eyni idempotentlik qaydasını işlədir), `11` (iki yeni əməliyyat növü), `98` (`Bill` kateqoriyası), `14` və `15` (admin təchizatçı səhifələri, `bills.js`). Hələ bu fayllara əlavə olunmayıb, yalnız `100`-də izah olunur.
- Gözləyən: Serilog ilə loglama (ən sonda), real köməkçi cavabları, depozit, treasury, hesab dondurma, unit testlər, Home footer.

### 2026-10-17 (axşam): Admin Users, hesab dondurma, təkər seçimi, footer sosial linkləri
- **Admin Users real oldu** (API `api/admin/users`, MVC `/Admin/Users`): müştərilərin siyahısı (axtarış ad, email, telefon və FİN üzrə, Active/Restricted süzgəci, nömrəli səhifələmə), profil (məlumat, kartlar, kreditlər, son əməliyyatlar, köçürmə və ödəniş cəmləri). "Müştəri" = Customer rolunda olan istifadəçi, işçilər siyahıya düşmür və dondurula bilmir (404).
- **Hesab dondurma (restrict/unfreeze):** səbəb məcburidir (5-300 simvol), kimin və nə vaxt dondurduğu saxlanır (`AppUser.RestrictionReason/RestrictedAt/RestrictedBy`, migration `AddedAccountRestrictionDetails`). Donmuş müştəri daxil ola bilmir; köhnə tokeni olsa belə köçürmə, ödəniş, kredit və kart əməliyyatları rədd olunur (bu yoxlamalar əvvəldən var idi). Siyahını bütün işçilər görür, dondurub-açmağı yalnız Admin, SuperAdmin və Security.
- Təkər tipli seçim (`wheel-select.js`) və admin tarix seçicisi, ortaq nömrəli səhifələmə (`_Pager`), Cashback kartı bloklanmır, footer sosial ikonları (Settings açarları `InstagramUrl` və s.). Bu hissələr hələ ayrıca fayllarda izah olunmayıb: `15` (müştəri səhifələri), `14` (admin) genişlənəcək.

### 2026-10-17 (gec): Audit Log
- **Audit Log real oldu** (API `api/admin/audit`, MVC `/Admin/Audit`): `AuditLog` cədvəli (migration `AddedAuditLog`) yalnız əlavə olunur, redaktə və silmə yoxdur. İşçinin adı və rolu həmin anın surətidir. Yazılan hadisələr (`AuditActions`): hesab dondurma və açma, kredit təsdiqi, azaldılmış təklif, rədd, ödəniş təchizatçısı dəyişikliyi, sayt ayarı dəyişikliyi. Yazma API controller-lərində əməliyyat uğurlu olandan sonra `IAuditService.LogAsync` ilə olur və xəta əsas əməliyyatı pozmur.
- Səhifədə axtarış (işçi, hədəf, izah), əməliyyat süzgəci, tarix aralığı (Bakı günü, təkər tipli seçici) və nömrəli səhifələmə. Yalnız SuperAdmin, Admin və Security görür (menyuda da yalnız onlara görünür).
- Gotcha: MVC-də süzgəc adı `action` ola bilməz, marşrut açarı ilə toqquşur ("Index" gəlir). Ona görə formada `kind` adlanır.

### 2026-10-17 (gec): Contact və Tickets, kredit aylıq ödənişinin düzəlişi, email mətni
- **Contact və Tickets real oldu.** Müştəri `/App/Support`-da (AI söhbət paneli önizləmə kimi üstdə qalır) "Write to us" ilə mövzu və mesaj yazır, "Your messages" siyahısında cavabı oxuyur, davam edir və bağlayır. İşçi (CustomerSupport, Admin, SuperAdmin) `/Admin/Tickets`-də siyahını (Pending əvvəldə, axtarış və vəziyyət süzgəci, səhifələmə) görür, açıb cavab verir və bağlayır. Vəziyyətlər: Pending (işçi cavabı gözlənilir) → Answered → müştəri yenə yazsa Pending → Closed. Müştərinin eyni anda ən çox 5 açıq mesajı olur, bir yazışmada ən çox 100 mesaj. Bildirişlər: yeni mesaj işçilərə, cavab və bağlama müştəriyə (`Support` kateqoriyası). Cavab və bağlama audit-ə yazılır (`TicketAnswered`, `TicketClosed`). Cədvəllər `SupportTickets`, `SupportMessages` (migration `AddedSupportTickets`).
- **Aylıq ödənişin vaxtı düzəldildi:** mobil, kommunal və internet ayın 1-i, **kredit isə ayın son günü** (taksitin vaxtı) ödənilir. Əvvəl kredit də 1-i ödənilirdi, bu halda taksit 1 gün gecikmiş olur və cərimə düşürdü. Qaydalar `Domain/Bills/RecurringRules.cs`-dədir. Gecikmə cəriməsi: ilk gecikmə günündən 10 AZN sabit + gündə 0.5% (gecikmiş əsas borc və faiz üzərindən), 90 gündən sonra Court. Bax: `100-bills.md` bölmə 6, `97-loans.md`.
- OTP səhifəsində email qurulubsa demo yazısı çıxmır; qurulmayıbsa kod ekranda göstərilir və "göndərdik" deyilmir. Gmail üçün `Smtp` bölməsi `appsettings.Local.json`-da və ya user-secrets-dədir.

### 2026-10-17 (axşam): Bank dashboard real oldu, Home rəqəmləri
- **Bank dashboard** (API `api/admin/dashboard?month=2026-10`, MVC `/Admin/BankDashboard`): seçilmiş ayın real rəqəmləri. Gəlir: köçürmə komissiyası (`Commission` əməliyyatları), kart açılış haqqı (`CardFee`), kredit faizi və gecikmə cəriməsi (`LoanPaymentRecords`). Xərc: ödənilən cashback (`Cashback` əməliyyatları), aylıq əməliyyat xərci və mənfəət vergisi. Vergi = `(gəlir - cashback - əməliyyat xərci) x vergi faizi`, zərər olanda 0. Rüblük vergi seçilmiş aya qədər yığılır. Ehtiyat: məcburi ehtiyat = müştəri kartlarındakı pulun `ReservePercent`-i, bankın öz vəsaiti bundan və rüb vergisindən az olsa kart qırmızı olur. Əvvəlki aya nisbətən dəyişmə faizi. Ay seçilir (son 12 ay). Gözləyən işlər: kredit, ticket, dondurulmuş hesab. Bütün işçilər görür.
- Bankın maliyyə qaydaları `OwnBank` cədvəlindədir: `OwnFunds` (2 400 000), `OperatingCostMonthly` (150), `ProfitTaxPercent` (20), `ReservePercent` (10). Migration `AddedBankFinanceRules`. Hələlik admin səhifədən dəyişmir, bazada dəyişdirilir (bank qaydaları səhifəsi sonra).
- **Home "Trust indicators" şişirdilib:** göstərilən rəqəm = real say + başlanğıc dəyər (`appsettings.json` → `HomeStats`: müştəri +900, köçürmə +4200, həcm +1 850 000, kart +1750). Real əməliyyat rəqəmi artırır (qeydiyyat 906 → 907). Başlanğıc dəyərləri 0 etsən Home real rəqəmləri göstərər. Bank dashboard bu dəyərləri işlətmir, orada yalnız real rəqəmlər var.

### 2026-10-17 (gecə): başqa banka köçürmə, aylıq limit, Caspian BIN, hissə-hissə kredit ödənişi
- **Başqa banka köçürmə** (`ExternalBanks`, `ExternalCards`, `ExternalTransfers`; migration `AddedExternalCardsAndTransfers`). Kart nömrəsinin ilk 8 rəqəmi (BIN) bankı müəyyən edir: `BankDirectoryService` əvvəl xarici bank siyahısına, sonra Caspian BIN-inə baxır. Formada nömrə yazılanda üstdə bankın loqosu və adı çıxır (MVC `GET /App/Transfer/Bank?bin=`, API `GET api/banks/lookup?bin=`). 6 real Azərbaycan bankı (Kapital, ABB, Rabitəbank, PASHA, Respublika, Xalq) və 40 saxta kart `Repository/Data/Seed/external-banks-and-cards.sql`-dədir; xarici kartın balansı yoxdur, pul yalnız göndərəndən çıxır. Alıcının adı maskalanır ("Elnur M."). Ayrıca banklararası haqq yoxdur (`InterbankFeePercent = 0`), yalnız limit komissiyası var.
- **Aylıq komissiyasız limit** (`Domain/Transfers/TransferFees.cs`): hər göndərən kartın növünün aylıq limiti var (Standard 500, Silver 2000, Gold 10000, Cashback 0). Komissiya yalnız bu ay artıq göndərilənlə birlikdə limiti aşan hissəyə tutulur: `ChargeableAmount = max(0, used + amount - limit) - max(0, used - limit)`. Nümunə: ilk 600 AZN-də komissiya 100-ün 1%-i = 1.00; limit bitdikdən sonra hər köçürmənin bütün məbləğinə tutulur. Qayda öz kartlar arası, başqa Caspian müştərisinə və başqa banka köçürmədə eynidir (öz kartlar arası köçürmə də limitə sayılır). Ay Bakı vaxtı ilə hesablanır. `GET api/transfers/limits` kartın limitini və bu ay istifadəni qaytarır, formadakı canlı komissiya göstəricisi bundan istifadə edir.
- **Cashback kartından istənilən karta köçürmək olar** (öz karta, başqa müştəriyə, başqa banka), komissiyasız (limit 0, faiz 0). Cashback kartına isə köçürmə olmur (o yalnız cashback yığır). Cashback kartından kredit də ödənir.
- **Kredit aylıq ödənişini hissə-hissə ödəmək:** `PayLoanDto.Amount` aylıq ödənişdə də işləyir. Boş (0) olsa bütün borc ödənir; borcdan azdırsa yalnız o qədər ödənir (`LoanEngine.ApplyInstallmentPayment`): ən köhnə ay əvvəl, hər ayda cərimə, sonra faiz, sonra əsas borc. Borcdan çox yazsan rədd olunur. Qalanını başqa kartdan ayrıca ödəmək olur (məs. 4 AZN Cashback kartından, qalanı Standard kartdan). Hər ödəniş ayrı `RequestId` və ayrı qəbz alır. Qismən ödənişdə qalan hissəyə cərimə hesablanması davam edir.
- **Caspian BIN = `96301020`.** `CardNumberGenerator.Prefix` bununla başlayır (8 rəqəm BIN + 7 təsadüfi rəqəm + Luhn yoxlama rəqəmi = 16). `OwnBanks.Bin` də eynidir. Mövcud kartlar `Repository/Data/Seed/caspian-bin-renumber.sql` ilə yeni nömrələrə keçirilib (kredit ödənişi tarixçəsindəki kart nömrələri də).

### 2026-10-17 (gecə): müddətli depozitlər və xəzinə
- **Yeni: `101-deposits.md`.** Müddətli depozit (3/6/12/24 ay, sadə faiz, müddət bitməmiş çıxarmaq yoxdur, pul və faiz müddət bitəndə karta qayıdır), `DepositPlan` və `Deposit` cədvəlləri (migration `AddedDeposits`), `DepositMaintenanceService`, müştəri səhifəsi `/App/Deposits`, admin səhifəsi `/Admin/Deposits` (variantların redaktəsi audit-ə yazılır).
- **Dashboard (xəzinə):** müştəri pulu = kart balansları + depozitlər, məcburi ehtiyat ikisinin üzərindən hesablanır, ödənilən depozit faizi xərcdir və mənfəət vergisinin bazasını azaldır.
- Yeni əməliyyat növləri `DepositOpen`, `DepositPayout`, `DepositInterest`; yeni bildiriş kateqoriyası `Deposit`; yeni audit hadisəsi `DepositPlanChanged`.

### 2026-10-17 (gecə 2): bankın xəzinəsi (Treasury) və kredit yoxlaması
- **`TreasuryEntries` jurnalı** (migration `AddedTreasury`): komissiya, kart haqqı, kredit ödənişləri (əsas borc, faiz, cərimə), depozitlər, verilən kreditlər və cashback `AppDbContext.SaveChanges` ilə əməliyyatla eyni saxlamada xəzinəyə yazılır. Balans = sətirlərin cəmi. Detallar: `101-deposits.md` bölmə 8.
- **Kredit yalnız xəzinə imkan verəndə verilir:** `xəzinə - kredit` məcburi ehtiyatdan az olmamalıdır (`LoanOutcome.TreasuryLow`).
- Admin səhifəsi `/Admin/Treasury` (balans, ehtiyat, kredit üçün qalan, aylıq giriş-çıxış, süzgəcli jurnal). Dashboard-da "own funds" = xəzinə balansı.

### 2026-10-17 (gecə 2): bank kart nömrəsinin ilk 6 rəqəmindən tanınır
- Bank axtarışı (`api/banks/lookup`, `BankDirectoryService`, çip formada) artıq ilk **6 rəqəmlə** işləyir (real kartlarda IIN 6 rəqəmdir): `55220993...` da ABB-dir, çünki ABB-nin BIN-i `552209`-la başlayır. Əvvəl 8 rəqəmin hamısı uyğun gəlməli idi. Caspian üçün də eyni (`963010...`). Konkret kart nömrəsi isə yenə xarici kartlar cədvəlində tam uyğun gəlməlidir, yoxsa "bu bankda belə kart tapılmadı" deyilir.

### 2026-10-17 (gecə 3): işçilərin idarəsi
- **Yeni: `102-staff-management.md`.** SuperAdmin `/Admin/Staff` səhifəsində işçi əlavə edir (şifrəni işçi emaildəki linklə seçir), rolunu dəyişir, hesabı deaktiv və ya aktiv edir. Hər əməliyyat audit-ə yazılır (`StaffAdded`, `StaffRoleChanged`, `StaffDeactivated`, `StaffActivated`).
- **Təhlükəsizlik:** işçi tokenləri hər sorğuda yoxlanır: deaktiv edilən və ya rolu dəyişən işçinin açıq tokeni dərhal 401 olur.

### 2026-10-17 (gecə 4): şübhəli fəaliyyət (Security)
- **Yeni: `103-security-alerts.md`.** Sistem 5 qayda ilə siqnal yaradır (böyük köçürmə, qısa vaxtda çox köçürmə, gündəlik cəm, yeni hesab, bloklanmış hesab). Security `/Admin/Security` səhifəsində "Mark safe" və ya "Freeze" deyir. Heç nə avtomatik bloklanmır. Qərarlar audit-ə yazılır, yeni siqnal Security, Admin və SuperAdmin-ə bildiriş göndərir. Migration `AddedSecurityAlerts`.

### 2026-10-21: depozit faizi ay-ay, ayın bağlanması, bank borcu
- **Depozit faizi hər ay yazılır** (`DepositInterestPostings`, migration `AddedDepositInterestPostings`): müştəri aylıq yazılışları görür, pul müddət bitəndə ödənilir. Bankın faiz xərci həmin ayın xərcidir (dashboard).
- **Ayın bağlanması:** əməliyyat xərci və mənfəət vergisi hər ay xəzinədən çıxış kimi yazılır (`TreasuryMaintenanceService`, əl ilə `close-month`). Yeni xəzinə növləri `OperatingCost`, `ProfitTax`; audit `TreasuryMonthClosed`.
- **Bank borcu:** xəzinə mənfi olsa admin səhifədə xəbərdarlıq, kredit verilmir, depozit ödənişləri davam edir. Detallar: `101-deposits.md` bölmə 9.

### 2026-10-21 (gecə): bildiriş (Report), təkrarlanan köçürmə, xərc analitikası, bank qaydaları, tema və dillər
- **Yeni: `104-reports-recurring-analytics-rules.md`.** Müştəri tarixçədən şübhəli əməliyyatı bildirir, Security/Admin baxır (baxıldı, tədbir: alan hesabı dondur və ya kartı blokla, əsassız). Aylıq təkrarlanan köçürmə (`/App/Recurring`, gün 1–28, hər ay təkrar göndərilməsin deyə deterministik `RequestId`). Xərc analitikası (`/App/Spending`, kateqoriyalar, aylıq müqayisə). Bank qaydaları səhifəsi (`/Admin/BankRules`). Kiçik: kreditin kodla hissə-hissə ödənişi, dəstək üçün FİN/telefon/kart axtarışı.
- **Yeni: `105-theme-and-languages.md`.** Ağ/qara tema (`theme-dark.css`, `cb-theme`) və üç dil (en, az, ru). Mətnlər `Translations` cədvəlindədir (açar = ingilis mətn), `@T["..."]` və JS-də `cbt("...")`, admin `/Admin/Translations` səhifəsi, ≈810 mətnlik seed `translations.sql`. Admin paneli ingilis qalır.
- Migration: `AddedReportsRecurringTransfersTranslations`, `AddedDepositInterestPostings`.

### 2026-10-21 (gecə 2): Contact us xəritəsi adminin əlindədir
- Xəritənin mərkəzi artıq koda yazılmayıb: `Settings` cədvəlində `MapLatitude` və `MapLongitude` (nöqtə ilə onluq kəsr, validator aralığı yoxlayır: enlik −90..90, uzunluq −180..180). Admin `Settings` səhifəsində dəyişir; `SiteSettingsVM.MapSrc` düzgün olmayan dəyərdə Nizami küçəsinə qayıdır. Seed: `settings-map.sql`. `Address` ayrı sahədir, admin ikisini birlikdə yeniləməlidir.

### 2026-10-24: Top-up yalnız həqiqi kartdan
- `CardService.TopUpAsync`: daxil edilən kart əvvəl Caspian kartları arasında axtarılır. Tapılsa həmin kartdan pul çıxır, hədəf karta düşür (`CardRepository.TopUpFromCardAsync`, bir SaveChanges, RowVersion). Bitmə tarixi kartdakı ilə eyni olmalıdır, kart bloklu ola bilməz, balans çatmasa "That card doesn't have enough balance for this payment." deyilir. Sahibinə "Money left your card" bildirişi gedir.
- Caspian kartı deyilsə `ExternalCards` cədvəlində axtarılır: tapılsa pul çıxmır (balansı bizdə yoxdur), tapılmasa "We couldn't find a card with that number.".
- Yeni `TransactionType.TopUpOut` (enumun sonunda): göndərən kartdakı çıxış qeydi. `TransferOut` istifadə olunmur ki, aylıq pulsuz limit, təhlükəsizlik siqnalları və ana səhifə statistikası pozulmasın.
- Endpoint `POST api/cards/{id}/top-up` artıq `transfer` rate limit altındadır (kart nömrəsi təxmin edilməsin).
- Hazır kartlar (`Repository/Data/Seed/topup-cards.sql`, `sqlcmd -I` ilə): demo xarici kart `4111 1111 1111 1111` və Rauf Quliyevin Gold kartı `9630 1020 7777 7772` (10/30, 5 000 000 AZN). Mətnlər: `translations-topup.sql`.

### 2026-10-24 (2): Qara siyahıdakı müştərinin krediti iki təsdiq tələb edir
- Əvvəl qara siyahıdakı müştərinin müraciəti dərhal rədd olunurdu. İndi qəbul olunur (`Loan.RequiresSuperAdminApproval = true`) və işçilərə "New loan application (blacklisted customer)" bildirişi gedir.
- Axın: `Pending` → kredit şöbəsi (Accountant/Admin) təsdiq edir (`LoanRepository.FirstApproveAsync`) → yeni status `AwaitingSuperAdmin`, Super Admin-ə "Do you approve it too?" bildirişi → Super Admin təsdiq edir (`ApproveAsync`: pul gedir və ya azaldılmışsa təklif olur) və ya rədd edir. İki təsdiq olmadan pul getmir.
- Qaydalar (`LoanReviewService`): Super Admin tək başına birinci təsdiqi verə bilməz; kredit şöbəsi ikinci təsdiqi və Super Admin-ə keçəndən sonra rədd qərarını verə bilməz; son təsdiqdə məbləğ kredit şöbəsinin təsdiq etdiyi məbləğdir. Müraciətdən sonra qara siyahıya düşən də eyni qaydaya düşür.
- Müştəri `AwaitingSuperAdmin` statusunu görmür: `LoanMapper` onu "Pending" kimi qaytarır (qara siyahı barədə heç nə bilmir). İşçi tərəfində `forStaff: true`.
- Migration: `AddedBlacklistTwoStepLoanApproval` (Loans: RequiresSuperAdminApproval, FirstApprovedByUserId, FirstApprovedAt, FirstApprovedAmount).
- MVC admin: siyahıda qırmızı "Blacklisted" nişanı və "Awaiting Super Admin" statusu, detalda addımları göstərən xəbərdarlıq lövhəsi, Super Admin üçün "Final approval" forması. Testlər: `BlacklistedLoanApprovalTests`.
- Kredit səhifəsi: aylıq ödəniş qutusunun qaranlıq rejimi düzəldi, təyinat/status/rədd mətnləri tərcümə olunur (`translations-loans.sql`).

### 2026-10-24 (3): Bildirişlərin tərcüməsi, Serilog, sənədlərin repoya köçürülməsi
- **Sənədlər repoya köçürüldü:** `docs/learning/` (API repo-sunun kökündə). Klon edəndə hamısı görünür. Köhnə `Desktop\docs` qovluğu artıq yenilənmir.
- **Bildirişlər və dəyişənli mətnlər tərcümə olunur:** `Translator` bazadakı `{0}` şablonlarını gələn mətnə uyğunlaşdırır, bildiriş mətnlərinin az/ru şablonları `translations-notifications.sql`-dədir, canlı bildiriş `POST /App/Notifications/Translate` ilə çevrilir. Bax `105-theme-and-languages.md`.
- **Serilog:** API və MVC öz `logs/` qovluğuna yazır (gündə bir fayl, 14 gün). Yeni sənəd `107-logging-serilog.md`. `TransferService`, `CardService` (top-up), `LoanReviewService` jurnala yazır.
- **Yenilənən sənədlər:** `09-cards.md` (top-up yalnız həqiqi kartdan), `10-transfers.md` (aylıq pulsuz limit, başqa banka köçürmə), `97-loans.md` (qara siyahıda iki təsdiq), `98-notifications-signalr.md`, `100-bills.md`, `105-theme-and-languages.md`, `106-tests.md`.
