using Domain.Entities;

namespace Service.Services.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) CreateToken(AppUser user, IEnumerable<string> roles);

        // Yalnız bildiriş hub-ında keçən çox qısa ömürlü bilet (başqa audience, API endpoint-lərində etibarsızdır).
        // Brauzer əsas JWT-ni görmür; hub-a qoşulmaq üçün MVC-dən bunu alır
        (string Token, DateTime ExpiresAt) CreateHubToken(string userId);
    }
}
