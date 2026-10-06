CASPIAN BANK: LAYİHƏ VƏZİYYƏTİ VƏ PLAN
=======================================
Yenilənib: 2026-10-06
Layihələr:
  API : C:\Users\User\Desktop\CaspianBank-API-FinalProject      (Domain / Repository / Service / API)
  MVC : C:\Users\User\Desktop\CaspianBank-MVC-FinalProject      (UI, API-ni HttpClient ilə çağırır)
  Front (dizayn mənbəyi): C:\Users\User\Desktop\CaspianBank-Frontend


1. HAZIRDIR
-----------
[x] Solution quruluşu: API (N-Tier) + ayrı MVC, EF Core, Identity, JWT, MailKit, FluentValidation

[x] AUTHENTICATION (tam)
      - Welcome (email) -> mövcuddursa Login, yenidirsə OTP
      - Email OTP: kod bazada HMAC hash, 5 dəq, 5 cəhd limiti, 60 san yenidən göndərmə gözləməsi
      - Register: 18+ yoxlaması, OTP təsdiqi olmadan qeydiyyat yoxdur, Customer rolu
      - Login (JWT), lockout (5 uğursuz cəhd -> 5 dəq), cookie auth, Logout
      - Forgot / Reset password (Identity token, 30 dəq, bir dəfəlik)
      - Rate limiting (auth endpoint-lərində IP başına dəqiqədə 10 sorğu)
[x] Rollar (seed): Customer, SuperAdmin, Admin, Accountant, WebDesigner, CustomerSupport, Security

[x] HOME SƏHİFƏSİ (API-dən gələn dinamik hissələr, hər biri ViewComponent)
      - Ticker, About (başlıq + 3 kart + video), Brendlər
      - Services və Benefits bölmələri (ServiceSection/ServiceItem, BenefitSection/BenefitItem)
      - KART DİZAYNLARI (hero-dakı kart yelpazəsi): CardDesign (Title, Image, ShowOnHome, DisplayOrder)
          * GET  /api/home/cards  (göstərilənlər, sıra ilə, maksimum 3)
          * Admin CRUD: /api/admin/card-designs (GET, POST, PUT, DELETE), şəkil yükləmə (PNG/JPEG/WebP, max 2 MB,
            tip faylın içindəki imzaya görə yoxlanır), rol: WebDesigner / Admin / SuperAdmin
          * MVC: HomeCardsViewComponent; API işləməsə köhnə statik 3 CSS kart ehtiyat kimi göstərilir
          * Başlanğıc 3 dizayn (regular/silver/gold) seed olunub, şəkillər wwwroot/images/cards/-dadır

[x] MVC səhifələri: Home, Welcome, Login, Register, VerifyOtp, ForgotPassword, ResetPassword

[x] BaseRepository ümumi metodları: GetAllAsync, GetAsync (tək sətir), GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync
    (Add/Update/Delete əməliyyatı bazaya dərhal yazır)

[x] Təqdimata hazırlıq
      - SQL Server avtomatik tapılır (.\SQLEXPRESS, LocalDB, localhost...), migration avtomatik tətbiq olunur
      - Jwt:Key hazırdır (yerli işləmə üçün), secrets tələb olunmur
      - Demo rejimi: SMTP qurulmayıbsa OTP kodu və reset linki səhifədə göstərilir
      - README.md (hər iki repoda), .gitignore

Migration-lar (bazaya tətbiq olunub): AddedUserTables, CreatedHomeTickerTable, AddedCreatedAtDefaultConfiguration,
AddedBrandTable, AddedAboutsTable, AddedVideoPathToAboutModel, AddedEmailOtpTable,
AddedServiceSectionsAndServiceitemsTables, AddedBenefitItemAndBenefitSectionTables, AddedCardDesignTable


2. İŞLƏNİR / QƏRAR GÖZLƏYİR
---------------------------
[ ] HERO modeli (Home-un yuxarısı, sol mətn): Eyebrow, Title, Description, ButtonText, ButtonUrl, tək sətir
      - Qərar: CardDesign ilə FK YOXDUR (iki ayrı model). Hansı kartların göründüyünü ShowOnHome + DisplayOrder idarə edir.
      - Plan: GET /api/home/hero, MVC-də HeroViewComponent (içində HomeCards ViewComponent-i çağırılır)
      - Sən özün yazırsan.
[ ] CardDesign seed qərarı: HasData (3 sətir) saxlansın, yoxsa çıxarılsın (çıxarsan yeni migration lazımdır)
[ ] Real Gmail emailinin gəldiyini təsdiqləmək (sertifikat düzəlişindən sonra yoxlanmayıb)


3. HAZIR DEYİL (plan üzrə ardıcıllıqla)
---------------------------------------
[ ] 1)  Global Exception Handling middleware (planda ən əvvəl idi)
[ ] 2)  Qalan entity-lər: Card, CardTierConfig, Transaction, LoanApplication, Report,
        AuditLog, BillPayment, Notification, ContactMessage
[ ] 4)  CARD SİSTEMİ (F2): ilk kart bundle (Main + Savings, pulsuz), əlavə kart (Standard/Silver/Gold),
        CardTierConfig seed, kart bloklama
          * Dizayn əlaqəsi: CardTierConfig.CardDesignId (tier-in standart dizaynı) və Card.CardDesignId (kart açılanda
            kopyalanır, admin sonra dəyişsə mövcud kartlar dəyişmir). İstifadədə olan dizayn silinə bilməz
            (DeleteBehavior.Restrict + CardDesignService.DeleteAsync-də aydın xəta).
          * Kartın üzərinə fon kimi dizayn şəkli, üstünə real son 4 rəqəm, balans, ad yazılır.
[ ] 5)  TRANSFER (F3, ən kritik): EF Core tranzaksiya, tier limiti, komissiya, cashback
          * Burada bir neçə əməliyyat atomic olmalıdır: BaseRepository-nin "özü saxlayır" metodları kifayət etmir,
            ayrıca tranzaksiya (BeginTransactionAsync / ITransferRepository) lazım olacaq.
[ ] 6)  Tarixçə (F4) + Statement (F5), filtrlər
[ ] 7)  Kredit (F8): müraciət + admin approve/decline
[ ] 8)  ADMIN PANEL (F6): overview, users (freeze), kartlar, kreditlər, audit log, tier config
[ ] 9)  Bildirişlər + SignalR (F7: aşağı balans bildirişi)
[ ] 10) Request logging middleware
[ ] 11) Extra-lar: Report, Contact Us, Bill Payment (+ Hangfire)
[ ] 12) Unit testlər (auth, transfer, tier/komissiya, loan, kontur)

MVC-də hələ köçürülməyən səhifələr (dizaynı CaspianBank-Frontend-də hazırdır):
[ ] _AppLayout
[ ] app (kartlar), add-card, card-detail
[ ] transfer
[ ] transaction-history, report-transaction
[ ] loan-application
[ ] bill-payment
[ ] notifications
[ ] contact-us
[ ] Bütün admin səhifələri (frontend repoya sonra qoyulacaq)


4. 8 MƏCBURİ FEATURE
--------------------
F1  Təhlükəsiz auth + session ............ HAZIRDIR
F2  Çoxlu hesab növü (Main/Savings) ...... yoxdur
F3  Hesablar arası köçürmə (atomic) ...... yoxdur
F4  Tam əməliyyat tarixçəsi .............. yoxdur
F5  Tarix aralığı üçün statement ......... yoxdur
F6  Admin panel + freeze ................. yoxdur
F7  Aşağı balans bildirişi ............... yoxdur
F8  Kredit müraciəti + approve/decline ... yoxdur
=> 1 / 8 bitib. Qalanı Card sistemindən asılıdır.


5. BİLƏRƏKDƏN SONRAYA SAXLANILIB
-------------------------------
- AUTHORIZATION: rola görə yönləndirmə, [Authorize] ilə qorunan MVC səhifələri, seed admin hesabları,
  rol idarəsi (SuperAdmin / Admin fərqi). Səhifələr yazılandan sonra.
  (API-də admin endpoint-lərində [Authorize(Roles = ...)] artıq var; bazada rollu istifadəçi yoxdur)
- Dinamik sayt loqosu (Settings), AuthBrand / HeaderAuth ViewComponent-ləri
- Home-un qalan statik hissələri (stats və s.)
- Admin üçün ticker / brend / About CRUD və şəkil yükləmə (kart dizaynı üçün artıq var)
- Report olunan köçürmənin geri qaytarılması (reversal) qərarları
- Login-dən sonra rola görə yönləndirmə (TODO işarələnib)


6. KİÇİK QALANLAR
-----------------
[ ] Home videosunu sıxmaq (homeVideo.mp4 ~16 MB, sayt donur): HandBrake, 720p, RF 26-28, səssiz, Web Optimized
[ ] Təqdimat üçün bazanı hazırlamaq: test istifadəçilərini (jwt.smoke*, otp.test*, fresh.*) və
    EmailOtps sətirlərini silmək, Home məlumatları qalsın, sonra .bak / .bacpac export
[ ] Şəkil və videonu təqdimata daxil etmək (wwwroot/images, wwwroot/images/cards, wwwroot/videos;
    video Git-də izlənilmir)
[ ] Yeni fayllar Git-ə commit olunmalıdır (hər iki repoda; wwwroot/images/cards/ daxil)
[ ] bin/obj Git-də izlənilir: git rm -r --cached .vs bin obj (hər repoda)
[ ] Müvəqqəti C:\cbtmp qovluğunu (layihənin sınaq nüsxəsi) əl ilə silmək
[ ] Başqa kompüterdə real import ilə bir dəfə sınamaq
[ ] Şifrə dəyişəndən sonra köhnə JWT müddəti bitənə qədər işləyir (məlum məhdudiyyət)
[ ] Brend və About ViewComponent-lərindəki şəkil ünvanı kodunu Helpers/ApiUrl.cs-ə birləşdirmək (istəyə bağlı)


7. TƏKLİF OLUNAN ARDICILLIQ
---------------------------
1. Hero modeli (sən yazırsan), CardDesign seed qərarı
2. Exception middleware (kiçik, hər şeyin altında dayanır)
3. Card sistemi + _AppLayout + app / add-card / card-detail
4. Transfer
5. Tarixçə + Statement
6. Kredit
7. Admin panel + authorization
8. Bildirişlər + SignalR
9. Request logging
10. Extra-lar (Contact, Report, Bill Payment) vaxt qalarsa
11. Testlər, video sıxmaq, bazanı təmizləyib export etmək

Vaxt qısa olsa, ən təhlükəsiz kəsiklər:
  - Bill Payment / Hangfire
  - Report sistemi
  - Admin-də Home məzmun CRUD-u (kart dizaynı istisna)
  - Reversal


8. İŞƏ SALMA (QISA)
-------------------
1. API-ni https profili ilə işə sal:  https://localhost:7293  (Swagger)
2. MVC-ni https profili ilə işə sal:  https://localhost:7082
3. Email qurulmayıbsa demo rejimi: OTP kodu və reset linki səhifədə görünür.
4. Real email üçün (API qovluğunda, parolu heç yerə yazma):
     dotnet user-secrets set "Smtp:UserName" "sizin.gmail@gmail.com"
     dotnet user-secrets set "Smtp:Password" "16-herfli-app-password"
     dotnet user-secrets set "Smtp:FromAddress" "sizin.gmail@gmail.com"
   (secrets yalnız həmin kompüterdə olur; Jwt:Key də user-secrets ilə üstələnə bilər)
