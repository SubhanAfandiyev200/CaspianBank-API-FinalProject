# 11. Əməliyyat tarixçəsi və kart çıxarışı

Fayllar: `Service/Services/HistoryService.cs`, `Repository/Repositories/TransactionRepository.cs`, `Repository/Models/TransactionPageResult.cs`, DTO-lar `Service/Helpers/DTOs/History`, validator-lar `Service/Helpers/Validators/History`, `Controllers/Client/HistoryController.cs`; MVC: `Controllers/HistoryController.cs`, `Views/History/Index.cshtml` və `Statement.cshtml`, `ViewModels/History/HistoryVMs.cs`.

## 1. Nəzəriyyə

### 1.1 Filtr + səhifələmə (pagination) server tərəfdə
Minlərlə əməliyyat varsa hamısını brauzerə göndərmək olmaz. Server **yalnız bir səhifə** qaytarır (`Skip((page-1)*size).Take(size)`) və **filtrləri bazada** tətbiq edir (LINQ → SQL `WHERE`).

Vacib: səhifələmədən **əvvəl** ümumi say və cəmlər filtrlənmiş **bütün** nəticədən hesablanır (yalnız cari səhifədən yox), yoxsa "Total in/out" yalnız göstərilən 20 sətrə aid olardı.

### 1.2 Vaxt zonaları
Bazada vaxt həmişə **UTC**. İstifadəçi isə Bakı vaxtı (UTC+4, yay vaxtı yoxdur) ilə düşünür. "2026-10-10" seçəndə nəzərdə tutulan: Bakı 00:00 → 24:00, yəni UTC 2026-10-09 20:00 → 2026-10-10 20:00. Ona görə:
```csharp
private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);
private static DateTime ToUtcStart(DateTime bakuDay) => DateTime.SpecifyKind(bakuDay.Date - BakuOffset, DateTimeKind.Utc);
```
- `From` günü: `ToUtcStart(from)`.
- `To` günü (daxil): `ToUtcStart(to.AddDays(1))` və sorğu `CreatedAt < toUtc` (yuxarı sərhəd **daxil deyil**).
- Göstərmə zamanı MVC (`_ActivityRow.cshtml`) `CreatedAt.ToLocalTime()` çağırır, yəni **serverin (MVC-nin işlədiyi kompüterin) saat qurşağı** ilə. Bakı kompüterində bu UTC+4-dür. Başqa saat qurşağında olan serverdə göstərilən saat fərqli olardı, amma gün sərhədləri (filtr) API-də həmişə Bakı üzrədir. Bu, bilinən sadələşdirmədir.

### 1.3 Çıxarış (statement) və balans bütövlüyü
Bank çıxarışı: seçilən tarix aralığı üçün
- **Açılış balansı** = aralıqdan əvvəlki **son** əməliyyatın `BalanceAfter`-i (heç nə yoxdursa 0).
- **Əməliyyatlar** aralıqda.
- **Bağlanış balansı** = aralıqdakı **son** əməliyyatın `BalanceAfter`-i (əməliyyat yoxdursa açılış balansı).
- Cəmlər: giriş və çıxış.
Yoxlama: `açılış + giriş − çıxış = bağlanış`. Bu bərabərlik tutursa jurnal düzgündür. Ona görə hər `Transaction` `BalanceAfter` saxlayır.

## 2. API

### 2.1 Tarixçə
`GET api/history?cardId=&type=&direction=&from=&to=&search=&page=1&pageSize=20`

| Parametr | Mənası |
|---|---|
| `cardId` | yalnız bu kart (boşdursa hamısı). Kart istifadəçinin deyilsə 404 |
| `type` | `TopUp`, `TransferOut`, `TransferIn`, `Commission`, `CardFee` |
| `direction` | `in` (giriş) və ya `out` (çıxış) |
| `from`, `to` | Bakı günləri (daxil) |
| `search` | `Reference`, `Description` və ya `Note`-da mətn (≤ 50 simvol) |
| `page`, `pageSize` | `page ≥ 1`, `pageSize` 1–50 |

Cavab `HistoryPageDto`: `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`, `TotalIn`, `TotalOut`.

`HistoryService.GetHistoryAsync`:
1. `HistoryFilterDtoValidator` (`EnsureValidAsync`).
2. `cardId` verilibsə `GetByIdForUserAsync`: sahibi deyilsə `NotFoundException` (404). Başqasının kart nömrəsini yazan heç nə öyrənmir.
3. `type` və `direction` parse olunur.
4. Bakı günləri UTC-yə çevrilir.
5. `GetPageForUserAsync(...)` sorğusu.
6. **Mövcud olmayan səhifə** istənilibsə son səhifəyə çəkilir və sorğu təkrarlanır (boş siyahı əvəzinə).

`TransactionRepository.GetPageForUserAsync`: əsas sorğu **həmişə** `t.Card.UserId == userId` ilə başlayır (başqasının heç bir sətri qaytarıla bilməz), sonra hər filtr yalnız verilibsə `Where`-ə əlavə olunur; sonra `CountAsync`, `SumAsync(in)`, `SumAsync(out)`; sonda `Include(Card)`, `OrderByDescending(CreatedAt).ThenByDescending(Id)`, `Skip/Take`.

> Niyə `ThenByDescending(Id)`? Eyni saniyədə yaranan iki sətirin sırası sabit olsun (köçürmənin `TransferOut`-u `Commission`-dan əvvəl yazılır).

### 2.2 Çıxarış
`GET api/history/statement?cardId=&from=&to=`
Validator: kart/from/to **məcburi**, `from ≤ to`, aralıq ≤ 366 gün.
`StatementDto`: `CardLabel`, `HolderName`, `From`, `To`, `OpeningBalance`, `ClosingBalance`, `TotalIn`, `TotalOut`, `Rows`.

## 3. MVC

### 3.1 `HistoryController` (`/App/History`)
- `Index([FromQuery] HistoryFilterVM filter)`:
  1. `api/cards` (filtrdəki kart seçimi üçün) → `Unauthorized` olsa session bitib, API işləmirsə `ApiUnavailable`.
  2. `From > To` olarsa xəbərdarlıq verilir və tarixlər atılır (API-yə səhv aralıq göndərilmir).
  3. `BuildQuery(filter)` (`QueryBuilder`): yalnız **dolu** filtrlər URL-ə əlavə olunur; `pageSize = 20`.
  4. `api/history?...` → `HistoryPageUIVM`.
  5. API son səhifəni qaytarsa formadakı `Page` də ona uyğunlaşdırılır.
- `Statement(cardId, from, to)`: məlumat yoxdursa `TempData["CardError"]` ilə `Index`-ə qayıdır; `FillBankInfoAsync` şirkət adı və ünvanı `api/settings`-dən alır (alınmasa ehtiyat mətn qalır).

### 3.2 Görünüş
- `Views/History/Index.cshtml`: başlıq ("Generate statement" düyməsi), statistika, filtr formu (Period düymələri `All / Day / Week / Month / Year`, tarix təkərləri, növ, kart, istiqamət, axtarış), cədvəl, səhifələmə, çıxarış modalı.
  - `Day` = bu gün; `Week` = son 7 gün (bu gün daxil); `Month` = bu ayın 1-dən bu günə; `Year` = bu ilin 1 yanvarından bu günə. Bunlar brauzerdə `app-cards.js`-də (`periodBounds`) hesablanır, `From`/`To` sahələrini doldurub formu göndərir.
  - Forma `GET`-dir: filtrlər URL-də qalır, səhifəni yeniləmək və paylaşmaq olur.
- `Views/History/Statement.cshtml`: çap üçün hazırdır ("Save as PDF").
- Paylaşılan partial-lar: `Views/Shared/_ActivityRow.cshtml` (əməliyyat sətri), `_CardThumb.cshtml` (kartın kiçik şəkli).
- JS: `app-cards.js` (period düymələri, avtomatik submit, `data-select`, `data-card-pick`), `wheel-date.js` (tarix təkərləri).

## 4. Təhlükəsizlik xülasəsi
- Hər sorğu `UserId` tokendən; sorğuda `userId` yoxdur.
- Repository sorğusu **həmişə** `Card.UserId == userId` ilə başlayır.
- `cardId` verilibsə əvvəlcə sahiblik yoxlanır.
- `search` parametrisiz LINQ (`Contains`) ilə gedir, SQL injection yoxdur (EF parametrləşdirir).

## Özünü yoxla

1. Niyə say və cəmlər səhifələmədən əvvəl hesablanır?
2. Bakı günü UTC-yə necə çevrilir? `To` günü niyə `AddDays(1)` və `<` ilə yoxlanır?
3. Çıxarışın açılış və bağlanış balansı necə tapılır? Bərabərlik nədir?
4. Başqasının `cardId`-sini yazan istifadəçi nə alır və niyə?
5. Mövcud olmayan səhifə istənilsə nə olur?
6. `OrderByDescending(CreatedAt).ThenByDescending(Id)` niyə iki sütunladır?
7. MVC `BuildQuery` niyə boş filtrləri URL-ə qoymur?
