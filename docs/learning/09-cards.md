# 09. Kartlar

Fayllar: `Domain/Entities/Card.cs`, `CardTierConfig.cs`, `Repository/Repositories/CardRepository.cs`, `Service/Services/CardService.cs`, `Controllers/Client/CardsController.cs`; MVC: `Controllers/AppController.cs`, `Views/App/*`.

## 1. Nəzəriyyə: bank kartı modeli bizim layihədə

- Hər müştərinin **bir və ya bir neçə** kartı var. Kart: nömrə, bitmə tarixi, balans, növ (tier), bloklu/açıq.
- **Növ (tier)** kartın qaydalarını müəyyən edir: açılış haqqı, cashback faizi, köçürmə limiti, komissiya.

| Tier | Haqq | Cashback % | Köçürmə limiti | Komissiya % | Qeyd |
|---|---|---|---|---|---|
| Cashback | 0 | 0 | 0 | 0 | hər müştəridə **tək**, pulu xaricdən yatırmaq olmur, yalnız öz kartlarına köçürə bilər, ona köçürmə olmur |
| Standard | 10 | 0.5 | 500 | 1 | |
| Silver | 15 | 1 | 2000 | 0.6 | |
| Gold | 40 | 1.5 | 10000 | 0.3 | |

(Bunlar başlanğıc dəyərlərdir, admin dəyişə bilər.)

- **Komissiya nə vaxt tutulur?** Yalnız köçürmə məbləği kartın `TransferLimit`-ini aşanda: `(amount - limit) * CommissionPercent / 100`. (Bax `10`.)
- **Jurnal (ledger)**: balansı dəyişən hər əməliyyat `Transaction` sətri kimi yazılır. Balans sadəcə rəqəm deyil, tarixçə ilə izah edilə bilməlidir.

## 2. Kart nömrəsi
`CardNumberGenerator.Generate()`: Caspian BIN-i `96301020` + 7 təsadüfi rəqəm + Luhn rəqəmi = 16 rəqəm. Unikallıq: `CardService.NewCardAsync` nömrəni bazada yoxlayır (10 cəhdə qədər), üstəlik `CardNumber` üzərində unikal indeks var.

Bitmə tarixi: yaradılma tarixindən 4 il sonra, o ayın son günü (`ValidYears = 4`).

Müştəriyə kartın tam nömrəsi **göstərilmir**; yalnız son 4 rəqəm (`Last4`) və `•••• 1234` etiketi. Tam nömrə yalnız kartın öz detal səhifəsində (`CardDetailDto.CardNumber`) göstərilir.

## 3. Axınlar

### 3.1 İlk kartlar (pulsuz)
`POST api/cards/initial` body: `{ fin }`.
`CardService.CreateInitialCardsAsync`:
1. Validator (FİN düz 7 hərf və ya rəqəm: `^[A-Za-z0-9]{7}$`).
2. İstifadəçi tapılır.
3. Artıq kartı varsa → xəta ("Your first cards have already been created.").
4. FİN başqa hesabda varsa → xəta (`FinInUseAsync`).
5. `Standard` və `Cashback` qaydaları götürülür.
6. FİN istifadəçiyə yazılır (`FinKod`; bazada unikal).
7. İki kart yaradılır (**pulsuz**, balans 0) və **bir** `SaveChanges` ilə yazılır (`AddRangeAsync`: ya ikisi də, ya heç biri).
8. İki sorğu eyni anda gəlsə, unikal indeks (`UX_Cards_UserId_Cashback`) ikincini rədd edir (`DbUpdateException` → `false`).

### 3.2 Ödənişli əlavə kart
`POST api/cards` body: `{ tier, fundingCardId }`.
- Haqq (`IssueFee`) **seçilən kartdan** çıxılır.
- `CardRepository.AddPaidCardAsync` bir tranzaksiyada: balansı təzədən oxuyur, bloklu/yetərsiz balansı yoxlayır, haqqı çıxır, yeni kartı yazır, `CardFee` jurnal sətrini yazır. `RowVersion` pozulsa `Conflict`.
- Cashback növü bu yolla açılmır: `AddCardDtoValidator` yalnız Standard, Silver, Gold qəbul edir ("Cashback kartı yalnız ilk kartlarla birlikdə açılır").

### 3.3 Balans artırma (top-up)
`POST api/cards/{id}/top-up` body: `{ amount, sourceCardNumber, expiry, cvv }` (rate limit: `transfer`).

Pul **yalnız həqiqi kartdan** gəlir (2026-10-24-dən; əvvəl istənilən Luhn-dan keçən nömrə qəbul olunurdu):
1. Nömrə düzgün formatdadır (Luhn), bitmə tarixi keçməyib, CVV 3-4 rəqəm: `TopUpDtoValidator`.
2. **Caspian kartıdırsa** (`GetByNumberAsync`): həmin kartdan pul **çıxır**, hədəf karta düşür (`CardRepository.TopUpFromCardAsync`, bir `SaveChanges`, `RowVersion`). Şərtlər: eyni kart olmaz; bitmə tarixi kartdakı ilə eyni olmalıdır ("The card details are not correct."; CVV saxlanmır, yalnız formatı yoxlanır); kart bloklu olmamalıdır; balans çatmalıdır ("That card doesn't have enough balance for this payment."). Cashback kartı da mənbə ola bilər. Göndərən kartda `TopUpOut`, hədəf kartda `TopUp` qeydi yazılır (eyni `TU-...` istinadı). Mənbə kartın sahibinə "Money left your card" bildirişi gedir.
3. **Başqa bankın kartıdırsa** (`ExternalCards`, `IBankDirectoryRepository.FindExternalCardAsync`): pul heç yerdən çıxmır (xarici kartın balansı bizdə yoxdur), yalnız hədəf karta düşür. Jurnala yalnız son 4 rəqəm yazılır.
4. İkisi də deyilsə: "We couldn't find a card with that number."

Hazır kartlar `Repository/Data/Seed/topup-cards.sql`-dədir: demo xarici kart `4111 1111 1111 1111` və Rauf Quliyevin Gold kartı `9630 1020 7777 7772` (10/30, 100 000 AZN). Yeni `TransactionType.TopUpOut` enumun **sonunda** dayanır (bazada rəqəm kimi saxlanır, ortaya əlavə etmək köhnə sətirləri pozardı).
### 3.4 Kartı bloklama və blokdan çıxarma (emailə gələn OTP kodu ilə)
Hər iki əməliyyat **iki addımdır**: əvvəl kod hesabın emailinə göndərilir, sonra istifadəçi kodu yazır.

| Addım | Endpoint | Body |
|---|---|---|
| Bloklamaq üçün kod göndər | `POST api/cards/{id}/block/code` | boş |
| Kodla blokla | `POST api/cards/{id}/block` | `{ code }` |
| Blokdan çıxarmaq üçün kod göndər | `POST api/cards/{id}/unblock/code` | boş |
| Kodla blokdan çıxar | `POST api/cards/{id}/unblock` | `{ code }` |

Hamısı `auth` rate limit-lidir (email göndərir və kod yoxlayır).

**Niyə parol yox, kod?** Kart itiriləndə və ya şübhə olanda parolu yazmaq əvəzinə emailə sahib olmağı sübut etmək həm daha təhlükəsizdir, həm də blokdan çıxarmanı da eyni üsulla etməyə imkan verir. Parol yoxlaması yanlış cəhd sayğacını artırırdı; indi bu işi OTP-nin öz cəhd limiti (5) görür.

**`CardService` metodları:** `SendBlockCodeAsync`, `BlockAsync`, `SendUnblockCodeAsync`, `UnblockAsync`.
1. Validator (`BlockCardDtoValidator` / `UnblockCardDtoValidator`: kod 6 rəqəm).
2. Kart bu istifadəçiyə aiddir? Başqasının kartı üçün `404`.
3. Vəziyyət uyğundur? Artıq bloklu karta blok kodu verilmir ("already blocked"); blokda olmayan karta blokdan çıxarma kodu verilmir ("not blocked").
4. Blokdan çıxarmada `IsRestricted` hesab rədd edilir.
5. `IOtpService.SendCodeAsync(email, purpose, subject, intro)` kodu göndərir; `ConfirmCodeAsync(email, purpose, code)` yoxlayır.
6. Uğurlu olarsa `IsBlocked = true/false`.

**OTP "məqsədi" (`Purpose`):** `EmailOtp` cədvəlinə `Purpose` sütunu əlavə olundu (migration `AddedPurposeToEmailOtp`, default `Register`). Sabitlər `Domain/Constants/OtpPurposes.cs`:
- `Register`: qeydiyyat kodu.
- `CardBlock:{cardId}` və `CardUnblock:{cardId}`: kart üçün kod.

Kodun hash-i məqsədə də bağlıdır. Nəticə: a) bloklama kodu başqa kartı bloklaya bilmir, b) bloklama kodu blokdan çıxara bilmir, c) qeydiyyat kodu heç birinə yaramır. Köhnə qeydiyyat kodlarının hash adı dəyişmədiyi üçün onlar da etibarlı qalır. Repository metodları (`GetLatestActiveAsync`, `GetLatestAsync`, `InvalidateActiveAsync`) artıq `purpose` parametri alır.

**`OtpService` refaktoru:** qeydiyyat və kart kodu eyni iki xüsusi metoddan istifadə edir: `IssueCodeAsync` (60 saniyə gözləmə, köhnə kodları ləğv et, hash saxla, email göndər) və `CheckCodeAsync` (5 cəhd limiti, `FixedTimeEquals`; qeydiyyatda təsdiq tokeni də yaradır, kartda yalnız kodu "istifadə olunmuş" sayır).

**Cavab:** kod göndəriləndə `CardCodeSentDto { maskedEmail, devCode }`. `devCode` yalnız Development-də (SMTP qurulmayıb) qayıdır. Gözləmə müddətində `400` + "Please wait N seconds before requesting a new code.".

> Bloklu kartla: top-up, köçürmə (göndərən və ya alan), yeni kart haqqı ödəmə mümkün deyil. Gələcəkdə bankın tərəfindən blok (şübhəli əməliyyat) üçün ayrıca `BlockedByBank` bayrağı lazım olacaq ki, müştəri onu özü açmasın.


### 3.5 Kartların siyahısı və detalı
- `GET api/cards` → `CardDto[]` (Id, Tier, Last4, Expiry `MM/yy`, Balance, IsBlocked, HolderName, DesignImage).
- `GET api/cards/{id}` → `CardDetailDto` (+ tam nömrə + tier qaydaları).
- `GET api/cards/{id}/transactions?take=20` → son 20 əməliyyat (yalnız kartın sahibinə).
- `GET api/cards/activity?take=8` → **bütün kartların** son əməliyyatları (`CardLabel` ilə). Route-da `activity` sabit sözdür, `{id:int}` ilə qarışmır.

## 4. MVC tərəfi (`AppController`)
| Action | URL | API çağırışı |
|---|---|---|
| `Index` | `/App` | `api/cards`, `api/cards/activity?take=8` |
| `AddCard` | `/App/AddCard` | `api/cards`, `api/card-tiers` |
| `CreateFirstCards` | POST | `api/cards/initial` |
| `AddTierCard` | POST | `api/cards` |
| `Card` | `/App/Card/{id}` | `api/cards/{id}`, `api/cards/{id}/transactions?take=20` |
| `TopUp` | POST | `api/cards/{id}/top-up` |
| `SendBlockCode`, `BlockCard` | POST | `api/cards/{id}/block/code`, `api/cards/{id}/block` |
| `SendUnblockCode`, `UnblockCard` | POST | `api/cards/{id}/unblock/code`, `api/cards/{id}/unblock` |

Ortaq şablon (hər action):
1. API-ni çağır (`GetAsync`/`PostAsync`).
2. `Unauthorized` (token bitib) → `SessionExpiredAsync()`: cookie bağlanır, Welcome-ə yönləndirilir.
3. Uğurlu → `TempData["Success"] = "..."` və redirect (PRG: səhifəni yeniləmək POST-u təkrar etməsin).
4. Xəta → `TempData["CardError"]` / `["TopUpError"]` / `["BlockError"]` (bir növ pəncərə xəta ilə yenidən açılır).

`Absolutize(cards)`: kartların `DesignImage` yolu API ünvanı ilə tam URL-ə çevrilir.

## 5. Real həyatda qeydlər
- Real bank kart nömrələrini və CVV-ni **heç vaxt** saxlamır (PCI DSS). Biz xarici kartı yalnız yoxlayırıq.
- Kart nömrəsi tokenləşdirilir (real sistemdə). Burada sadə saxlanılır, çünki bu təhsil layihəsidir.

## Özünü yoxla

1. Hər müştəri ilk dəfə nə qədər kart alır və niyə pulsuz?
2. Cashback kartının "tək olmalıdır" qaydası harada qorunur (kod və baza)?
3. Ödənişli kart açılanda haqq harada çıxılır? Niyə tranzaksiya lazımdır?
4. Top-up-da kart Caspian kartıdırsa pul haradan çıxır, xarici kartdırsa niyə çıxmır? Məlumatı saxlanılırmı?
5. Kartı bloklamaq üçün niyə emailə gələn kod istifadə olunur? Bloklama kodu başqa kartı və ya blokdan çıxarmanı niyə aça bilmir (`Purpose`)?
6. `GetByIdForUserAsync` olmasa hansı təhlükə yaranardı?
7. PRG nədir və MVC-də niyə istifadə olunur?
