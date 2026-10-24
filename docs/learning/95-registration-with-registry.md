# 95. Qeydiyyat dövlət reyestri ilə (FİN + vəsiqə), avtomatik kartlar, sınaq müştəriləri

Fayllar (API): `Service/Services/AccountService.cs`, `IAccountService.cs`, `Service/Helpers/DTOs/Accounts/RegisterDto.cs`, `CheckIdentityDto.cs`, `Validators/Accounts/RegisterDtoValidator.cs`, `CheckIdentityDtoValidator.cs`, `Responses/CheckIdentityResponse.cs`, `Repository/Repositories/CustomerRepository.cs`, `AccountRepository.cs`, `Service/Services/CardService.cs`, `CaspianBank-API-FinalProject/Helpers/CustomerAccountSeeder.cs`, `Controllers/Client/AccountController.cs`, `CardsController.cs`.
Fayllar (MVC): `Controllers/AccountController.cs`, `Views/Account/Register.cshtml`, `ConfirmIdentity.cshtml`, `CreateAccount.cshtml`, `ViewModels/Account/IdentityVM.cs`, `IdentityResponseVM.cs`, `ConfirmIdentityVM.cs`, `RegisterVM.cs`, `wwwroot/css/style.css` (`.citizen-card`, `.citizen-photo`).

## 1. Nəzəriyyə: niyə qeydiyyat reyestrə bağlanır
- Real bank müştərini **tanımalıdır** (KYC, "müştərini tanı"). İstifadəçi ad, soyad və doğum tarixini özü yazsa, istənilən adı yaza bilər. Bizdə bu məlumat **dövlət reyestrindən** (saxta `Customer` cədvəli) gəlir, istifadəçi yalnız FİN + vəsiqə nömrəsi yazır.
- İki məlumat (FİN və vəsiqə nömrəsi) **eyni vətəndaşa** aid olmalıdır. Biri səhv olsa cavab eynidir ("ikisini də yoxlayın"): hansının səhv olduğunu deməmək təxmin etməni çətinləşdirir.
- Bir vətəndaş yalnız **bir** hesab aça bilər (`AppUser.CustomerId` unikal indeksdir).
- Vəfat etmiş vətəndaşa hesab açılmır. Qara siyahıdakılar qeydiyyatdan keçə bilər, yalnız kredit alamazlar (sonra).
- Reyestrdə email və telefon **yoxdur**: qeydiyyatdan keçən adam öz emailini (OTP ilə təsdiqlənir) və telefonunu yazır.

## 2. Axın (MVC)
1. **Welcome**: email yazılır. Email yeni olarsa OTP kodu göndərilir.
2. **VerifyOtp**: kod yazılır, təsdiq tokeni alınır.
3. **Register** (1-ci addım): FİN + vəsiqə nömrəsi. MVC `POST api/account/check-identity` çağırır.
4. **ConfirmIdentity** (2-ci addım, "Bu sizsiniz?"): şəkil (CSS ilə ağ-qara), ad, soyadın baş hərfi, doğum ili. "Yes, this is me" → `ConfirmIdentityYes`.
5. **CreateAccount** (3-cü addım): telefon + şifrə. MVC `POST api/account/register` çağırır. API hesabı açır və **ilk iki kartı avtomatik yaradır** (Standard + Cashback).
6. **Login**: "Your account is ready, with your first two cards."

Addımlar arası məlumat `TempData`-dadır (`RegFin`, `RegIdCard`, `RegName`, `RegSurname`, `RegYear`, `RegPhoto`, `RegConfirmed`). `Peek` ilə oxunur ki, səhifə yenilənəndə silinməsin. 3-cü addımda `RegConfirmed` yoxdursa 1-ciyə qaytarılır: kimliyi təsdiqləməmiş hesab açılmır. Şifrə heç vaxt TempData-ya yazılmır.

## 3. API
**`POST api/account/check-identity`** (`auth` rate limit-i, FİN təxmin etməyə qarşı): body `{ fin, idCardNumber }`.
Qaydalar (`AccountService.FindCitizenAsync`): 1) reyestrdə FİN + vəsiqə uyğundur? yoxdursa "We couldn't find a citizen...". 2) `DeathDate` boş deyil → "An account can't be opened with this ID record." 3) bu vətəndaşın hesabı var → "An account already exists... Log in instead."
Uğurlu cavab: `{ name, maskedSurname ("H."), birthYear, photoUrl }`: yalnız bu qədər, tam məlumat verilmir.

**`POST api/account/register`**: body `{ email, phoneNumber, fin, idCardNumber, password, verificationToken }` (ad, soyad, doğum tarixi artıq yoxdur). Sıra: validator → OTP tokeni → vətəndaş yoxlaması → telefon başqa hesabda varmı (`PhoneInUseAsync`, 0-lu və 0-suz eyni sayılır, bazaya həmişə 0-lu yazılır) → `AppUser` yaradılır (`Name`, `Surname`, `BirthDay`, `FinKod`, `CustomerId` reyestrdən) → rol `Customer` → OTP tokeni istifadə olundu → `CardService.CreateInitialCardsAsync(userId)`.
Kartlar alınmasa hesab yenə hazırdır; müştəri Cards səhifəsindəki "Create my cards" düyməsi ilə yenidən yarada bilər (`POST api/cards/initial`, artıq FİN soruşmur).

**Yaş qaydası:** əvvəl qeydiyyatda 18 yaş tələb olunurdu. İndi doğum tarixi reyestrdəndir və hesab açmaq üçün yaş yoxlanmır (16 yaşlı Murad hesab aça bilər). 18 yaş qaydası **kredit** üçün olacaq (`OwnBank.MinAge`).

## 4. Sınaq müştəriləri (`SeedCustomers`)
`appsettings.Local.json` (git-ə düşmür) → `SeedCustomers`: 6 reyestr vətəndaşı üçün email, telefon, parol, Standard kartın başlanğıc balansı. `CustomerAccountSeeder` (API işə düşəndə, `Program.cs`) hər sətir üçün:
- email-də `SENINGMAIL` yazılıbsa atlayır (xəbərdarlıq loglanır);
- vətəndaş yoxdursa atlayır;
- hesab **artıq varsa** yalnız email fərqlidirsə yeniləyir (parol, kart, balansa toxunmur);
- hesab yoxdursa yaradır, rol verir, ilk kartları açır, balansı `TopUp` jurnalı ilə ("Opening balance") yazır.

**Gmail "+etiket" hiyləsi:** Gmail ünvanda `+`-dan sonrakı hissəni atır. `subhan+aysel@gmail.com` və `subhan+elcin@gmail.com` bazada fərqli email sayılır, amma poçtların hamısı `subhan@gmail.com`-a düşür.

**Diqqət:** `AsNoTracking` ilə gətirilən kartın balansını dəyişib `SaveChanges` çağırsan **balans yazılmır** (yalnız jurnal sətri yazılar). Seeder-də bu xəta olmuşdu; kart `GetByIdForUserAsync` ilə (izlənən) təkrar götürülür.

## 5. Yoxlama (12 oktyabr 2026)
API (curl): yanlış vəsiqə, yanlış FİN, vəfat etmiş, artıq qeydiyyatlı → düzgün mesajlar; kiçik hərflə yazılan FİN işləyir; təkrar telefon rədd olunur; uğurlu qeydiyyatda 2 kart; eyni OTP tokeni ikinci dəfə işləmir. Brauzer: 1-ci, 2-ci, 3-cü addım və login, kartlar səhifəsində iki kart. Seed müştərisi Aysel-dən yeni müştəriyə köçürmə uğurlu ("Orxan N." maskalı). Sınaq istifadəçiləri silindi.

## Özünü yoxla
1. Niyə ad, soyad və doğum tarixini istifadəçi yox, reyestr verir?
2. Yanlış FİN ilə yanlış vəsiqə nömrəsi üçün niyə eyni mesaj qaytarılır?
3. Niyə `check-identity` rate limit-lidir?
4. `AsNoTracking` ilə götürülən kartın balansını dəyişəndə nə baş verir?
5. Gmail `+etiket` nədir və niyə seed müştəriləri üçün istifadə edirik?
6. Kartlar yaranmasa hesab niyə yenə hazır sayılır və müştəri nə edə bilər?

---

## 6. YENİLƏMƏ (12 oktyabr 2026): qeydiyyat kimlikdən başlayır, "Open account" birbaşa kimlik formuna aparır
Yuxarıdakı 2-ci bölmədəki sıra dəyişdi. **İndiki sıra:**
1. **Register** (`/Account/Register`): FİN, vəsiqə nömrəsi, **ad və soyad** yazılır. "Open account" düymələri (Home başlığı, hero, footer, mega kart, bazadakı `/Account/Welcome` düymələri) login olmayanları birbaşa bura gətirir (`NavLinks.OpenAccount`, `NavLinks.Register`). Dördü eyni vətəndaşa uyğun gəlməsə: "We couldn't find a citizen with these details...".
2. **ConfirmIdentity**: "Bu sizsiniz?" (ağ-qara şəkil, ad, soyadın baş hərfi, doğum ili).
3. **Email**: Welcome səhifəsindən gələn yeni email yadda qalıb (`RegEmail`), kod dərhal ona gedir. "Open account" ilə gələn adamdan `RegisterEmail` səhifəsində soruşulur (email sistemdə varsa "Log in instead" deyilir).
4. **VerifyOtp**: 6 rəqəmli kod.
5. **CreateAccount**: telefon və şifrə. Hesab açılır, ilk iki kart avtomatik yaranır.
6. **Login**.

**Ad və soyad müqayisəsi** (`AccountService.Fold`): böyük/kiçik hərf, boşluq və Azərbaycan hərfləri fərqlənmir (ə=e, ı=i, ö=o, ü=u, ş=s, ç=c, ğ=g), ona görə "emil kazimov" yazılan "Emil Kazımov" ilə uyğun gəlir. Hansının səhv olduğu deyilmir. Hesaba yenə reyestrdəki yazılış düşür ("Kazımov").
**API:** `check-identity` və `register` indi `name`, `surname` da alır (register təkrar yoxlayır, ki kimlik addımını keçib birbaşa hesab açmaq olmasın).
**Welcome** (email ilə) yalnız giriş üçün qalıb: email sistemdə varsa Login-ə, yoxdursa kimlik formuna aparır.
**Login olmuş adam** üçün "Open account" yoxdur: header-də görünmür, footer-də "Add a card", `/Account/Register` açsa ana səhifəsinə yönləndirilir.

---

## 7. Profil səhifəsi və header-də şəkil (12 oktyabr 2026)
- **API:** `GET api/account/profile` (`[Authorize]`, UserId tokendən): `ProfileDto { name, surname, fin, phoneNumber, email, photoUrl }`. Şəkil `AppUser.CustomerId` → `Customer.PhotoUrl` ilə gəlir (işçilərdə boşdur). Login cavabına da `photoUrl` əlavə olundu (`LoginResponse.PhotoUrl`).
- **MVC:** `ProfileController` (`/App/Profile`), `Views/Profile/Index.cshtml`. Header-dəki şəkil + ad (app və Home header) profilə aparır. Şəkil girişdə cookie claim-inə (`photo`) yazılır ki, hər səhifədə API çağırmaq lazım olmasın; köhnə sessiyada claim yoxdursa ikon göstərilir (yenidən giriş lazımdır).
- Şəkil CSS ilə ağ-qara göstərilir (`filter: grayscale(1)`). Header-də şəklin sağ alt küncündə kiçik adam işarəsi nişanı (`.header-user-badge`) qalır. Yalnız ad, soyad, FİN, telefon, email və şəkil göstərilir (reyestrin qalan məlumatı yox).

**Özünü yoxla:** 1) Niyə şəkli header üçün claim-ə yazırıq, hər dəfə API-dən almırıq? 2) Profilin UserId-si niyə sorğudan yox, tokendən götürülür?

**Düzəliş (7-ci bölmə):** header-dəki şəkil artıq cookie claim-i ilə deyil, `GET /App/Profile/Avatar` ünvanından gəlir (`ProfileController.Avatar`: API-dən profili alıb müştərinin şəklinə yönləndirir, brauzer 1 saat yadda saxlayır). Üstünlüyü: köhnə sessiyada da işləyir, yenidən giriş lazım deyil. Şəkli olmayanda (işçilər) 404 qayıdır və `onerror` şəkli silir, adam ikonu qalır. `LoginResponse.PhotoUrl` və `photo` claim-i götürüldü, küncdəki nişan da götürüldü: şəkil ikonun yerini tutur.

---

## 8. Welcome səhifəsi silindi, loqo hər yerdə bazadan (14 oktyabr 2026)
- **Welcome (email-first) səhifəsi silindi.** Giriş `Account/Login`-dən (email + parol), qeydiyyat "Open account" ilə `Account/Register`-dən (kimlik formu) başlayır. Giriş tələb edən səhifələrə girişsiz gələn Login-ə yönləndirilir (`LoginPath = /Account/Login`, `NavLinks.Page`, `SessionExpiredAsync`).
- **Footer-dakı email formu** `Account/Start`-a göndərilir: email sistemdə varsa Login (email hazır dolu), yoxdursa Register (email yadda qalır, kimlik təsdiqindən sonra ona OTP gedir). Xəta olsa Login-də mesaj göstərilir (ayrıca səhifə yoxdur).
- **Köhnə `/Account/Welcome` ünvanı** Login-ə yönləndirilir (köhnə link və bookmark üçün). Bazadakı düymələrdə `/Account/Welcome` dəyəri qala bilər: `NavLinks.FromDb` onu "hesab aç" kimi tanıyır (`LegacyWelcome`).
- `WelcomeVM` adı `RegisterEmailVM` oldu (yalnız email soruşan səhifə üçündür).
- **Loqo:** `SiteLogo` ViewComponent loqonu və şirkət adını Settings cədvəlindən (`Logo`, `CompanyName`) götürür və giriş/qeydiyyat səhifələrində, müştəri və admin panelində, xəta səhifəsində göstərir. Home header və footer eyni məlumatı öz komponentlərində istifadə edirdi. Bazada loqo yoxdursa köhnə SVG işarə qalır. Saytın loqosu `API/wwwroot/images/logo.png`-dir (Settings-də yolu `/images/logo.png`), yeni "C və dalğa" işarəsi ilə əvəz olundu.
