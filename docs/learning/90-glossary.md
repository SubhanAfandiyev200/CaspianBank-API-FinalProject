# 90. Terminlər lüğəti

Qısa, sadə izahlar. Hər terminin yanında layihədə harada rast gəlindiyi yazılıb.

## Arxitektura
| Termin | İzah | Layihədə |
|---|---|---|
| **N-Tier** | Kodu qatlara bölmək; hər qat yalnız altındakını tanıyır | Domain / Repository / Service / API |
| **Entity** | Cədvəl sətirinin C# sinfi | `Domain/Entities` |
| **DTO** | API-yə girib-çıxan sadə məlumat sinfi | `Service/Helpers/DTOs` |
| **ViewModel** | View üçün məlumat paketi | MVC `ViewModels` |
| **Repository** | Bazaya çıxışı saxlayan sinif | `Repository/Repositories` |
| **Service** | İş qaydaları sinfi | `Service/Services` |
| **Controller** | HTTP girişi, nazik qat | API və MVC controller-lər |
| **Middleware** | Sorğu/cavab zəncirində filtr | `ExceptionHandlingMiddleware` |
| **DI (Dependency Injection)** | Asılılıqları sinfə kənardan vermək | `AddScoped<I, Impl>()` |
| **Scoped / Transient / Singleton** | DI ömür müddəti: sorğu başına / hər dəfə yeni / tək | Repository-lər Scoped |
| **Interface** | "Nə edə bilər" müqaviləsi, implementasiyadan ayrı | `IBrandService` |
| **ViewComponent** | Öz məntiqi olan View parçası | `BrandViewComponent` |
| **Area** | MVC-də controller qrupu | `Admin` |
| **Pattern: Repository** | Bazaya çıxışı bir yerdə toplamaq | `BaseRepository<T>` |
| **PRG** | POST → Redirect → GET: yeniləmə təkrar göndərməsin | MVC POST action-ları |

## Verilənlər bazası və EF Core
| Termin | İzah |
|---|---|
| **ORM** | Obyektlər ↔ cədvəllər çevirməsi (EF Core) |
| **DbContext / DbSet** | Bazaya pəncərə / cədvəl |
| **Migration** | Modeldəki dəyişikliyi bazaya tətbiq edən skript |
| **Seed (HasData)** | Başlanğıc sətirlər |
| **Primary key / Foreign key** | Unikal id / başqa cədvələ istinad |
| **Navigation property** | Əlaqəli obyektə keçid (`card.User`) |
| **Unique index** | Təkrar dəyəri qadağan edir |
| **Filtered index** | Yalnız şərtə uyğun sətirlərdə indeks |
| **RowVersion (concurrency token)** | Sətirin versiyası; paralel dəyişikliyi aşkar edir |
| **Tracking / AsNoTracking** | EF dəyişiklikləri izləyir / izləmir (oxumada sürətli) |
| **Tranzaksiya** | Ya hamısı, ya heç biri |
| **Include** | Əlaqəli məlumatı birlikdə yüklə |
| **LINQ** | C#-da sorğu sintaksisi (SQL-ə çevrilir) |
| **Precision (18,2)** | `decimal` ölçüsü: 18 rəqəm, 2 onluq |

## Təhlükəsizlik
| Termin | İzah |
|---|---|
| **Authentication / Authorization** | Kimlik / icazə |
| **JWT** | İmzalı token: header.payload.signature |
| **Claim** | Tokendə bir fakt (rol, email) |
| **Hash** | Birtərəfli çevirmə (parol, OTP) |
| **HMAC** | Gizli açarla hash |
| **Salt** | Hash-ə əlavə təsadüfi dəyər |
| **OTP** | Birdəfəlik kod |
| **Lockout** | Çoxlu uğursuz cəhddən sonra müvəqqəti blok |
| **Rate limiting** | Sorğu sayına limit |
| **CSRF** | Başqa saytdan istifadəçi adına gizli sorğu; `[ValidateAntiForgeryToken]` qoruyur |
| **XSS** | Səhifəyə zərərli skript yeridilməsi; `HttpOnly` cookie token oğurluğunu çətinləşdirir |
| **SQL injection** | Giriş vasitəsilə SQL yeritmə; EF parametrləşdirir |
| **User enumeration** | Cavab fərqi ilə hesabın mövcudluğunu öyrənmə |
| **Open redirect** | Giriş sonrası xarici saytlara yönləndirmə; `Url.IsLocalUrl` qoruyur |
| **Path traversal** | Fayl adında `../` ilə kənar yerə yazma |
| **Magic bytes** | Faylın ilk baytları: format imzası |
| **TOCTOU** | Yoxlama ilə istifadə arasında vəziyyətin dəyişməsi |

## Pul və bank
| Termin | İzah |
|---|---|
| **Ledger / Jurnal** | Hər pul hərəkətinin qeydi (`Transaction`) |
| **BalanceAfter** | Əməliyyatdan sonrakı balans |
| **Reference** | Eyni əməliyyatın sətirlərini birləşdirən kod (`TR-...`) |
| **Idempotency** | Eyni sorğu təkrar olsa nəticə dəyişmir (`RequestId`) |
| **Atomiklik** | Əməliyyat bölünməzdir |
| **Optimistic concurrency** | Paralelliyi sonradan aşkar edib təkrar cəhd |
| **Tier** | Kart növü: Cashback / Standard / Silver / Gold |
| **Komissiya** | Köçürmənin limitdən aşan hissəsinə faiz |
| **Cashback** | Alışdan geri qayıdan faiz (planlaşdırılıb) |
| **Luhn** | Kart nömrəsinin yoxlama rəqəmi alqoritmi |
| **FİN** | Azərbaycanda şəxsi identifikasiya kodu (7 simvol) |
| **Statement (çıxarış)** | Dövr üzrə açılış/bağlanış balansı + əməliyyatlar |

## Web
| Termin | İzah |
|---|---|
| **REST** | Resurslara HTTP metodları ilə müraciət |
| **JSON** | Mətn əsaslı məlumat formatı |
| **multipart/form-data** | Fayl göndərmə formatı |
| **Cookie** | Brauzerdə saxlanan kiçik məlumat |
| **TempData** | Növbəti sorğuya qədər yaşayan mesaj yaddaşı |
| **Tag Helper** | Razor-da `asp-*` atributları |
| **Razor** | HTML + C# şablon dili |
| **Swagger** | API-ni brauzerdə sınamaq və sənədləşdirmək |
| **IIFE** | JS-də özünü çağıran funksiya (qlobal çirklənmənin qarşısı) |
| **data-\* atributları** | HTML-də JS üçün açıqlayıcı məlumat |
| **GSAP** | JS animasiya kitabxanası (Home) |
