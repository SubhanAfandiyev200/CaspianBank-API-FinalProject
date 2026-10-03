CASPIAN BANK: LAYİHƏ VƏZİYYƏTİ VƏ PLAN
=======================================
Tarix: 2026-10-04
Layihələr:
  API : C:\Users\User\Desktop\CaspianBank-API-FinalProject      (Domain / Repository / Service / API)
  MVC : C:\Users\User\Desktop\CaspianBank-MVC-FinalProject      (UI, API-ni HttpClient ilə çağırır)
  Front (dizayn mənbəyi): C:\Users\User\Desktop\CaspianBank-Frontend


1. HAZIRDIR
-----------
[x] Solution quruluşu: API (N-Tier) + ayrı MVC, EF Core, Identity, JWT, MailKit, FluentValidation
[x] AUTHENTICATION (tam)
      - Welcome səhifəsi: email yazılır -> mövcuddursa Login, yenidirsə OTP
      - Email OTP: kod bazada HMAC hash, 5 dəq, 5 cəhd limiti, 60 san yenidən göndərmə gözləməsi
      - Register: 18+ yoxlaması, OTP təsdiqi olmadan qeydiyyat mümkün deyil, Customer rolu
      - Login (JWT), lockout (5 uğursuz cəhd -> 5 dəq blok), cookie auth, Logout
      - Forgot / Reset password (Identity token, 30 dəq, bir dəfəlik)
      - Rate limiting (auth endpoint-lərində IP başına dəqiqədə 10 sorğu)
[x] Rollar (seed): Customer, SuperAdmin, Admin, Accountant, WebDesigner, CustomerSupport, Security
[x] Home səhifəsinin dinamik hissələri (API-dən): Ticker, About (başlıq + 3 kart + video), Brendlər
[x] MVC səhifələri: Home, Welcome, Login, Register, VerifyOtp, ForgotPassword, ResetPassword
[x] Təqdimata hazırlıq:
      - SQL Server avtomatik tapılır (.\SQLEXPRESS, LocalDB, localhost...), migration avtomatik tətbiq olunur
      - Jwt:Key hazırdır (yerli işləmə üçün), secrets tələb olunmur
      - Demo rejimi: SMTP qurulmayıbsa OTP kodu və reset linki səhifədə göstərilir
      - README.md (hər iki repoda), .gitignore


2. HAZIR DEYİL (plan üzrə ardıcıllıqla)
---------------------------------------
[ ] 1)  Global Exception Handling middleware (planda ən əvvəl idi)
[ ] 2)  Qalan entity-lər: Card, CardTierConfig, Transaction, LoanApplication, Report,
        AuditLog, BillPayment, Notification, ContactMessage
[ ] 4)  CARD SİSTEMİ (F2): ilk kart bundle (Main + Savings, pulsuz), əlavə kart (Standard/Silver/Gold),
        CardTierConfig seed, kart bloklama
[ ] 5)  TRANSFER (F3, ən kritik): EF Core tranzaksiya, tier limiti, komissiya, cashback
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


3. 8 MƏCBURİ FEATURE
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


4. BİLƏRƏKDƏN SONRAYA SAXLANILIB
-------------------------------
- AUTHORIZATION: rola görə yönləndirmə, [Authorize] ilə qorunan səhifələr, seed admin hesabları,
  rol idarəsi (SuperAdmin / Admin fərqi). Səhifələr yazılandan sonra.
- Dinamik sayt loqosu (Settings), AuthBrand / HeaderAuth ViewComponent-ləri
- Home-un qalan hissələri (hero, services, stats) hələ sərt yazılıb
- Admin üçün ticker / brend / About CRUD və şəkil yükləmə
- Report olunan köçürmənin geri qaytarılması (reversal) qərarları
- Welcome/login-dən sonra rola görə yönləndirmə (TODO işarələnib)


5. KİÇİK QALANLAR
-----------------
[ ] Home videosunu sıxmaq (homeVideo.mp4 ~16 MB, sayt donur): HandBrake, 720p, RF 26-28, səssiz, Web Optimized
[ ] Real Gmail emailinin gəldiyini təsdiqləmək (sertifikat düzəlişindən sonra yoxlanmayıb)
[ ] Təqdimat üçün bazanı hazırlamaq: test istifadəçilərini (jwt.smoke*, otp.test*, fresh.*) və
    EmailOtps sətirlərini silmək, Home məlumatları (ticker, About, brendlər) qalsın, sonra .bak / .bacpac export
[ ] Şəkil və videonu təqdimata daxil etmək (wwwroot/images, wwwroot/videos; video Git-də izlənilmir)
[ ] bin/obj Git-də izlənilir: git rm -r --cached .vs bin obj (hər repoda)
[ ] Başqa kompüterdə real import ilə bir dəfə sınamaq
[ ] Şifrə dəyişəndən sonra köhnə JWT müddəti bitənə qədər işləyir (məlum məhdudiyyət, təqdimatda qeyd etmək olar)


6. TƏKLİF OLUNAN ARDICILLIQ
---------------------------
1. Exception middleware (kiçik, hər şeyin altında dayanır)
2. Card sistemi + _AppLayout + app / add-card / card-detail
3. Transfer
4. Tarixçə + Statement
5. Kredit
6. Admin panel + authorization
7. Bildirişlər + SignalR
8. Request logging
9. Extra-lar (Contact, Report, Bill Payment) vaxt qalarsa
10. Testlər, video sıxmaq, bazanı təmizləyib export etmək

Vaxt qısa olsa, ən təhlükəsiz kəsiklər:
  - Bill Payment / Hangfire
  - Report sistemi
  - Admin-də Home məzmun CRUD-u
  - Reversal


7. İŞƏ SALMA (QISA)
-------------------
1. API-ni https profili ilə işə sal:  https://localhost:7293  (Swagger)
2. MVC-ni https profili ilə işə sal:  https://localhost:7082
3. Email qurulmayıbsa demo rejimi: OTP kodu və reset linki səhifədə görünür.
4. Real email üçün (API qovluğunda, parolu heç yerə yazma):
     dotnet user-secrets set "Smtp:UserName" "sizin.gmail@gmail.com"
     dotnet user-secrets set "Smtp:Password" "16-herfli-app-password"
     dotnet user-secrets set "Smtp:FromAddress" "sizin.gmail@gmail.com"
