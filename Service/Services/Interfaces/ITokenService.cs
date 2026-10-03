using Domain.Entities;

namespace Service.Services.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) CreateToken(AppUser user, IEnumerable<string> roles);
    }
}
