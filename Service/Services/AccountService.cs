using Domain.Constants;
using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.Responses;
using Service.Helpers.Responses.DTOs.Account;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AccountService : IAccountService
    {
        private const int MinAge = 18;

        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterDto model)
        {
            if (!IsAdult(model.BirthDay))
                return Fail($"Qeydiyyat üçün minimum yaş {MinAge}-dir.");

            var user = new AppUser
            {
                Name = model.Name,
                Surname = model.Surname,
                Email = model.Email,
                UserName = model.Email, // login email ilədir
                PhoneNumber = model.PhoneNumber,
                BirthDay = model.BirthDay,
                EmailConfirmed = true // email OTP ilə qeydiyyatdan əvvəl təsdiqlənir
            };

            var result = await _accountRepository.RegisterAsync(user, model.Password);
            if (!result.Succeeded)
                return Fail(result.Errors.Select(e => e.Description).ToArray());

            var roleResult = await _accountRepository.AddToRoleAsync(user, Roles.Customer);
            if (!roleResult.Succeeded)
                return Fail(roleResult.Errors.Select(e => e.Description).ToArray());

            return new RegisterResponse { IsSuccess = true };
        }

        private static bool IsAdult(DateTime birthDay)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - birthDay.Year;
            if (birthDay.Date > today.AddYears(-age)) age--;
            return age >= MinAge;
        }

        private static RegisterResponse Fail(params string[] errors)
            => new() { IsSuccess = false, Errors = errors };
    }
}
