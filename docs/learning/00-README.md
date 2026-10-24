# Caspian Bank: öyrənmə sənədləri

Bu qovluq layihəni **sıfırdan başa düşmək və yenidən yazmaq** üçündür. Məqsəd: kodu oxuyub "bu nədir, niyə belədir, nə hardan gəlir" suallarına özün cavab verə biləsən.

> Bu sənədlər layihə yazıldıqca **böyüyür**. Hər yeni funksiyadan sonra aid fayla bölmə əlavə olunur, əlavələr `99-changelog.md`-də qeyd edilir.
> Tarix: ilk versiya 2026-10-10. Qeyd edilən fayl yolları iki repoya aiddir: **API** (bu repo) və **MVC** (`CaspianBank-MVC-FinalProject`).

## Necə oxumalı

1. **Sırayla oxu.** Hər fayl əvvəlki fayldakı anlayışlara söykənir.
2. Hər fayl iki hissədən ibarətdir: **Nəzəriyyə** (ümumi anlayış, hər layihədə eynidir) və **Bizim layihədə** (bu anlayışın konkret kodu, fayl yolu ilə).
3. Kod parçalarını oxuyanda faylı yanında aç. Sənəd kodu əvəz etmir, yol göstərir.
4. Hər faylın sonunda **"Özünü yoxla"** sualları var. Cavabı yaza bilmirsənsə, faylı yenidən oxu.
5. Sıfırdan yazmağa başlayanda `91-rebuild-roadmap.md` addım-addım sıranı verir.

## Fayllar

| Fayl | Nə haqqındadır |
|---|---|
| `01-big-picture.md` | Layihə nədir, iki layihə (API + MVC), N-Tier, bir sorğunun yolu, texnologiyalar |
| `02-domain-and-database.md` | Entity-lər, EF Core, konfiqurasiya, migration, seed, unikal index, RowVersion |
| `03-repository-layer.md` | Repository nədir, `BaseRepository<T>`, xüsusi repository-lər, tranzaksiya |
| `04-service-layer.md` | Service, DTO, FluentValidation, exception-lar, `ServiceResult`, DI qeydiyyatı |
| `05-auth-jwt-otp.md` | Identity, JWT, rollar, OTP, qeydiyyat/giriş/şifrə bərpası, lockout |
| `06-api-controllers-middleware.md` | API controller-lər, `ExceptionHandlingMiddleware`, rate limiting, `Program.cs` |
| `07-files-upload.md` | Şəkil/video yükləmə, magic bytes, təhlükəsiz silmə |
| `08-home-cms-pattern.md` | Home səhifəsinin admin idarəsi (Brand/Ticker/Hero/…) və təkrarlanan şablon |
| `09-cards.md` | Kart növləri, ilk kartlar, ödənişli kart, top-up, bloklama |
| `10-transfers.md` | Köçürmə: qaydalar, komissiya, idempotency, concurrency, qəbz |
| `11-history-statement.md` | Əməliyyat tarixçəsi, filtr, səhifələmə, kart çıxarışı, Bakı vaxtı |
| `12-mvc-architecture.md` | MVC layihəsi: `ApiControllerBase`, `BearerTokenHandler`, ViewComponent-lər |
| `13-mvc-auth-flow.md` | MVC-də giriş: cookie içində JWT, Welcome → Login/Register axını |
| `14-mvc-admin-area.md` | Admin Area, rollar, CRUD səhifələri, fayl yükləmə formaları |
| `15-mvc-customer-pages-js.md` | Müştəri səhifələri (App, Transfer, History) və JavaScript faylları |
| `90-glossary.md` | Terminlər lüğəti (qısa izahlar) |
| `91-rebuild-roadmap.md` | Layihəni sıfırdan yenidən yazma planı (addım-addım) |
| `92-interview-questions.md` | Müsahibə/müdafiə sualları və cavab planları |
| `93-planned-registry-and-bank-directory.md` | PLAN: saxta dövlət reyestri, FİN ilə qeydiyyat, bank BIN kataloqu, başqa banka köçürmə (reyestr və qeydiyyat hissəsi artıq yazılıb, BIN/başqa banka köçürmə gözləyir) |
| `94-registry-seed-draft.md` | Reyestrin nümunə məlumatları (vətəndaşlar, iş yerləri, xarici kreditlər) |
| `95-registration-with-registry.md` | FİN ilə qeydiyyat, "Bu sizsiniz?" təsdiqi, profil, hazır müştərilər |
| `96-home-stats.md` | Home "Trust indicators": bazadan canlı rəqəmlər |
| `97-loans.md` | Kreditlər: qaydalar, müraciət, işçi baxışı, qara siyahıda iki təsdiq (kredit şöbəsi + Super Admin), ödəniş, erkən ödəniş, cərimə, Court, gündəlik iş |
| `98-notifications-signalr.md` | Bildirişlər və SignalR: canlı çatdırılma, hub bileti, kim kimə nə bildirir, dedupe |
| `99-changelog.md` | Sənədlərdə nə vaxt nə əlavə olunub, hansı funksiya sənədləşib |
| `100-bills.md` | Ödənişlər: mobil operatorlar, kommunal, internet, kredit kodu, işarəli balans, aylıq hesab, cashback, aylıq təkrarlanan ödənişlər, admin loqo idarəsi |
| `101-deposits.md` | Müddətli depozitlər: qaydalar, faiz düsturu, açılış və ödəniş, arxa plan işi, xəzinə jurnalı (Treasury), kredit yoxlaması, MVC səhifələri |
| `102-staff-management.md` | İşçilərin idarəsi: əlavə etmə, rol dəyişmə, deaktiv, dəvət linki, açıq tokenin etibarsız edilməsi |
| `103-security-alerts.md` | Şübhəli fəaliyyət: qaydalar, siqnallar, "təhlükəsizdir" və ya hesabı dondur, audit |
| `104-reports-recurring-analytics-rules.md` | Şübhəli əməliyyat bildirişi, təkrarlanan köçürmə, xərc analitikası, bankın qaydaları |
| `105-theme-and-languages.md` | Ağ/qara tema, dillər (az/ru/en), tərcümələrin DB-də saxlanması, dəyişənli cümlələrin şablon tərcüməsi, yeni mətn əlavə etmə |
| `106-tests.md` | xUnit testləri: necə işlədilir, quruluş, paylaşılan/təcrid olunmuş baza, testlərin tapdığı xətalar |
| `107-logging-serilog.md` | Jurnal (Serilog): fayllar, səviyyələr, nə yazılır və nə yazılmır |

## Layihənin bir cümləlik xəritəsi

```
Brauzer ──> MVC (səhifələr, forma, cookie) ──HttpClient + JWT──> API (qaydalar, baza) ──EF Core──> SQL Server
```

- **MVC** istifadəçiyə HTML göstərir və API-yə müraciət edir. Bazaya birbaşa toxunmur.
- **API** bütün qaydaları (kim nə edə bilər, komissiya, balans) və bazanı saxlayır.
- Bazaya yazmaq yalnız API-dən keçir. Bu, "pul məntiqi"ni tək yerdə saxlayır.

## Hazırkı vəziyyət (2026-10-17)

Hazırdır: qeydiyyat (reyestrlə)/giriş/OTP/şifrə bərpası, profil, Home səhifəsinin bütün idarəsi (admin) və canlı rəqəmləri, kartlar, top-up, kart bloklama, köçürmə, əməliyyat tarixçəsi və çıxarış, kreditlər (müştəri səhifələri, işçi baxışı, erkən ödəniş, cərimə, gündəlik iş), canlı bildirişlər (SignalR), hər səhifədə köməkçi (chat) düyməsi (önizləmə), **ödənişlər (Bills): mobil, kommunal, internet, kredit kodu, cashback, aylıq təkrarlanan ödənişlər, admin loqo idarəsi**.
Statik dizayn (nümunə sətirlərlə): Contact/chat səhifəsi, şübhəli əməliyyatı bildir, admin Users/Reports/Audit/Tickets/Bank dashboard/Staff.
Hələ yoxdur (gələcək): depozit, bank xəzinəsi (treasury), hesab dondurma, hesabat/audit məntiqi, işçi idarəsinin real işləməsi, real köməkçi cavabları. Bunlar yazıldıqca sənədlərə əlavə olunacaq.
