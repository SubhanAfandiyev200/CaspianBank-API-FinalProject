# 105. Ağ/qara tema və saytın dilləri (az, ru, en)

## 1. Ağ/qara tema

**İdeya.** Bütün rənglər `style.css` və `home.css`-də CSS dəyişənləridir (`--bg`, `--surface`, `--ink`...). Qara tema həmin dəyişənlərin dəyərlərini dəyişir; kod `<html data-theme="dark">` olanda işləyir.

- `wwwroot/css/theme-dark.css` — qara temanın dəyişənləri və səhifələr üzrə düzəlişlər (home, çat vidceti, kart önizləməsi, qəbzlər, kredit banneri, admin).
- `Views/Shared/_ThemeInit.cshtml` — `<head>`-də kiçik inline skript: seçimi `localStorage["cb-theme"]`-dən oxuyub `data-theme` qoyur. **Səhifə ağ yanıb sönməsin** deyə CSS-dən əvvəl işləməlidir.
- `wwwroot/js/theme.js` — düymə (`data-theme-toggle`) seçimi dəyişir və yadda saxlayır.
- `Views/Shared/_SiteControls.cshtml` — dil menyusu + tema düyməsi, bütün layout-larda (customer, auth, home, admin) eyni yerdə (`nav-tools`, auth səhifələrində üzən).
- **Yeni səhifə yazanda:** rəngi sərt kodlama (`#fff`, `#0d1310`), həmişə `var(--...)` işlət. Yeni dəyişən lazımdırsa həm `:root`-a, həm `theme-dark.css`-ə əlavə et.
- Yoxlama: qara temada mətnin fonla kontrastı (gözlə baxmaq kifayətdir, ağ fon üzərində ağ mətn qalmasın).

## 2. Dillər: mətnlər verilənlər bazasındadır

**Qayda.** Açar **ingilis mətnin özüdür**. Səhifədə `@T["Transfer money"]` yazılır; DB-də (`Translations` cədvəli) `az` və `ru` sətirləri var. Tərcümə yoxdursa **ingilis mətn** göstərilir, ona görə heç nə sınmır.

### API tərəfi
- `Translation` entity: `Language` (`az`/`ru`), `Key` (≤400), `Value` (≤2000), `(Language, Key)` unikaldır. SQL Server-də müqayisə **böyük-kiçik hərfə həssas deyil** ("Get Started" = "Get started").
- `GET api/i18n/{az|ru|en}` — hamıya açıq (login səhifəsi də tərcümə olunur). Cavab `{ "English text": "tərcümə" }` lüğətidir; `en` üçün boşdur.
- `GET /api/admin/translations?search=&onlyMissing=&page=` və `PUT /api/admin/translations` (SuperAdmin, Admin) — admin səhifəsi üçün.
- İlk tərcümələr `Repository/Data/Seed/translations.sql` (≈810 mətn). Skript **təkrar işlədilə bilər**: yalnız olmayan sətirləri əlavə edir, admin dəyişibsə toxunmur. Yeni DB-də `sqlcmd -I -f 65001 -i translations.sql` ilə işlədin.

### MVC tərəfi
- `LanguageMiddleware` hər sorğuda dili seçir: əvvəl cookie `cb-lang`, sonra brauzerin `Accept-Language` başlığı (az/ru), yoxsa ingilis. Lüğəti `ITranslationStore` verir (5 dəqiqə yaddaşda; API işləməsə 30 saniyə boş lüğət, səhifə ingilis qalır).
- `ITranslator T` (`_ViewImports`-da inject olunub): `@T["text"]`, formatlı `@T["Balance {0}", amount]`. Verilənlər bazasından gələn mətnlərdə sonda boşluq ola bilər, ona görə `Translator` kəsilmiş halı da yoxlayır.
- `LanguageController.Set(lang, returnUrl)` cookie qoyur (yalnız yerli `returnUrl`).
- `<html lang="@T.Lang">` — brauzer və ekran oxuyucu üçün; **diqqət:** `lang="az"` olanda CSS `text-transform: uppercase` "i"-ni "İ" edir, ona görə tərcüməsiz ingilis mətn "OFFİCE" kimi görünə bilər. Tərcümə varsa problem yoxdur.
- **JavaScript mətnləri:** `cbt("Pay {0}", amount)` (`wwwroot/js/i18n.js`). Lüğət layout-da `window.I18N` kimi gəlir; açarlar DB-də **`js:` prefiksi** ilə saxlanılır (`js:Pay {0}`). Skriptlər `_I18nScript` partial-ından sonra yüklənməlidir.
- **Admin paneli də üç dildədir** (2026-10-24-dən): `_SiteControls` admin-də də dil menyusunu göstərir. Admin view-ları `@T["..."]` ilə yazılıb, tərcümələr `translations-admin-1..7.sql`-dədir. Yeni admin mətni yazanda eyni qaydadır: `@T["My text"]` + az/ru sətri.
- **Bildirişlər:** başlıqlar da, dəyişənli mətnlər də (şablon tərcüməsi) tərcümə olunur, bax aşağıdakı bölmə.

### Yeni mətn əlavə etmə qaydası (həmişə eyni)
1. Səhifədə `@T["My new text"]` (və ya JS-də `cbt("My new text")`) yaz.
2. `/Admin/Translations` səhifəsində "Add a text" ilə ingilis mətni (**dəqiq eyni**) və az/ru tərcüməsini yaz. JS mətni üçün açarın əvvəlinə `js:` qoy.
3. Yadda saxlayanda MVC-nin yaddaşındakı lüğət təzələnir, dəyişiklik dərhal görünür.
4. Uzun müddətli: yeni mətnləri `translations.sql`-ə də əlavə et ki, yeni DB-də də olsun.

Hansı mətnin tərcüməsi çatmır? `/Admin/Translations` → "Only missing a translation".

### Dəyişənli cümlələr: şablon tərcüməsi (2026-10-24)
API-nin göndərdiyi hazır cümlələrdə dəyişən hissə olur ("Your card •••• 1234 was blocked...", "We can approve 4,000 AZN instead of 6,000 AZN for loan KR-2026-000103..."). Hər real cümlə üçün açar yazmaq mümkün deyil, ona görə:
- Bazada açar **şablon** kimi yazılır: `Your card •••• {0} was blocked. You can unblock it with a code sent to your email.` və az/ru tərcüməsində də eyni `{0}` yerləri.
- `Translator` (MVC) əvvəl tam uyğunluğa baxır, tapmasa dəyişənli açarlardan **regex** qurur (`{n}` → `(?<gN>.+?)`, qalan hər şey `Regex.Escape`), mətni uyğunlaşdırır və tərcümədəki `{n}` yerlərinə tapılan dəyərləri qoyur. Şablon siyahısı lüğət obyektinə görə yaddaşda saxlanılır (lüğət 5 dəqiqədən bir yenilənəndə yenidən qurulur). Uzun dəyər (məs. "Not enough money.") özü də lüğətdə varsa tərcümə olunur.
- Beləliklə `@T[item.Message]`, `@T[Model.Error]` kimi çağırışlar dəyişənli mətnlərdə də işləyir. İngilis dilində lüğət boşdur, ona görə heç nə dəyişmir.
- Bildiriş mətnləri: `Repository/Data/Seed/translations-notifications.sql`. Yeni bildiriş yazanda mətnin şablon variantını eyni faylda az/ru ilə əlavə et.
- **Canlı bildiriş** (SignalR) API-dən ingiliscə gəlir. `notifications.js` onu göstərməzdən əvvəl `POST /App/Notifications/Translate`-ə göndərir (mətn sorğunun gövdəsindədir, URL-də yox), server seçilmiş dildəki başlıq və mətni qaytarır. Alınmasa ingilis mətn göstərilir.

### Digər köməkçilər (2026-10-24)
- `Helpers/TextTranslation.cs`: `T.Describe(description)` əməliyyat təsvirlərini şablonla tərcümə edir (`Transfer to your Gold card •••• 1387`), `T.CardLabel`, `T.Greeting(hour)` (Good morning...), `T.Person(name)` ad-soyadı rus dilində kirillcə yazır (`Aysel Məmmədova` → `Айсел Мемедова`, sadə transliterasiya).
- `Helpers/TranslateModelErrorsFilter.cs`: forma xətaları (`ModelState`) səhifə göstərilməzdən əvvəl tərcümə olunur (global filter).
- Təsdiq pəncərələri (`data-confirm-*`) və `Done` düyməsi də tərcümə olunur; `confirm-dialog.js` standart mətnləri `cbt("Are you sure?")` ilə alır.
- Aşağıdakı seed fayllar `Repository/Data/Seed/`-dədir və təkrar işlədilə bilər (olanı dəyişmir): `translations.sql`, `translations-transactions.sql`, `translations-messages.sql`, `translations-loans.sql`, `translations-topup.sql`, `translations-notifications.sql`. Yeni bazada hamısını `sqlcmd -S <server> -d CaspianBankDataBase -E -C -f 65001 -i <fayl>` ilə işlət.
- Tünd tema paleti qapqara deyil, tünd yaşıldır (`theme-dark.css`: `--color-bg #0c2a1f`, kartlar `#123a2b`). Loqo zolağı (`.brand-img`) qaranlıqda rəngi saxlayır, ağ fonu isə `mix-blend-mode` ilə silir (zolağın öz fonu `partners` ilə eyni olmalıdır, çünki hərəkət edən element öz qatını yaradır).
### Nə tərcümə olunmur (bilərəkdən)
- Admin paneli ingilis dilindədir. API-nin doğrulama xətaları cümlə tam uyğun gələndə və ya şablonla tərcümə olunur; bazada olmayan cümlə ingiliscə qalır. Staff bildirişləri (məs. "New loan application") admin üçündür və ingiliscədir.
- Home CMS mətnləri (hero, xidmətlər...) admin tərəfindən dəyişdirilsə, yeni mətnin tərcüməsi olmur və ingilis qalır; yeni mətni `Translations`-a əlavə etmək lazımdır.

## Admin CMS mətnlərinin dilə görə redaktəsi (2026-10-24)
Ana səhifənin mətnləri (Hero, About və sütunları, Services, Benefits, Ticker, kart dizaynının adı, footer başlığı və təsviri) bazada **ingiliscə** saxlanır. Az və ru tərcümələr `Translations` cədvəlindədir, açar həmin ingilis mətnin özüdür.
- **İstifadə:** admin bu mətnlərin **Edit** səhifəsini açır, yuxarıda `EN | AZ | RU` düymələri var. EN: adi redaktə. AZ və ya RU: eyni mətn sahələrində həmin dilin tərcüməsi görünür (yoxdursa boş, ingilis mətn işarə kimi göstərilir), yazıb "Save changes" basanda tərcümə yadda saxlanır (boş saxlasan tərcümə silinir və ingilis mətn göstərilir). Şəkil və digər sahələr tərcümə rejimində gizlənir.
- **İş:** `wwwroot/js/lang-edit.js` (səhifədə `data-translatable` sahələri və `_LangTabs` partial-ı varsa işləyir), MVC `Areas/Admin/Controllers/ContentTranslationsController` (`Lookup`, `Save`), API `POST api/admin/translations/lookup` və `PUT api/admin/translations/content` (`TranslationService.LookupAsync`, `SaveContentAsync`; biri digər dilə toxunmur). Saxladıqdan sonra MVC-nin lüğət yaddaşı təzələnir.
- **İngilis mətn dəyişəndə tərcümə itmir:** `AppDbContext.SaveChanges` CMS varlıqlarının (About, AboutPillar, BenefitSection/Item, ServiceSection/Item, CardHero, HomeTicker, CardDesign, Setting) mətn sahələri dəyişəndə həmin köhnə açarın tərcümə sətirlərini yeni mətnə köçürür (eyni saxlamada). Yeni mətnin tərcüməsi artıq varsa toxunmur; yalnız böyük-kiçik hərf fərqi varsa heç nə etmir (açar böyük-kiçik hərfə həssas deyil).
- Yeni redaktə səhifəsinə əlavə etmək: mətn inputlarına `data-translatable`, forma üstünə `<partial name="_LangTabs" />`. Create səhifələrində yoxdur: əvvəl ingilis mətnlə yarat, sonra Edit-də tərcümə et.
- Testlər: `ContentTranslationTests`.
