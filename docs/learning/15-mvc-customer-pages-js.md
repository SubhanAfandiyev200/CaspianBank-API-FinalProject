# 15. Müştəri səhifələri, CSS və JavaScript

MVC repo: `Views/App`, `Views/Transfer`, `Views/History`, `wwwroot/js`, `wwwroot/css`.

## 1. Nəzəriyyə (brauzer tərəfi)

### 1.1 Niyə JavaScript lazımdır?
Server HTML göndərir; JavaScript səhifəni **canlı** edir: açılan siyahılar, kart dəsti, canlı komissiya, tarix seçici, təsdiq pəncərəsi. **Qayda:** JS yalnız rahatlıq üçündür, əsas məntiq (yoxlama, pul) serverdə qalır. JS söndürülsə də forma işləməlidir (təhlükəsizlik JS-ə güvənmir).

### 1.2 `data-*` atributları ilə davranış bağlamaq
HTML-də elementə `data-open-modal`, `data-card-pick`, `data-select` yazılır; JS bu atributları axtarıb davranış əlavə edir. Bunun üstünlüyü: JS faylı "hansı səhifədədir" bilmir, sadəcə "bu atribut varsa et".

### 1.3 Progressive enhancement
`input[type=date]` və `<select>` standart elementlərdir; JS onları öz dizaynımıza uyğun "təkər tarix" və "custom select"-ə çevirir. JS yüklənməsə standart element qalır.

## 2. Səhifələr

### 2.1 `/App` (Cards)
`Views/App/Index.cshtml`, `AppIndexVM`:
- Kart dəsti (deck): kartlar üst-üstə, klik ilə seçilir.
- "Recent activity": `api/cards/activity?take=8`; hər sətir `_ActivityRow`; "View all" → History.
- Boş vəziyyət: kartı yoxdursa "AddCard" çağırışı.

### 2.2 `/App/AddCard`
İki rejim:
- `FirstTime` (kartı yoxdur): FİN yazılır, **Standard + Cashback pulsuz** açılır.
- Əks halda: növ seçilir (Standard/Silver/Gold), haqq tutulacaq kart seçilir, ödənişli kart açılır.
Növ qaydaları (`CardTierUIVM`) `api/card-tiers`-dən gəlir (komissiya, limit, cashback kartın üzərində göstərilir).

### 2.3 `/App/Card/{id}`
Kartın böyük görünüşü, tam nömrə, qaydalar, son 20 əməliyyat, düymələr:
- **Top up** modalı (xarici kart formu).
- **Block / Unblock** modalı: iki addımlı (1: "Send code" emailə kod göndərir; 2: 6 rəqəmli kodu yaz). Eyni `Views/Shared/_CardCodeModal.cshtml` partial-ı hər ikisi üçün istifadə olunur (`CardCodeModalVM`: başlıq, mətn, action adları, düymə rəngi). Bloklu kartda "Unblock card" düyməsi görünür.
- **Transfer** (göndərən kart əvvəlcədən seçilir: `/App/Transfer?from={id}`).
Modallar `data-open-modal` / `data-close-modal` ilə; xəta olarsa (`TopUpError`, `BlockError`, `UnblockError`) və ya kod göndərilibsə (`BlockSentTo`, `UnblockSentTo`) səhifə yenidən yüklənəndə modal özü açılır (`data-open-on-load`). Demo kod `TempData.Peek` ilə oxunur ki, səhv kod yazılıb səhifə yenilənəndə itməsin.

### 2.4 `/App/Transfer` (3 addım)
Bax `10-transfers.md`: forma → Review → Done. Formada:
- "From" kartı (custom card picker), "To": `own` və ya `other` tab.
- Məbləğ sahəsi (`.amount-box`, prefiks, status).
- Qeyd (140 simvol sayğacı).
- Canlı komissiya: View hər kartın seçim elementinə növün qaydalarını `data-limit` (komissiyasız limit), `data-pct` (komissiya faizi) və `data-balance` atributları kimi yazır; `app-cards.js` bu atributlardan oxuyub göstərici hesablayır (sırf göstəriş; real hesab API-dədir).
- Cashback kartı "To" siyahısına qoyulmur (`ownTargets`), çünki ona köçürmə olmur.
- Öz kartın yoxdursa "boş vəziyyət" (`#own-empty`).

### 2.5 `/App/History`
Bax `11`. `.page-activity` dizayn sinfi altında; cədvəl (`.data-table`), pager, statement modalı.

### 2.6 Statement və qəbz
Çap üçün: `#rc-print` düyməsi ("Save as PDF") `window.print()` çağırır (`app-cards.js`); həm `Statement.cshtml`, həm də köçürmə qəbzində (`Transfer/Done.cshtml`) istifadə olunur. `@media print` CSS-i çapda yalnız çıxarışı/qəbzi göstərir.

## 3. JavaScript faylları

| Fayl | Hansı səhifələr | Nə edir |
|---|---|---|
| `home.js` | Home | GSAP animasiyaları, ticker, kart yelpazəsi, rəqəm sayğacları (rings) |
| `auth-fields.js` | Login/Register | telefon maskası; email-in səhifələr arasında daşınması |
| `otp-input.js` | VerifyOtp | 6 xanalı kod, avtomatik keçid, "Resend" geri sayımı |
| `wheel-date.js` | Register, History, Admin | `input[type=date]`-i təkər tarix seçiciyə çevirir (avtomatik) |
| `confirm-dialog.js` | hamısı | `data-confirm-*` olan düymələr üçün təsdiq pəncərəsi (SweetAlert tipli) |
| `file-drop.js` | Admin formaları | fayl seçmə sahəsi |
| `app-cards.js` | App, Transfer, History | menyu, kart dəsti, modallar, kopyalama, kart picker (`data-card-pick`), custom select (`data-select`), transfer məntiqi, tarixçə period düymələri |
| `admin.js` | Admin | mobil menyu, bildiriş zəngi |
| `wwwroot/admin/js/admin-*.js` | Admin Bank bölməsi (statik) | demo davranışı: hələ API-yə qoşulmayıb |

### 3.1 `app-cards.js` quruluşu
Bir böyük IIFE içində bloklar; hər blok yalnız lazımi element səhifədə varsa işləyir:
```js
(function () {
  // mobil menyu
  // kart dəsti (deck)
  // modallar (data-open-modal / data-close-modal)
  // kart picker: select[data-card-pick] (data-all = "All cards" seçimi)
  // custom select: select[data-select]  → mountSelects()
  // transfer: canlı komissiya, öz/başqa tab, qeyd sayğacı
  // history: period düymələri, avtomatik submit
})();
```
`"use strict"` + IIFE: qlobal dəyişən çirkləndirməsin.

### 3.2 `wheel-date.js`
`input[type=date]`-i taparaq gizlədir və yerinə açılan "təkər" qurur (il/ay/gün sütunları). Dəyər orijinal inputa yazılır (forma yenə standart `yyyy-MM-dd` göndərir). `min`/`max` atributlarına hörmət edir. `_AppLayout`-da `@RenderSection("Scripts", required: false)` səhifəyə bu faylı yükləməyə imkan verir.

## 4. CSS
- `wwwroot/css/style.css`: müştəri tətbiqi (App, Auth, History) üçün ortaq dizayn: tipoqrafiya, düymələr, formlar, `.plastic*` (kart görünüşü), `.activity*` (əməliyyat sətri), `.data-table`, `.pick*` (custom select/picker), `.check` (custom checkbox), `.wheel-pop` (tarix təkəri), `.st*` (çıxarış), `@media print`.
- `wwwroot/css/home.css`: yalnız Home.
- `wwwroot/admin/css/style.css`: admin paneli.
- CSS dəyişənləri (`:root`) rəngləri mərkəzləşdirir.
- Mobil: `@media (max-width: ...)`; `box-sizing: border-box`, `width: 100%` ilə horizontal aşma qarşısı alınıb (məs. `.amount-input`).

## 5. Dizayn mənbəyi
Statik HTML dizayn: `C:\Users\User\Desktop\CaspianBank-Frontend`. Yeni müştəri səhifəsi yazanda əvvəl oradakı uyğun HTML (transaction-history, loan-application, bill-payment, notifications, contact-us, report-transaction, card-detail, transfer) açılır və dizayn ona uyğun qurulur.

## Özünü yoxla

1. JavaScript niyə təhlükəsizlik məntiqini daşımamalıdır?
2. `data-*` atributları ilə davranış bağlamaq nə üstünlük verir?
3. Canlı komissiya göstəricisi JS-dədir. Real komissiya harada hesablanır və niyə?
4. Təkər tarix seçici formaya hansı formatda dəyər göndərir?
5. `app-cards.js` niyə hər şeyi bir IIFE-də saxlayır?
6. Top-up xəta verəndə modal necə yenidən açılır?

## Əlavə (16 oktyabr 2026): köməkçi paneli, şübhəli əməliyyat səhifəsi, kiçik düzəlişlər

### Köməkçi (chat) düyməsi və sidebar
- Fayllar: `Views/Shared/_ChatWidget.cshtml`, `wwwroot/css/chat-widget.css`, `wwwroot/js/chat-widget.js`. Home, müştəri (`_AppLayout`) və giriş (`_AuthLayout`) layout-larına qoşulub; admin layout-unda yoxdur.
- Sağ aşağıda "Ask Caspian" düyməsi var; basanda sağdan tam hündürlükdə panel açılır (AI köməkçilərdəki kimi). Başlıq, söhbət, təklif düymələri (chip), yazı sahəsi (Enter göndərir, Shift+Enter yeni sətir), "yeni söhbət", Escape ilə bağlanma.
- **Hələlik önizləmədir:** yazılan mesaj görünür, köməkçi isə açıq deyir ki, cavab verə bilmir. Real xidmət qoşulanda yalnız `chat-widget.js`-dəki `reply(question)` funksiyası dəyişəcək (Promise ilə mətn qaytarır).
- CSS ayrı faylda və öz `--cw-*` dəyişənləri ilə yazılıb, çünki Home layout-u `style.css` yükləmir.
- Tələ: `font: 600 14px/1 inherit` yazmaq olmaz (qısa yazılışda `inherit` ailə yerinə düşmür, qayda ləğv olur). `font-family: inherit` ayrıca yazılır.

### Şübhəli əməliyyatı bildir (statik)
- `Controllers/ReportController.cs` (`App/Report`), `Views/Report/Index.cshtml` (dizayn: `report-transaction.html`). History cədvəlində hər xərc sətrində "Report" düyməsi var, əməliyyatın istinadı, təsviri, məbləği və tarixi linkdə gedir və səhifədə göstərilir. Göndərmə düyməsi söndürülüb (fraud komandasının admin tərəfi yazılanda qoşulacaq).

### Staff səhifəsi (admin, statik)
- `Areas/Admin/Controllers/StaffController.cs` (yalnız SuperAdmin), `Areas/Admin/Views/Staff/Index.cshtml`: komanda siyahısı, rol dəyişmə və deaktiv etmə düymələri, işçi əlavə etmə forması. Sidebar-da "Staff" linki yalnız SuperAdmin-ə görünür.

### Kiçik düzəlişlər
- Kreditlər cədvəlində ayrıca "Open" sütunu götürüldü (admin cədvəli üçün nəzərdə tutulmuş `td.actions { min-width: 230px }` müştəri cədvəlini enləndirirdi); kredit nömrəsi özü keçiddir.
- `style.css`-də `.admin-side` və `main.page` qaydaları media sorğusundan sonra təkrar elan olunduğu üçün admin yan menyusu telefonda ekranı tuturdu. Faylın sonuna eyni `@media (max-width: 860px)` qaydası əlavə olundu: menyu üstdə sıralanır.
