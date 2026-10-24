# 107. Jurnal (Serilog)

## Nə üçündür
Xəta olanda və ya "bu əməliyyat niyə belə oldu?" sualı yaranda nəyin nə vaxt baş verdiyini fayldan oxumaq üçün. API və MVC hər biri öz qovluğunda jurnal faylı yazır.

## Harada yazılır
| Layihə | Fayl |
|---|---|
| API | `CaspianBank-API-FinalProject/logs/api-YYYYMMDD.log` |
| MVC | `CaspianBank-MVC-FinalProject/logs/mvc-YYYYMMDD.log` |

- Gündə bir fayl, **14 gün** saxlanılır, köhnələri avtomatik silinir.
- `logs/` qovluğu repoda `.gitkeep` ilə qalır (klon edəndə qovluq görünür), amma içindəki `.log` faylları `.gitignore`-dadır.
- Eyni mətn konsola da çıxır (Visual Studio-nun Output pəncərəsi).

## Necə qurulub
1. Paket: `Serilog.AspNetCore` 8.0.3 (konsol və fayl yazıcıları onun içindədir).
2. `Program.cs`: `builder.Host.UseSerilog(...)`. Səviyyələr `appsettings.json`-dakı `"Serilog"` bölməsindən oxunur (`ReadFrom.Configuration`), fayl yazıcısı kodda qurulub ki yol həmişə layihə qovluğunun içində olsun (`ContentRootPath/logs`).
3. `app.UseSerilogRequestLogging()`: hər sorğu bir sətir (metod, yol, status, müddət). Sətrə `UserId` əlavə olunur (giriş edənin id-si, girişsizdə `anonymous`). Sorğu sətri (`?a=b`) yazılmır.
4. Səviyyələr: ümumi `Information`; `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `System` yalnız `Warning` və yuxarı (yoxsa jurnal SQL sorğularından boğulur).

Sətir formatı:
```
2026-10-24 16:49:43.134 [INF] Transfer TR-1A2B3C4D5E6F done: 120.00 AZN, commission 0.00, user ... {}
```

## Nə yazılır
- Hər sorğu (yuxarıdakı bir sətir).
- `ExceptionHandlingMiddleware`: gözlənilməz xəta `Error` səviyyəsində, yığın izi ilə (istifadəçiyə yalnız ümumi mətn gedir).
- Pul əməliyyatları (`Information`): köçürmə (`TransferService`), top-up (`CardService`), kredit qərarları (`LoanReviewService`: birinci təsdiq, son təsdiq, rədd).
- Mövcud xəbərdarlıqlar: email göndərilə bilmədi, bildiriş yazıla bilmədi, tərcümələr alınmadı, fon işləri.

## Nə **yazılmır** (qayda)
Parol, JWT, OTP kodu, tam kart nömrəsi, CVV. Yeni jurnal sətri yazanda yalnız id, istinad (`Reference`), məbləğ və nəticəni yaz. Kartı son 4 rəqəmlə göstər.

## Yeni jurnal sətri necə əlavə olunur
```csharp
private readonly ILogger<MyService> _logger;           // konstruktorda alınır (DI avtomatik verir)
_logger.LogInformation("Deposit {Reference} opened: {Amount} AZN, user {UserId}", reference, amount, userId);
```
`{Reference}` kimi adlandırılmış yerlər **struktur** saxlayır: mətnə qatmaq (`$"..."`) əvəzinə həmişə belə yaz.

## Tələlər
- Fayl yazıcısı `shared: true` ilədir ki, iki proses (məs. Visual Studio və `dotnet run`) eyni faylı açanda xəta olmasın.
- API `bin` qovluğunda yox, **layihə qovluğunda** `logs/` yazır (`ContentRootPath`). Fərqli yerdən işlədəndə (məs. başqa `--contentRoot`) fayl orada yaranır.

## Özünü yoxla
1. Niyə `Microsoft.EntityFrameworkCore` səviyyəsi `Warning`-dir?
2. Niyə mesaj şablonunda `{Amount}` yazırıq, `$"{amount}"` yox?
3. Hansı məlumat jurnala heç vaxt yazılmamalıdır və niyə?
