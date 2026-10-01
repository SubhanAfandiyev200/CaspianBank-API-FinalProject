using Domain.Constants;
using FluentValidation;
using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<RegisterDto> _registerValidator;

        public AccountService(IAccountRepository accountRepository,
                              IValidator<RegisterDto> registerValidator)
        {
            _accountRepository = accountRepository;
            _registerValidator = registerValidator;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterDto model)
        {
            var validation = await _registerValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());

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

        private static RegisterResponse Fail(params string[] errors)
            => new() { IsSuccess = false, Errors = errors };
    }
}
