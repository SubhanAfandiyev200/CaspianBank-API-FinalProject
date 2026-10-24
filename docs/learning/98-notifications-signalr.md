# 98. Bildirişlər və SignalR (canlı bildirişlər)

## 1. Nəzəriyyə

**Problem:** müştəri kredit təklifini səhifəni yeniləmədən görməlidir. Adi HTTP-də server özü brauzerə mesaj göndərə bilmir: brauzer soruşmalıdır.

| Yol | Necə işləyir | Çatışmazlığı |
|---|---|---|
| Polling | brauzer hər 5 saniyədən bir "yeni bir şey var?" soruşur | boş sorğular, gecikmə |
| **WebSocket** | brauzer və server arasında açıq qalan iki tərəfli boru: server istədiyi vaxt yazır | özün idarə etməlisən (qopma, təkrar qoşulma) |
| **SignalR** | WebSocket üstündə hazır kitabxana: hub, istifadəçiyə mesaj, avtomatik yenidən qoşulma | yeni anlayışlar |

SignalR-da **Hub** serverdəki "otaq"dır. Server `Clients.User(userId).SendAsync("notification", ...)` deyir, həmin istifadəçinin bütün açıq səhifələrinə (bir neçə tab ola bilər) mesaj gedir. Brauzer `connection.on("notification", ...)` ilə dinləyir.

## 2. Fayllar

**API**
- `Domain/Entities/Notification.cs`, `Enums/NotificationCategory.cs` (Loan, Transfer, Card, Staff), `Configurations/NotificationConfiguration.cs`, migration `AddedNotifications`.
- `Repository/Repositories/NotificationRepository.cs` (+ interfeys): siyahı, oxunmamış sayı, oxundu işarələmə, `TryAddAsync`. `AccountRepository.GetUserIdsInRolesAsync` (işçilərə göndərmək üçün).
- `Service/Services/NotificationService.cs`: `NotifyAsync`, `NotifyRolesAsync`, siyahı, oxundu, hub bileti. `INotificationPublisher` (servis qatı SignalR-ı tanımır), `NullNotificationPublisher`.
- `CaspianBank-API-FinalProject/Hubs/NotificationHub.cs`, `Helpers/SignalRNotificationPublisher.cs` (real göndərmə), `Controllers/Client/NotificationsController.cs` (`api/notifications`).
- `Program.cs`: `AddSignalR`, ikinci JWT sxemi `Hub`, `MapHub("/hubs/notifications")`.

**MVC**
- `Controllers/NotificationsController.cs` (`App/Notifications`: siyahı, Open, MarkAll, Ticket), `Areas/Admin/Controllers/NotificationsController.cs` (işçi siyahısı).
- `ViewComponents/NotificationBellViewComponent.cs` + `Views/Shared/Components/NotificationBell/Default.cshtml` (header zəngi), `Views/Shared/_NotificationsPage.cshtml` (ortaq səhifə gövdəsi), `Helpers/NotificationUi.cs`.
- `wwwroot/js/notifications.js` (SignalR qoşulması, sayğac, toast), `_AppLayout` və `_AdminLayout` (zəng və skriptlər).

## 3. Kim kimə nə bildirir

| Hadisə | Kimə | Harada yazılır |
|---|---|---|
| Yeni kredit müraciəti | Accountant, Admin, SuperAdmin | `LoanService.ApplyAsync` |
| Kredit təsdiq olundu (pul düşdü) | müştəri | `LoanReviewService.ApproveAsync` |
| Azaldılmış təklif | müştəri | eyni yer |
| Müraciət rədd olundu (ümumi mətn) | müştəri | `LoanReviewService.RejectAsync` |
| Başqası kodla ödəniş etdi / kredit bağlandı | kreditin sahibi | `LoanService` (ödəniş) |
| Təklifin vaxtı bitdi, ödəniş yaxınlaşır (3 gün), gecikmə, Court | müştəri | gündəlik iş (`RunDailyMaintenanceAsync`) |
| Pul gəldi (başqa müştəridən köçürmə) | alıcı | `TransferService` |
| Kart bloklandı / blokdan çıxdı | kartın sahibi | `CardService` |

**Təkrar yazılmasın:** gündəlik iş hər saat işləyir. Hər hadisənin `DedupeKey`-i var (`late:42`, `due:42`, `court:7`...), `(UserId, DedupeKey)` unikal indeksdir. Eyni gecikmə üçün ikinci bildiriş yazılmır.

**Əsas əməliyyat bildirişdən asılı deyil:** `NotifyAsync` xətanı tutub loglayır. Bildiriş yazıla bilməsə də kredit və ya köçürmə uğurlu qalır.

## 4. Təhlükəsizlik: niyə ayrıca "hub bileti"?

Layihədə JWT cookie-nin içindədir və JS onu görə bilmir (XSS olsa belə oğurlanmasın deyə). WebSocket üçün isə brauzerin tokeni lazımdır. Həll:

1. Brauzer MVC-dən `GET /App/Notifications/Ticket` istəyir (cookie ilə).
2. MVC adi JWT ilə API-yə `POST api/notifications/hub-token` deyir, **2 dəqiqəlik bilet** alır.
3. Bilet **başqa audience**-lidir (`...Hub`). Adi API endpoint-lərində etibarsızdır (401), hub-da isə adi JWT etibarsızdır. Hub-ın ayrıca `Hub` sxemi var.
4. Bilet WebSocket-də `?access_token=` ilə gedir (WebSocket başlıq göndərə bilmir), API onu yalnız `/hubs` yolunda oxuyur.
5. Yenidən qoşulanda JS təzə bilet alır.

Bilet oğurlansa da 2 dəqiqəyə bitir və yalnız həmin istifadəçinin bildirişlərini oxuya bilər, pul əməliyyatı edə bilməz.

## 5. Yoxlama (necə sınandı)
- PowerShell ilə WebSocket: adi JWT hub-a qoşula bilmir, hub bileti adi API-də 401 verir, bilet hub-a qoşulur, mühasib canlı mesaj alır; CustomerSupport işçi bildirişi almır.
- Brauzerdə iki sessiya: mühasib qərar verəndə müştərinin səhifəsində sayğac və toast yenilənmədən görünür.
- Gündəlik iş: vaxtı bitən təklif, gecikmə, yaxınlaşan ödəniş və Court bildirişi yazıldı; iş ikinci dəfə işləyəndə təkrar olmadı.

## 6. Tələlər
- **MVC iki cür "Notifications":** müştəri controller-i və `Admin` area-sı eyni adlıdır. Area redirect-ində `new { area = "Admin" }` yazmaq lazımdır.
- **admin.js ilə toqquşma:** zəngdə `data-dropdown` yazmaq olmaz (admin.js də onu idarə edib iki dəfə açıb-bağlayır). Zəng `data-bell` işlədir.
- **CORS lazım olmadı:** `skipNegotiation + WebSockets` işlədilir, ona görə brauzer API-yə CORS sorğusu atmır.
- Test skriptində PowerShell dəyişən adları böyük-kiçik hərfə həssas deyil (`$A` və `$a` eynidir).

## Özünü yoxla
1. Polling, WebSocket və SignalR arasındakı fərq nədir?
2. `Clients.User(userId)` bir istifadəçinin iki tabına necə çatır?
3. Niyə hub üçün ayrıca qısa ömürlü bilet var və adi JWT-ni JS-ə vermirik?
4. `DedupeKey` və unikal indeks hansı problemi həll edir?
5. Servis qatı niyə `INotificationPublisher` interfeysini bilir, SignalR-ı yox?
6. Bildiriş yazıla bilməsə kredit əməliyyatı niyə uğurlu qalmalıdır?

## 7. Sonradan əlavə olunanlar (2026-10-24)
- **Qara siyahı:** kredit şöbəsi qara siyahıdakı müştərinin kreditini təsdiq edəndə Super Admin-lərə "Loan for a blacklisted customer needs your approval" gedir (mətn: "X approved N AZN for Y (loan KR-...), who is on the blacklist. Do you approve it too?"). Yeni müraciətdə işçilərə "New loan application (blacklisted customer)" gedir. Müştəriyə qara siyahı barədə heç nə deyilmir.
- **Top-up:** başqa Caspian kartından top-up ediləndə mənbə kartın sahibinə "Money left your card" gedir.
- **Tərcümə:** bildiriş mətnləri dəyişənli olduğu üçün şablon kimi bazadadır (`translations-notifications.sql`); MVC onları gələn mətnə uyğunlaşdırır (`105-theme-and-languages.md`). Canlı (SignalR) bildiriş brauzerdə `POST /App/Notifications/Translate` ilə seçilmiş dilə çevrilir, sonra göstərilir.
