# 96. Home "Trust indicators": bazadan canlı rəqəmlər

Fayllar (API): `Service/Helpers/DTOs/HomeStats/HomeStatsDto.cs`, `Repository/Repositories/Interfaces/IHomeStatsRepository.cs`, `HomeStatsRepository.cs`, `Service/Services/Interfaces/IHomeStatsService.cs`, `HomeStatsService.cs`, `Controllers/Client/HomeController.cs` (`GET api/home/stats`), iki `DependencyInjection.cs`.
Fayllar (MVC): `ViewModels/HomeStats/HomeStatsUIVM.cs`, `ViewComponents/HomeStatsViewComponent.cs`, `Views/Shared/Components/HomeStats/Default.cshtml`, `Views/Home/Index.cshtml`, `wwwroot/js/home.js`.

## 1. Nəzəriyyə
- Əvvəl bu bölmədə 97%, 98%, 99% uydurma rəqəmlər (uptime, retention, AML/KYC) var idi. Müdafiədə "bu rəqəm haradan gəlir?" sualının cavabı olmazdı. İndi hər rəqəm bazadan hesablanır.
- **Aqreqat sorğu** (say və cəm): bütün sətirləri gətirmək əvəzinə bazanın özü `COUNT` və `SUM` hesablayır. `AsNoTracking` lazım olmayan izləməni söndürür.
- **Açıq endpoint:** rəqəmlər sirr deyil, Home-u giriş etməyən də görür, ona görə `GET api/home/stats` `[Authorize]`-suzdur. Yalnız ümumi say qaytarır, heç bir şəxsi məlumat yox.

## 2. Rəqəmlər
| Rəqəm | Sorğu |
|---|---|
| Müştərilər | `AspNetUserRoles` ilə `AspNetRoles` birləşdirilir, `Role = Customer` olanlar sayılır (işçilər sayılmır) |
| Köçürmələr | `Transactions`-da `Type = TransferOut` sayı. Bir köçürmənin göndərən sətri bir dəfə sayılır; alan sətri (`TransferIn`) və komissiya (`Commission`) sayılmır |
| Köçürülən məbləğ | həmin sətirlərin `Amount` cəmi (komissiya daxil deyil). Sətir yoxdursa `SumAsync` `null` qaytarır, `0`-a çevrilir |
| Kartlar | `Cards` sayı (bütün növlər) |

`HomeStatsService` dörd sorğunu **ardıcıl** icra edir: eyni `DbContext`-də paralel sorğuya icazə yoxdur.

## 3. MVC
- `HomeStatsViewComponent` API-dən `api/home/stats` çəkir. API işləməsə (`HttpRequestException`) **bölmə gizlənir**, rəqəm uydurulmur və Home-un qalanı açılır.
- Dairə dekorativdir (`data-percent="100"`, həmişə tam dolur), içindəki rəqəm `home.js`-də GSAP ilə 0-dan real rəqəmə qədər sayır. Mində birlər vergüllə ayrılır (`toLocaleString("en-US")`).
- Köçürmə kartının mətni ümumi məbləği göstərir: "Card transfers completed, moving 4,698.00 AZN in total."
- Boş bazada rəqəmlər 0 görünür. Təqdimatdan əvvəl bir neçə nümunə köçürmə etmək lazımdır.

## Özünü yoxla
1. Niyə bu endpoint açıqdır və hansı məlumatı qaytarmır?
2. Niyə köçürmə sayı `TransferOut` sətirləri ilə hesablanır, bütün köçürmə sətirləri ilə yox?
3. API işləməyəndə niyə bölməni gizlədirik, 0 yazmırıq?
4. `SumAsync(t => (decimal?)t.Amount)` yazılışında `decimal?` niyə lazımdır?

## 4. Dizayn düzəlişi (13 oktyabr 2026)
- Dairənin içindəki rəqəm böyüdü (32 px, qalın, `tabular-nums`). Rəqəm uzun olanda şrift kiçilir: 1,000 və yuxarı `ring-long` (23 px), 100,000 və yuxarı `ring-xlong` (17 px). Sinfi `Default.cshtml`-dəki `Size()` seçir.
- Başlıq və mətn bir `.stat-text` blokundadır, ona görə aralarında boşluq qalmır (əvvəl dairə iki sıranı tuturdu).
- AZN məbləği `.stat-money` ilə limon rəngində vurğulanır.
- **Razor tələsi:** `class="ring@Size(...)"` yazanda Razor `ring@Size` hissəsini email ünvanı sanır və ifadəni icra etmir. Düzgün yazılış: `class="ring @Size(...)"` (arada boşluq). Səhv olanda `.ring` sinfi pozulur və say animasiyası işləmir.
