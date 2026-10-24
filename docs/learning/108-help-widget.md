# Help pəncərəsi (sağ aşağıdakı "Ask Caspian")

## Məqsəd
Hər səhifədəki pəncərə indi **Help** kimi işləyir: yazılan mesaj (istəsə bir şəkillə) işçilərə gedir. AI köməkçi sonra qoşulanda yalnız `chat-widget.js` içindəki `deliver` hissəsi dəyişəcək.

## Axın
1. Ziyarətçi/müştəri pəncərədə mesaj yazır. **Giriş etməyibsə** email də yazır (məcburidir); **daxil olubsa** email soruşulmur.
2. `chat-widget.js` → MVC `POST /Help/Send` (`HelpController`, anonim, antiforgery ilə) → API `POST api/help` (`HelpController`, anonim, `auth` sürət limiti).
3. `SupportTicketService.SendHelpAsync`:
   - Qeydiyyatsız: `SupportTicket.UserId = null`, `GuestEmail = email`; bir email üçün ən çoxu **3 açıq** yazışma.
   - Daxil olmuş: son 24 saatdakı açıq "Help" yazışması davam edir, yoxdursa yeni açılır.
   - Şəkil `files` xidməti ilə `help` qovluğuna yazılır (`SupportMessage.ImagePath`).
   - **Bütün işçi rolları** (`Roles.AllStaff`) bildiriş alır; bildiriş `/Admin/Tickets/Detail/{id}` açır.
4. İşçi admin panelində "Contact Tickets" səhifəsində oxuyur (indi hər işçi rolu görür) və cavab yazır (`StaffReplyAsync`):
   - Qeydiyyatlı müştəri → "We answered your message" bildirişi + `/App/Support/Ticket/{id}`.
   - Qeydiyyatsız → cavab **emailə** gedir (`SendGuestEmailAsync`).

## Nəyə diqqət
- Qeydiyyatsızın bildirişi yoxdur, ona görə cavabı yalnız email daşıyır. Email göndərilməsə yazışma qalır.
- Rolu daraltmaq istəyəndə: `Roles.AllStaff` (API) və `AppRoles.Staff` / `_AdminLayout.cshtml` (MVC) dəyişir.
- Test: `Tests/Services/HelpMessageTests.cs`.
