using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BankDirectoryService : IBankDirectoryService
    {
        private readonly IBankDirectoryRepository _directory;
        private readonly IBaseRepository<OwnBank> _ownBanks;

        public BankDirectoryService(IBankDirectoryRepository directory, IBaseRepository<OwnBank> ownBanks)
        {
            _directory = directory;
            _ownBanks = ownBanks;
        }

        public async Task<ServiceResult<BankLookupDto>> LookupAsync(string? bin)
        {
            var digits = new string((bin ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
            // Bank kart nömrəsinin ilk 6 rəqəmindən (real kartlarda olduğu kimi) tanınır; 8 rəqəmlik BIN-in qalan 2 rəqəmi bankı dəyişmir
            if (digits.Length < 6)
            {
                return ServiceResult<BankLookupDto>.Fail("Enter at least the first 6 digits of the card number.");
            }
            digits = digits.Substring(0, 6);

            var external = await _directory.FindExternalBankByBinAsync(digits);
            if (external is not null)
            {
                return ServiceResult<BankLookupDto>.Ok(new BankLookupDto { Name = ShortName(external.Name), LogoUrl = external.LogoUrl, IsCaspian = false });
            }

            // Caspian: bank qaydalarındakı BIN və ya mövcud Caspian kartlarının başlanğıcı
            var own = await _ownBanks.GetAsync();
            if ((own is not null && own.Bin.StartsWith(digits)) || await _directory.AnyCaspianCardStartsWithAsync(digits))
            {
                return ServiceResult<BankLookupDto>.Ok(new BankLookupDto { Name = own?.Name ?? "Caspian Bank", LogoUrl = own?.LogoUrl, IsCaspian = true });
            }

            return ServiceResult<BankLookupDto>.NotFound();
        }

        // "International Bank of Azerbaijan (ABB)" -> "ABB": çipdə qısa ad
        private static string ShortName(string name)
        {
            var open = name.LastIndexOf('(');
            var close = name.LastIndexOf(')');
            if (open >= 0 && close > open)
            {
                return name.Substring(open + 1, close - open - 1).Trim();
            }
            return name;
        }
    }
}
