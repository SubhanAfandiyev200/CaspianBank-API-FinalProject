# 02. Domain və verilənlər bazası

Layihə: `Domain/` (class library). Burada **heç bir iş məntiqi yoxdur**: yalnız "bazada nə saxlanır" və "necə saxlanır".

## 1. Nəzəriyyə

### 1.1 Entity nədir?
Entity: bazadakı bir cədvəlin C# sinfidir. Sinfin hər property-si cədvəlin sütunudur, hər obyekt isə cədvəlin bir sətridir.

```csharp
public class Brand : BaseEntity        // Brands cədvəli
{
    public string Name { get; set; }   // Name sütunu
    public string Image { get; set; }  // Image sütunu
}
```

### 1.2 ORM və EF Core
Əgər SQL-i əl ilə yazsaydıq: `INSERT INTO Brands (Name, Image) VALUES (...)`. **ORM** (Object-Relational Mapper) bunu bizim yerimizə edir: `dbContext.Brands.Add(brand); SaveChanges();`.
EF Core bizim ORM-dir. Əsas hissələri:
- **DbContext** (`AppDbContext`): bazaya "pəncərə". `DbSet<Brand> Brands` = `Brands` cədvəli.
- **Change Tracker**: EF yüklənmiş obyektlərin dəyişikliyini izləyir. `SaveChanges()` çağırılanda yalnız dəyişənləri SQL-ə çevirir.
- **LINQ**: C# ilə sorğu yazırıq (`.Where(...)`), EF onu SQL-ə çevirir.
- **Migration**: kod dəyişəndə bazanı da dəyişən skript (bax 1.6).

### 1.3 Əlaqələr (relationships)
- **One-to-many**: bir istifadəçinin çox kartı var. Kartda `UserId` (foreign key) və `User` (navigation property) olur, istifadəçidə `ICollection<Card> Cards`.
- **Foreign key (FK)**: başqa cədvəlin sətrinə istinad edən sütun.
- **Navigation property**: kodda əlaqəli obyektə keçmək üçün (`card.User`).
- **DeleteBehavior.Restrict**: əlaqəli sətir varsa silməyə icazə vermir (məsələn kartı olan istifadəçi silinmir).

### 1.4 Konfiqurasiya: Data Annotation vs Fluent API
Sütunun uzunluğunu, məcburi olmasını, indeksi iki yolla vermək olar. Biz **Fluent API** istifadə edirik: hər entity üçün ayrıca `IEntityTypeConfiguration<T>` sinfi (`Domain/Configurations/`). Üstünlüyü: entity təmiz qalır, bütün baza qaydaları bir yerdə toplanır.

`AppDbContext.OnModelCreating` bunların hamısını bir sətirlə tapır:
```csharp
builder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
```

### 1.5 Faydalı baza anlayışları
- **Unique index**: sütunda təkrar dəyər olmasın (kart nömrəsi, `Setting.Key`).
- **Filtered unique index**: yalnız şərtə uyğun sətirlərdə unikal. (`FinKod IS NOT NULL`: boş FİN-lər təkrar ola bilər.)
- **Composite index**: bir neçə sütun birlikdə (`CardId, CreatedAt`: kartın tarixçəsi tez gəlsin).
- **Precision**: pul `decimal(18,2)`. Pul üçün `double` və `float` **heç vaxt**: yuvarlaqlaşdırma xətası.
- **RowVersion (concurrency token)**: sətirdə avtomatik dəyişən "versiya". İki əməliyyat eyni anda eyni sətri dəyişəndə ikincisi `DbUpdateConcurrencyException` alır (bax `10-transfers.md`).
- **HasDefaultValueSql("GETUTCDATE()")**: SQL-dən əl ilə sətir əlavə edəndə `CreatedAt` özü dolsun.
- **Seed (HasData)**: migration ilə bazaya başlanğıc sətirlər yazılır.

### 1.6 Migration nədir?
Migration: modeldəki dəyişikliyi bazaya tətbiq edən C# faylı (`Up` = tətbiq et, `Down` = geri qaytar). İş axını:
1. Entity/konfiqurasiya dəyişir.
2. `dotnet ef migrations add AddedXTable` migration faylı yaradır.
3. `Database.MigrateAsync()` (Program.cs-də) başlanğıcda hələ tətbiq olunmamış migration-ları bazaya tətbiq edir.

Bu layihədə migration-lar (`Repository/Migrations`), xronoloji:
`AddedUserTables` → `CreatedHomeTickerTable` → `AddedCreatedAtDefaultConfiguration` → `AddedBrandTable` → `AddedAboutsTable` → `AddedVideoPathToAboutModel` → `AddedEmailOtpTable` → `AddedServiceSectionsAndServiceitemsTables` → `AddedBenefitItemAndBenefitSectionTables` → `AddedCardDesignTable` → `AddedCardHeroTable` → `AddedSettingsTable` → `AddedCardTables` → `AddedTransactionTable` → `AddedPurposeToEmailOtp` (OTP-yə məqsəd sütunu).

Əhəmiyyətli: Bazanı müəllimin kompüterinə **import** edəndə də tətbiq işə düşəndə `MigrateAsync()` çatmayan migration varsa onu əlavə edir, hazır baza toxunulmaz qalır.

## 2. Bizim layihədə

### 2.1 `BaseEntity`
`Domain/Common/BaseEntity.cs`
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```
Bütün entity-lər bundan miras alır: hamısında `Id` (primary key) və `CreatedAt` (UTC) var. `BaseRepository<T>` bu iki sahəyə arxalanır (`OrderByDescending(m => m.CreatedAt)`).

> Vaxt həmişə **UTC** saxlanılır. İstifadəçiyə göstərəndə Bakı vaxtına çevrilir (bax `11-history-statement.md`).

### 2.2 Entity-lərin xəritəsi

| Entity | Məqsəd | Əlaqə |
|---|---|---|
| `AppUser` (`IdentityUser`-dan) | İstifadəçi: Name, Surname, BirthDay, FinKod, IsRestricted | `Cards` (1:N) |
| `Card` | Kart: nömrə, tier, balans, bloklu, RowVersion | `User`, `CardDesign` |
| `Transaction` | Balansı dəyişən hər əməliyyatın qeydi (jurnal) | `Card` |
| `CardTierConfig` | Hər kart növünün qaydaları: haqq, cashback, limit, komissiya | `CardDesign` |
| `CardDesign` | Kartın fon şəkli (Home-da göstərilir) | `Cards`, `TierConfigs` |
| `EmailOtp` | Email kodu (hash), məqsəd (`Purpose`: qeydiyyat və ya kart bloku/blokdan çıxarma), cəhd sayı, təsdiq tokeni | yoxdur |
| `Brand`, `HomeTicker`, `CardHero`, `Setting` | Home səhifəsinin sadə məzmunu | yoxdur |
| `About` + `AboutPillar` | "About" bölməsi + sütunlar | 1:N |
| `ServiceSection` + `ServiceItem` | "Services" başlığı + kartlar | 1:N |
| `BenefitSection` + `BenefitItem` | "Benefits" başlığı + elementlər | 1:N |

### 2.3 Enum-lar
- `CardTier`: `Cashback, Standard, Silver, Gold`. Bazada **rəqəm** kimi saxlanır (`Cashback = 0`). Konfiqurasiyada `[Tier] = 0` yazılması buna görədir.
- `TransactionType`: `TopUp, TransferOut, TransferIn, Commission, CardFee`.

### 2.4 `AppUser`: Identity ilə əlaqə
`AppUser : IdentityUser`. `IdentityUser` hazır sahələri verir: `Id` (string GUID), `Email`, `UserName`, `PasswordHash`, `PhoneNumber`, `EmailConfirmed`, `LockoutEnd`, `AccessFailedCount` və s. Biz əlavə etdik:
- `Name`, `Surname`, `BirthDay`
- `FinKod`: FİN kod, ilk kart açılanda daxil edilir; unikaldır (filtered unique index)
- `IsRestricted`: admin dondurub (login ola bilmir)
- `CreatedAt`

`AppDbContext : IdentityDbContext<AppUser>`: Identity-nin hazır cədvəllərini (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` və s.) avtomatik yaradır.

### 2.5 Kritik konfiqurasiyalar (nə üçün belədir)

**`CardConfiguration`**
- `CardNumber` unikal: köçürmə nömrə ilə tapılır.
- `Balance` `decimal(18,2)`.
- `RowVersion` `IsRowVersion()`: **eyni anda iki əməliyyat balansı pozmasın**.
- `UX_Cards_UserId_Cashback`: `UserId` üzrə unikal, yalnız `[Tier] = 0` olan sətirlərdə. Yəni **hər istifadəçinin yalnız bir Cashback kartı** ola bilər, bunu baza özü qoruyur (kod xətası olsa belə).
- `DeleteBehavior.Restrict` hər iki FK-da.

**`TransactionConfiguration`**
- Unikal index `(Reference, Type)`: **eyni əməliyyat eyni növlə iki dəfə yazıla bilməz**. Forma iki dəfə göndərilsə, pul iki dəfə köçməsin deyə.
- İndeks `(CardId, CreatedAt)`: kartın tarixçəsi sürətli.
- `Description` max 200, `Reference` max 30, `Note` max 140.

**`CardTierConfigConfiguration`**
- `Tier` unikal: hər növün yalnız bir qaydası.
- `HasData(...)`: 4 növün başlanğıc qaydaları (kart açma bunlarsız işləməz):

| Tier | IssueFee | Cashback % | TransferLimit | Commission % | Dizayn |
|---|---|---|---|---|---|
| Cashback | 0 | 0 | 0 | 0 | 4 |
| Standard | 10 | 0.5 | 500 | 1 | 1 |
| Silver | 15 | 1 | 2000 | 0.6 | 2 |
| Gold | 40 | 1.5 | 10000 | 0.3 | 3 |

(Admin bunları sonra dəyişə bilər. Bu rəqəmlər yalnız başlanğıc dəyərlərdir.)

**`CardDesignConfiguration`**: 4 başlanğıc dizayn (`Regular`, `Silver`, `Gold`, `Cashback`). Seed-in `Id`-ləri `CardTierConfig`-də FK kimi istifadə olunur. Cashback dizaynı `ShowOnHome = false`.

**`EmailOtpConfiguration`**: `CodeHash` max 100, `Email` indeksli, `Purpose` max 50 (default `Register`).

**`SettingConfiguration`**: `Key` unikal. Setting-lər açar-dəyər cütüdür, yeni ayar üçün yeni cədvəl lazım deyil.

### 2.6 `Domain/Constants`
- `Roles`: rol adları (`Customer`, `SuperAdmin`, `Admin`, `Accountant`, `CustomerSupport`, `Security`) və `All` massivi.
- `SettingKeys`: Settings cədvəlindəki açarlar (`Logo`, `CompanyName`, `Address`, `Email`, `PhoneNumber`, `FooterTitle`, `FooterDescription`, `Copyright`). Sayt bunları adla oxuyur, ona görə admin açarın adını dəyişmir, yalnız dəyərini.
- `ButtonDestinations`: Home düymələrinin gedə biləcəyi səhifələrin siyahısı (admin linki əl ilə yazmır, siyahıdan seçir).

> MVC layihəsi Domain-i tanımır, ona görə `Roles` MVC-də `AppRoles` kimi **təkrar yazılıb**. Onlar eyni olmalıdır.

## 3. Konkret nümunə: yeni cədvəl əlavə etmək

Misal: `Notification` əlavə etmək.
1. `Domain/Entities/Notification.cs` (`BaseEntity`-dən miras, sahələr).
2. `Domain/Configurations/NotificationConfiguration.cs` (uzunluqlar, indekslər, `CreatedAt` default).
3. `AppDbContext`-ə `public DbSet<Notification> Notifications { get; set; }`.
4. Terminalda (Repository layihəsində): `dotnet ef migrations add AddedNotificationTable --startup-project ..\CaspianBank-API-FinalProject`.
5. Tətbiqi işə sal: `MigrateAsync()` cədvəli yaradır.

## Özünü yoxla

1. Entity və cədvəl arasında fərq nədir?
2. Pul niyə `decimal(18,2)`-dir, `double` deyil?
3. `RowVersion` nə üçündür? Onsuz iki eyni vaxtlı köçürmə nə edə bilərdi?
4. Cashback kartının "hər istifadəçidə bir dənə" qaydası kodda yoxdur, harada qorunur?
5. `(Reference, Type)` unikal indeksi hansı real problemi həll edir?
6. `Migration` nədir? `MigrateAsync()` başlanğıcda nə edir?
7. `DeleteBehavior.Restrict` və `Cascade` fərqi?
