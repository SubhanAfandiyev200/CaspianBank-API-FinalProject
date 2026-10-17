using FluentValidation;
using Repository.Repositories.Interfaces;
using Repository.Results;
using Service.Helpers;
using Service.Helpers.DTOs.Users;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class UserAdminService : IUserAdminService
    {
        private const int MaxPageSize = 50;

        private readonly IUserAdminRepository _users;
        private readonly IValidator<RestrictUserDto> _restrictValidator;

        public UserAdminService(IUserAdminRepository users, IValidator<RestrictUserDto> restrictValidator)
        {
            _users = users;
            _restrictValidator = restrictValidator;
        }

        public async Task<ServiceResult<UserPageDto>> GetPageAsync(UserFilterDto filter)
        {
            bool? restricted = null;
            if (string.Equals(filter.Status, "Restricted", StringComparison.OrdinalIgnoreCase))
            {
                restricted = true;
            }
            else if (string.Equals(filter.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                restricted = false;
            }
            else if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                return ServiceResult<UserPageDto>.Fail("Choose Active or Restricted.");
            }

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Min(MaxPageSize, Math.Max(1, filter.PageSize));

            var result = await _users.GetCustomersPageAsync(filter.Search, restricted, (page - 1) * pageSize, pageSize);
            return ServiceResult<UserPageDto>.Ok(new UserPageDto
            {
                Items = result.Items.Select(ToListItem).ToList(),
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize,
                RestrictedCount = result.RestrictedCount
            });
        }

        public async Task<ServiceResult<UserDetailDto>> GetDetailAsync(string userId)
        {
            var data = await _users.GetCustomerDetailAsync(userId);
            if (data is null)
            {
                return ServiceResult<UserDetailDto>.NotFound();
            }
            return ServiceResult<UserDetailDto>.Ok(ToDetail(data));
        }

        public async Task<ServiceResult<UserDetailDto>> RestrictAsync(string staffName, string userId, RestrictUserDto model)
        {
            var validation = await _restrictValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<UserDetailDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var user = await _users.GetCustomerAsync(userId);
            if (user is null)
            {
                return ServiceResult<UserDetailDto>.NotFound();
            }
            if (user.IsRestricted)
            {
                return ServiceResult<UserDetailDto>.Fail("This account is already restricted.");
            }

            user.IsRestricted = true;
            user.RestrictionReason = model.Reason!.Trim();
            user.RestrictedAt = DateTime.UtcNow;
            user.RestrictedBy = string.IsNullOrWhiteSpace(staffName) ? "Staff" : staffName;
            await _users.SaveAsync(user);

            return await GetDetailAsync(userId);
        }

        public async Task<ServiceResult<UserDetailDto>> UnrestrictAsync(string userId)
        {
            var user = await _users.GetCustomerAsync(userId);
            if (user is null)
            {
                return ServiceResult<UserDetailDto>.NotFound();
            }
            if (!user.IsRestricted)
            {
                return ServiceResult<UserDetailDto>.Fail("This account is not restricted.");
            }

            user.IsRestricted = false;
            user.RestrictionReason = null;
            user.RestrictedAt = null;
            user.RestrictedBy = null;
            await _users.SaveAsync(user);

            return await GetDetailAsync(userId);
        }

        private static UserListItemDto ToListItem(UserRow row)
        {
            return new UserListItemDto
            {
                Id = row.User.Id,
                FullName = row.User.Name + " " + row.User.Surname,
                Email = row.User.Email ?? string.Empty,
                Phone = row.User.PhoneNumber,
                Fin = row.User.FinKod,
                RegisteredAt = row.User.CreatedAt,
                IsRestricted = row.User.IsRestricted,
                CardCount = row.CardCount,
                TotalBalance = row.TotalBalance
            };
        }

        private static UserDetailDto ToDetail(UserDetailData data)
        {
            var user = data.User;
            var cardById = data.Cards.ToDictionary(c => c.Id);

            return new UserDetailDto
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber,
                Fin = user.FinKod,
                BirthDate = user.BirthDay,
                RegisteredAt = user.CreatedAt,
                IsRestricted = user.IsRestricted,
                RestrictionReason = user.RestrictionReason,
                RestrictedAt = user.RestrictedAt,
                RestrictedBy = user.RestrictedBy,
                TotalSent = data.TotalSent,
                BillPaymentCount = data.BillPaymentCount,
                BillPaymentTotal = data.BillPaymentTotal,
                Cards = data.Cards.Select(c => new UserCardDto
                {
                    Id = c.Id,
                    Tier = c.Tier.ToString(),
                    Last4 = c.CardNumber.Length >= 4 ? c.CardNumber.Substring(c.CardNumber.Length - 4) : c.CardNumber,
                    Expiry = c.ExpiryDate.ToString("MM/yy", CultureInfo.InvariantCulture),
                    Balance = c.Balance,
                    IsBlocked = c.IsBlocked
                }).ToList(),
                Loans = data.Loans.Select(l => new UserLoanDto
                {
                    Id = l.Id,
                    ContractNumber = l.ContractNumber,
                    Status = l.Status.ToString(),
                    Amount = l.ApprovedAmount ?? l.RequestedAmount,
                    OutstandingPrincipal = l.OutstandingPrincipal
                }).ToList(),
                RecentActivity = data.RecentTransactions.Select(t => new UserActivityDto
                {
                    CreatedAt = t.CreatedAt,
                    Description = t.Description,
                    Type = t.Type.ToString(),
                    CardLabel = cardById.TryGetValue(t.CardId, out var card) ? card.Tier + " " + LoanMapper.CardLabel(card.CardNumber) : string.Empty,
                    Amount = t.Amount,
                    IsIncome = t.IsIncome
                }).ToList()
            };
        }
    }
}
