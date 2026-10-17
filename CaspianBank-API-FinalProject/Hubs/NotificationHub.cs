using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CaspianBank_API_FinalProject.Hubs
{
    // Canlı bildirişlər: brauzer bura qoşulur, server "notification" mesajını yalnız həmin istifadəçinin bağlantılarına göndərir.
    // Giriş yalnız hub üçün verilən qısa ömürlü bilet ilə olur ("Hub" sxemi), adi JWT burada işləmir
    [Authorize(AuthenticationSchemes = HubAuth.Scheme)]
    public class NotificationHub : Hub
    {
    }

    public static class HubAuth
    {
        public const string Scheme = "Hub";
        public const string Path = "/hubs/notifications";
    }
}
