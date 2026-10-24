# 10. Köçürmə (Transfer)

Bu, layihənin **ən vacib** funksiyasıdır və müsahibədə/müdafiədə ən çox soruşulacaq hissədir. Diqqətlə oxu.

Fayllar:
- `Service/Services/TransferService.cs` (qaydalar)
- `Repository/Repositories/TransferRepository.cs` (atomik icra)
- `Repository/Results/TransferResult.cs` (`TransferOutcome`, `TransferPlan`, `TransferRecord`)
- `Controllers/Client/TransfersController.cs`
- MVC: `Controllers/TransferController.cs`, `Views/Transfer/*`, `ViewModels/Transfers/TransferVMs.cs`

## 1. Nəzəriyyə: pul köçürməsi niyə çətindir?

### 1.1 Atomiklik (ACID-in "A"-sı)
Köçürmə üç yazıdır: göndərənin balansı azalır, alanın balansı artır, jurnal yazılır. Ortada xəta (elektrik kəsilməsi, istisna) olsa, **pul itməməli və ya yoxdan yaranmamalıdır**. Ona görə hamısı **bir tranzaksiyada**: ya hamısı yazılır, ya heç biri.

### 1.2 Concurrency (eyni vaxtlılıq) və "lost update"
```
Balans = 100.
Sorğu A: oxuyur 100, 30 çıxır → yazmaq istəyir 70
Sorğu B: oxuyur 100, 50 çıxır → yazmaq istəyir 50
Hər ikisi yazarsa nəticə 50 və ya 70, olmalı idi 20.
```
Həll: **optimistic concurrency**. `Card.RowVersion` (SQL `rowversion`) hər dəyişəndə avtomatik artır. EF `UPDATE ... WHERE Id = @id AND RowVersion = @oxuduğumuz` göndərir. Bu arada başqası dəyişibsə 0 sətir təsirlənir → `DbUpdateConcurrencyException`. Biz tranzaksiyanı geri qaytarırıq, **yenidən oxuyub** (təzə balansla) təkrar cəhd edirik.

(Alternativ: pessimistic lock, yəni sətri kilidləmək. Optimistic oxuma-ağır sistemlərdə daha sürətlidir.)

### 1.3 Idempotency (təkrar sorğu)
İstifadəçi düyməyə iki dəfə klikləyir, səhifəni yeniləyir, şəbəkə təkrar göndərir. Pul **iki dəfə** köçməməlidir.
Həll: forma açılanda **`RequestId` (GUID)** yaranır. Eyni `RequestId` ilə ikinci sorğu gələndə əvvəlki nəticə qaytarılır, pul təkrar köçmür. Baza səviyyəsində də qorunur: `Transaction`-da unikal indeks `(Reference, Type)`.

### 1.4 Jurnal (ledger) və qəbz
Hər pul hərəkəti `Transaction` sətridir. Bir köçürmə **eyni `Reference`** ilə bağlı üç sətirdir:
- `TransferOut` (göndərən, çıxış)
- `Commission` (göndərən, çıxış, yalnız komissiya varsa)
- `TransferIn` (alan, giriş)

Hər sətirdə `BalanceAfter` var: o anda kartın balansı. Buna görə çıxarış balansı sonradan asanlıqla hesablana bilir.

## 2. İş qaydaları

| Qayda | Harada |
|---|---|
| Məbləğ > 0, ≤ 100 000, ≤ 2 onluq | `TransferDtoValidator` |
| Alıcı **ya** öz kart, **ya** nömrə (ikisi birlikdə yox) | validator (XOR) |
| Hesab dondurulubsa (`IsRestricted`) olmaz | `ResolveAsync` |
| Göndərən kart **mənimdir** və bloklu deyil | `GetByIdForUserAsync` |
| Alıcı kart bloklu olmamalıdır | `ResolveAsync` + repository |
| Göndərən = alıcı olmaz | `ResolveAsync` |
| **Cashback kartına heç kim köçürə bilməz** | `ResolveAsync` + repository |
| **Cashback kartından yalnız öz kartlarına** köçürmək olar | `ResolveAsync` + repository |
| Balans ≥ məbləğ + komissiya | `ResolveAsync` + repository (təzə balansla) |
| Komissiya: yalnız kartın **aylıq** pulsuz limitini (`TransferLimit`) keçən hissəyə `CommissionPercent`% | `ResolveAsync` + `TransferFees` |

**Komissiya nümunəsi** (Standard: aylıq pulsuz limit 500, komissiya 1%): bu ay artıq 400 göndərilib, indi 300 göndərilir. Limitə 100 qalıb, 200 limiti keçir: komissiya `200 * 1 / 100 = 2.00` AZN. Göndərənin balansından `302` çıxılır, alan `300` alır. Yuvarlama `MidpointRounding.AwayFromZero`, 2 onluq.

**Komissiya öz kartlarına köçürmədə də tətbiq olunur.** Komissiya **göndərən kartın** tier-inə görədir.

## 3. İki addım: preview və transfer

İstifadəçi əvvəl **yoxlama** (preview) görür: komissiya nə qədərdir, pul kimə gedir. Sonra təsdiq edir.

```
POST /api/transfers/preview   →  heç nə yazmır. Qaydaları yoxlayır, komissiyanı və alıcının gizlədilmiş adını qaytarır.
POST /api/transfers           →  həqiqətən icra edir.
```
Alıcının tam adı **göstərilmir**: `MaskedName` → "Nurlan Efendiyev" → "Nurlan E.". Alıcı kartın nömrəsi də yalnız son 4 rəqəm: `•••• 1234`.

İki endpointin ortaq hissəsi `TransferService.ResolveAsync` metodudur: validator, kartları tapmaq, qaydalar, komissiya. Nəticəsi `Plan` rekordu və ya xətalar.

## 4. `TransferService.TransferAsync` addım-addım

```csharp
// 1. Idempotency
var requestId = Guid.TryParse(model.RequestId, out var parsed) ? parsed : Guid.NewGuid();
var reference = "TR-" + requestId.ToString("N")[..12].ToUpperInvariant();      // məs. TR-3F9A12BC7E01
if (!string.IsNullOrEmpty(model.RequestId))
{
    var existing = await ExistingReceiptAsync(reference, userId);
    if (existing is not null) return ServiceResult<TransferReceiptDto>.Ok(existing);   // əvvəlki qəbz
}

// 2. Bütün yoxlamalar və komissiya
var (plan, errors) = await ResolveAsync(userId, model);
if (plan is null) return ServiceResult<TransferReceiptDto>.Fail(errors);

// 3. Təsvirlər (jurnalda görünən mətn)
descriptionOut = isOwn ? "Transfer to your Gold card •••• 1234" : "Transfer to •••• 1234"
descriptionIn  = isOwn ? "Transfer from your Standard card •••• 5678" : "Transfer from Nurlan E."

// 4. Repository-yə plan ötürülür: həqiqi icra
var result = await _transfers.ExecuteAsync(new TransferPlan(userId, fromId, toId, amount, commission, reference, note, descOut, descIn));

// 5. Nəticəni mesaja çevir
switch (result.Outcome) { Success → qəbz; InsufficientFunds, SourceBlocked, DestinationBlocked, NotAllowed → xəta mesajı;
                           Duplicate → əvvəlki qəbzi tap və qaytar; Conflict → "balance changed, try again" }
```

**Niyə servisdə yoxlayıb repository-də yenə yoxlayırıq?** Servis yoxlayan an ilə repository yazan an arasında başqa sorğu kartın balansını dəyişə bilər (**TOCTOU**: time-of-check to time-of-use). Servis istifadəçiyə yaxşı mesaj vermək üçün yoxlayır; Repository isə **tranzaksiya daxilində təzə məlumatla** yenə yoxlayır, çünki pul məsələsində son sözü o deyir.

## 5. `TransferRepository.ExecuteAsync`

```csharp
for (var attempt = 0; attempt < MaxAttempts /*8*/; attempt++)
{
    _dbContext.ChangeTracker.Clear();                       // köhnə məlumatı unut
    await using var transaction = await _dbContext.Database.BeginTransactionAsync();

    var source      = await Cards.FirstOrDefaultAsync(c => c.Id == plan.FromCardId && c.UserId == plan.UserId);
    var destination = await Cards.FirstOrDefaultAsync(c => c.Id == plan.ToCardId);

    // təzə məlumatla yenidən yoxlamalar: tapılmadı / bloklu / Cashback qaydaları / balans
    ...

    source.Balance      -= plan.Amount;     Transactions.Add(TransferOut)
    if (commission > 0) { source.Balance -= commission;  Transactions.Add(Commission) }
    destination.Balance += plan.Amount;     Transactions.Add(TransferIn)

    try { await SaveChangesAsync(); await transaction.CommitAsync(); return Success(source.Balance); }
    catch (DbUpdateConcurrencyException) { rollback; kiçik təsadüfi gecikmə (10–60 ms); növbəti cəhd }
    catch (DbUpdateException)            { rollback; return Duplicate; }   // (Reference, Type) unikal indeksi
}
return Conflict;   // 8 cəhd bitdi
```

Mühüm detallar:
- `ChangeTracker.Clear()`: hər cəhddə EF-in yaddaşı təmizlənir, nəticədə kartlar bazadan **yenidən** oxunur (köhnə `RowVersion` yox).
- Təsadüfi gecikmə: eyni anda gələn sorğular növbə ilə keçsin (hamısı eyni anda təkrarlayıb yenə toqquşmasın).
- `DbUpdateException` (concurrency olmayan): unikal indeks pozulub, yəni bu `Reference` artıq yazılıb → `Duplicate`.
- Yoxlamalar `return` ilə çıxanda `await using` tranzaksiyanı avtomatik geri qaytarır (commit olunmayıb).

## 6. Nəticə enum-ları
`TransferOutcome`: `Success, SourceNotFound, DestinationNotFound, SourceBlocked, DestinationBlocked, NotAllowed, InsufficientFunds, Duplicate, Conflict`.

Repository istifadəçiyə mesaj bilmir, yalnız nəticə kodu qaytarır; mesajı Service seçir (qatların məsuliyyəti ayrıdır).

## 7. Qəbzin bərpası (`FindAsync`)
`Duplicate` olanda və ya təkrar sorğuda orijinal qəbz **bazadan** qurulur: eyni `Reference` ilə olan sətirlər tapılır (`TransferOut`, `TransferIn`, `Commission`). Yalnız **göndərənin öz** köçürməsi qaytarılır (`r.Card.UserId == userId` şərti), başqasının `Reference`-i ilə məlumat çıxmasın.

## 8. MVC axını (3 səhifə)

```
GET  /App/Transfer           → forma (kartlar + tier qaydaları canlı komissiya hesabı üçün)
POST /App/Transfer/Review    → API preview çağırılır → "Review" səhifəsi (kartlar gizli sahələrdə saxlanır)
POST /App/Transfer/Edit      → "Back": formaya qayıdır, daxil edilənlər qalır
POST /App/Transfer/Confirm   → API transfer çağırılır → TempData-ya qəbz → redirect /App/Transfer/Done
GET  /App/Transfer/Done      → qəbz səhifəsi (TempData bir dəfə oxunur)
```
- **PRG** (Post-Redirect-Get): `Confirm` POST-dan sonra redirect olur, ona görə qəbz səhifəsini yeniləmək köçürməni təkrar göndərmir.
- `RequestId`: `TransferFormVM`-də `Guid.NewGuid().ToString()`; formanın hər açılışında yeni olur, Review və Confirm-də gizli sahə kimi qalır.
- `ToRequest`: məbləğ "50,5" və "50.5" ikisini də qəbul edir (server mədəniyyətindən asılı olmasın): vergülü nöqtəyə çevirir, `InvariantCulture` ilə parse edir.
- Alıcı rejimi: `own` (öz kartlarım) və `other` (başqa müştəri, kart nömrəsi ilə).
- Hər şey `[Authorize]`; `Unauthorized` olsa `SessionExpiredAsync()`.

JavaScript tərəfi (`wwwroot/js/app-cards.js`, transfer blokunda): canlı komissiya göstəricisi (`Tiers` datasından), kart seçimi, məbləğ sahəsi (prefiks/status), qeyd sayğacı, öz kartı yoxdursa boş vəziyyət (`#own-empty`).


## 8a. Başqa banka köçürmə və aylıq pulsuz limit
- Alıcı kart nömrəsi əvvəl Caspian kartları arasında axtarılır (`GetByNumberAsync`). Tapılmasa `ExternalCards` cədvəlində (başqa bankların saxta kartları, `IBankDirectoryRepository.FindExternalCardAsync`) axtarılır. Heç yerdə yoxdursa "We couldn't find a card with that number."
- Başqa banka köçürmədə pul alıcıya düşmür (onun balansı bizdə yoxdur): göndərən kartdan çıxır, jurnala `Transfer to <Bank> •••• 1234` yazılır, qəbzdə bankın adı və alıcının qısaldılmış adı görünür. Bir dəfəyə göndərilə bilən ən çox məbləğ `OwnBank.MaxInterbankAmount`-dur.
- **Komissiya** artıq tək köçürmənin məbləğinə yox, kartın **aylıq pulsuz limitini** keçən hissəyə tutulur: `TransferFees.ChargeableAmount(used, amount, limit)`; `used` bu ay həmin kartdan göndərilənlərin cəmidir (`TransferRepository.SumSentAsync`, Bakı ayı). Komissiya faizi tier-dən gəlir. Öz kartları arasında da, başqa banka da eynidir. (Yuxarıdakı 2-ci bölmədəki "amount > TransferLimit" qaydası bununla əvəz olunub.)
- Alıcı başqa Caspian müştərisidirsə ona "Money received" bildirişi gedir. Öz kartları arasında və başqa banka bildiriş yoxdur.
- Hər köçürmə `SecurityMonitor`-a verilir (böyük məbləğ, qısa müddətdə çox köçürmə, günlük cəm, yeni hesabdan çıxış). Siqnal yaradır, heç nəyi bloklamır.
- **Top-up köçürmə deyil:** kartın balansını başqa kartdan artırmaq `TransactionType.TopUp` / `TopUpOut` ilə yazılır və aylıq pulsuz limiti və təhlükəsizlik sayğaclarını dəyişmir (bax `09-cards.md` §3.3).
## 9. Köçürmə testləri üçün düşüncə (nə yoxlamaq olar?)
1. Normal köçürmə: göndərənin balansı azalır, alanın artır, 2 jurnal sətri.
2. Limitdən çox: komissiya hesablanır, 3 jurnal sətri.
3. Balans çatmır → xəta, heç nə dəyişmir.
4. Eyni `RequestId` iki dəfə → pul bir dəfə köçür, ikinci cavab eyni qəbzdir.
5. Cashback kartına köçürmə → rədd. Cashback → başqa müştəriyə → rədd.
6. Bloklu kart → rədd.
7. Başqasının `Reference`-i ilə qəbz istəmək → tapılmır.
8. İki paralel köçürmə eyni kartdan → ikisi də nəzərə alınır (retry), balans mənfi olmur.

## Özünü yoxla

1. Köçürmənin atomik olması nə deməkdir və hansı mexanizm təmin edir?
2. `RowVersion` nədir? İki paralel köçürmə zamanı nə baş verir?
3. Idempotency nədir? `RequestId` və `(Reference, Type)` indeksi birlikdə necə işləyir?
4. Servis yoxlayır, repository yenidən yoxlayır, niyə?
5. 700 AZN Standard kartdan göndərilir: komissiya və göndərənin çıxan məbləği nədir?
6. Preview və Transfer arasındakı fərq? Niyə ayrıdırlar?
7. Alıcının adı niyə maskalanır?
8. Niyə `ChangeTracker.Clear()` hər cəhddə çağırılır?
9. Qəbz səhifəsi niyə PRG ilə göstərilir?
