# 03. Repository qatı

Layihə: `Repository/`. Burada bazaya **necə** müraciət olunduğu yazılır. Qayda: bu qat "bankın qaydalarını" bilmir; o, yalnız "bunu yaz", "bunu oxu" əmrlərini icra edir.

## 1. Nəzəriyyə

### 1.1 Repository pattern
Service qatı `dbContext.Cards.Where(...)` yazmasın deyə, bazaya çıxış ayrıca sinifdədir. Fayda:
- **Bir yerdə**: "istifadəçinin kartları" sorğusu bir dəfə yazılır, hər yerdə təkrar çağırılır.
- **Dəyişmək asan**: bazanı dəyişsək yalnız Repository dəyişir.
- **Test**: interfeys sayəsində Service-i saxta repository ilə test etmək olur.

### 1.2 Interface + implementation
Hər repository-nin iki hissəsi var:
```csharp
public interface IBrandRepository : IBaseRepository<Brand> { }       // "nə edə bilər" (müqavilə)
public class BrandRepository : BaseRepository<Brand>, IBrandRepository { ... }   // "necə edir"
```
Service yalnız interfeysi görür (`IBrandRepository`).

### 1.3 Dependency Injection (DI)
Service konstruktorunda `IBrandRepository` istəyir; **kim verəcək?** DI konteyneri (ASP.NET-in daxilində). Biz `Repository/DependencyInjection.cs`-də deyirik: "`IBrandRepository` istəyən olsa `BrandRepository` ver."
```csharp
services.AddScoped<IBrandRepository, BrandRepository>();
```
- **Scoped**: hər HTTP sorğusu üçün bir nüsxə. (`DbContext` də Scoped-dır: bir sorğuda eyni context istifadə olunur.)
- Transient: hər dəfə yeni. Singleton: bütün tətbiq boyu bir dənə.

### 1.4 Generic repository
`BaseRepository<T>`: bütün entity-lərə ortaq əməliyyatları bir dəfə yazır.

### 1.5 Tracking və AsNoTracking
EF yüklənmiş obyektləri **izləyir** (dəyişsən `SaveChanges` bilir). Yalnız oxuyuruqsa `.AsNoTracking()`, çünki daha sürətlidir və yaddaşa az yük verir.

### 1.6 Tranzaksiya
Bir neçə əməliyyat "ya hamısı, ya heç biri" olmalıdırsa tranzaksiya lazımdır. Misal: köçürmədə göndərənin balansı azalır, alanın artır; ortada xəta olsa pul itməməlidir.
- Tək `SaveChanges()` çağırışı onsuz da tranzaksiyadır.
- Daha mürəkkəb halda `BeginTransactionAsync()` → ... → `CommitAsync()` / `RollbackAsync()`.

### 1.7 Concurrency (eyni vaxtlılıq)
İki istifadəçi eyni anda eyni karta pul göndərsə? Hər ikisi balansı `100` oxuyur, hər biri öz hesabını yazır, birinin dəyişikliyi itir. **Optimistic concurrency** (RowVersion) bunun qarşısını alır: yazanda EF yoxlayır "oxuyandan bəri sətir dəyişməyib?" Dəyişibsə `DbUpdateConcurrencyException` atır. Repository bunu tutub yenidən cəhd edir (bax `10-transfers.md`).

## 2. Bizim layihədə

### 2.1 `BaseRepository<T>`
`Repository/Repositories/BaseRepository.cs`

| Metod | Nə edir |
|---|---|
| `GetAllAsync()` | Hamısı, `AsNoTracking`, yeni olan əvvəldə (`CreatedAt` azalan) |
| `GetAsync()` | Ən yeni **bir** sətir (tək-sətirli modellər üçün: Hero, BenefitSection) |
| `GetByIdAsync(id)` | `Id` ilə, **izlənən** (dəyişmək/silmək üçün) |
| `AddAsync(entity)` | Əlavə et və dərhal `SaveChanges` |
| `UpdateAsync(entity)` | Yenilə və dərhal `SaveChanges` |
| `DeleteAsync(entity)` | Sil və dərhal `SaveChanges` |

> Diqqət: `AddAsync/UpdateAsync/DeleteAsync` **özü** `SaveChanges` çağırır. Service-də `await` unudulsa əməliyyat yarımçıq qala bilər (`CS4014` xəbərdarlığı).

`protected readonly AppDbContext _dbContext; protected readonly DbSet<T> _dbSet;` alt siniflər üçün açıqdır.

### 2.2 Sadə repository (Brand nümunəsi)
`BrandRepository : BaseRepository<Brand>, IBrandRepository` boş gövdəlidir: bütün lazım olan `BaseRepository`-dədir. Brand, HomeTicker, Hero, BenefitSection və s. eyni şəkildədir.

### 2.3 `CardRepository`: pul əməliyyatları

`Repository/Repositories/CardRepository.cs`

| Metod | Nəticə |
|---|---|
| `GetByUserAsync(userId)` | İstifadəçinin kartları (dizayn daxil), yaradılma sırası ilə |
| `GetByIdForUserAsync(cardId, userId)` | **Sahibliyi yoxlayır**: kart başqasınındırsa `null` |
| `GetByNumberAsync(number)` | Nömrə ilə (köçürmə alıcısı) |
| `CardNumberExistsAsync` | Yeni nömrə unikaldırmı |
| `UserHasCardsAsync` | İlk kartlar artıq verilibmi |
| `AddRangeAsync(cards)` | Bir neçə kartı bir `SaveChanges` ilə; unikal indeks pozulsa `false` |
| `TopUpAsync(card, amount, desc)` | `Balance += amount` + `Transaction` sətri, **bir** `SaveChanges`; concurrency olsa `false` |
| `AddPaidCardAsync(newCard, fundingId, userId, fee)` | Tranzaksiyada: haqqı çıx, kartı yarat, `CardFee` jurnalı yaz |

Burada diqqət:
- `GetByIdForUserAsync`: "başqasının kartına baxmaq" hücumunun qarşısı. **Id ilə tapmaq kifayət deyil, sahibi də yoxlanır.**
- `AddPaidCardAsync` "balans servisdən gələn rəqəmə güvənmir": tranzaksiyanın içində balansı **yenidən oxuyur** və yoxlayır. Servis qərarı hazırlayıb, repository onu "təzə məlumatla təsdiqləyir".
- Nəticə üçün `enum CardOpenResult` istifadə olunur (exception əvəzinə): `Success, FundingCardNotFound, FundingCardBlocked, InsufficientFunds, Conflict`. Service bu nəticəyə görə istifadəçi mesajı seçir. Repository istifadəçi mesajı bilmir.

### 2.4 `TransferRepository`
Bax `10-transfers.md`: bütün köçürmə bir tranzaksiyada. Burada qısa: `ExecuteAsync(TransferPlan)` 8 cəhdə qədər yenidən cəhd edir; hər cəhddə balansı təzədən oxuyur, yoxlayır, 3 jurnal sətri yazır.

`Repository/Results/TransferResult.cs`: `TransferOutcome` enum-u və `TransferPlan`, `TransferRecord` rekordları. **Plan**: servis hesablayıb, yoxlayıb hazırladığı sadə məlumat paketi; repository onu icra edir. Bu, Repository-ni Service-dən asılı etmir (Repository Service-in DTO-larını tanımır).

### 2.5 `TransactionRepository`: tarixçə sorğuları
- `GetByCardAsync`, `GetRecentForUserAsync`: son N əməliyyat.
- `GetPageForUserAsync(...)`: **filtr + səhifələmə** (bax `11-history-statement.md`). Primitive parametrlər qəbul edir (`string userId, int? cardId, ...`), DTO yox, çünki Repository DTO-nu tanımır. Qaytarır: `TransactionPageResult { Items, TotalCount, TotalIn, TotalOut }`.
- `GetLastBeforeAsync`, `GetByCardInRangeAsync`: kart çıxarışı üçün.

### 2.6 `AccountRepository`: Identity-ni gizlədir
`UserManager<AppUser>` Identity-nin əsas sinfidir. `AccountRepository` onu sarıyır: `RegisterAsync`, `CheckPasswordAsync`, `GetRolesAsync`, `GeneratePasswordResetTokenAsync`, `IsLockedOutAsync`, `RegisterFailedAttemptAsync` və s. Service-lər `UserManager`-i birbaşa bilmir, yalnız `IAccountRepository`-ni.

### 2.7 `EmailOtpRepository`
OTP üçün xüsusi sorğular: `GetLatestActiveAsync` (istifadə edilməyən, vaxtı keçməyən son kod), `GetLatestAsync`, `GetVerifiedAsync(email, tokenHash, verifiedAfter)`, `InvalidateActiveAsync` (`ExecuteUpdateAsync` ilə bir sorğuda köhnə kodları ləğv edir).

### 2.8 Seeder-lər (`Repository/Data`)
- `RoleSeeder`: `Roles.All`-dakı rolları yaradır (yoxdursa).
- `AdminSeeder`: `SeedAdmins` siyahısından işçi hesabları yaradır. Hesab artıq varsa **toxunmur** (parol və rol dəyişməz). Parollar `appsettings.Local.json`-dadır ki, GitHub-a düşməsin.

## 3. Yeni repository yazma şablonu

1. `Repository/Repositories/Interfaces/IXRepository.cs`: `IBaseRepository<X>`-dən miras + xüsusi metodlar.
2. `Repository/Repositories/XRepository.cs`: `BaseRepository<X>, IXRepository`.
3. `Repository/DependencyInjection.cs`-ə `AddScoped<IXRepository, XRepository>()`.

## Özünü yoxla

1. Repository nə üçündür? Service niyə `DbContext`-i birbaşa çağırmır?
2. `AddScoped`, `AddTransient`, `AddSingleton` fərqi?
3. `AsNoTracking()` nə vaxt istifadə olunur?
4. `GetByIdForUserAsync` niyə sadə `GetByIdAsync`-dən təhlükəsizdir?
5. Niyə `AddPaidCardAsync` tranzaksiyanın içində balansı yenidən oxuyur?
6. Repository niyə istifadəçiyə mesaj qaytarmır, enum qaytarır?
7. `TransferPlan` nə üçündür? Niyə Repository DTO qəbul etmir?
