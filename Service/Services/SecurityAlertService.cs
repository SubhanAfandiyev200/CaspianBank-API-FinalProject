using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Security;
using Service.Helpers.DTOs.Users;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class SecurityAlertService : ISecurityAlertService
    {
        private const int MaxPageSize = 100;
        private const int MaxNoteLength = 300;

        private readonly ISecurityRepository _security;
        private readonly IUserAdminService _users;

        public SecurityAlertService(ISecurityRepository security, IUserAdminService users)
        {
            _security = security;
            _users = users;
        }

        public async Task<ServiceResult<SecurityAlertPageDto>> GetPageAsync(SecurityAlertFilterDto filter)
        {
            SecurityAlertStatus? status = null;
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (!Enum.TryParse<SecurityAlertStatus>(filter.Status, true, out var parsedStatus) || !Enum.IsDefined(parsedStatus))
                {
                    return ServiceResult<SecurityAlertPageDto>.Fail("Unknown status.");
                }
                status = parsedStatus;
            }

            SecurityAlertSeverity? severity = null;
            if (!string.IsNullOrWhiteSpace(filter.Severity))
            {
                if (!Enum.TryParse<SecurityAlertSeverity>(filter.Severity, true, out var parsedSeverity) || !Enum.IsDefined(parsedSeverity))
                {
                    return ServiceResult<SecurityAlertPageDto>.Fail("Unknown severity.");
                }
                severity = parsedSeverity;
            }

            SecurityAlertKind? kind = null;
            if (!string.IsNullOrWhiteSpace(filter.Kind))
            {
                if (!Enum.TryParse<SecurityAlertKind>(filter.Kind, true, out var parsedKind) || !Enum.IsDefined(parsedKind))
                {
                    return ServiceResult<SecurityAlertPageDto>.Fail("Unknown alert type.");
                }
                kind = parsedKind;
            }

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, MaxPageSize);
            var (items, total) = await _security.GetPagedAsync(status, severity, kind, filter.Search, page, pageSize);
            var (open, openHigh, resolvedWeek) = await _security.GetCountsAsync(DateTime.UtcNow.AddDays(-7));

            return ServiceResult<SecurityAlertPageDto>.Ok(new SecurityAlertPageDto
            {
                Items = items.Select(ToDto).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)),
                OpenCount = open,
                OpenHighCount = openHigh,
                ResolvedThisWeek = resolvedWeek
            });
        }

        public async Task<ServiceResult<SecurityAlertDto>> ResolveAsync(string staffName, int id, ResolveAlertDto model)
        {
            var freeze = string.Equals(model.Outcome, "Freeze", StringComparison.OrdinalIgnoreCase);
            var dismiss = string.Equals(model.Outcome, "Dismiss", StringComparison.OrdinalIgnoreCase);
            if (!freeze && !dismiss)
            {
                return ServiceResult<SecurityAlertDto>.Fail("Choose what to do with this alert.");
            }

            var note = model.Note?.Trim();
            if (note is not null && note.Length > MaxNoteLength)
            {
                return ServiceResult<SecurityAlertDto>.Fail("The note can be at most " + MaxNoteLength + " characters.");
            }
            if (freeze && (note is null || note.Length < 5))
            {
                return ServiceResult<SecurityAlertDto>.Fail("Write why the account is frozen (at least 5 characters).");
            }

            var alert = await _security.GetAsync(id);
            if (alert is null)
            {
                return ServiceResult<SecurityAlertDto>.NotFound();
            }
            if (alert.Status != SecurityAlertStatus.Open)
            {
                return ServiceResult<SecurityAlertDto>.Fail("This alert was already handled.");
            }

            if (freeze && alert.User is null)
            {
                return ServiceResult<SecurityAlertDto>.Fail("This alert has no account to freeze.");
            }

            if (freeze && alert.User is not null && !alert.User.IsRestricted)
            {
                var restricted = await _users.RestrictAsync(staffName, alert.UserId!, new RestrictUserDto { Reason = "Security: " + note });
                if (!restricted.IsSuccess)
                {
                    return restricted.IsNotFound
                        ? ServiceResult<SecurityAlertDto>.Fail("The account can't be frozen here (it is not a customer account).")
                        : ServiceResult<SecurityAlertDto>.Fail(restricted.Errors);
                }

                // RestrictAsync başqa kontekst vəziyyəti dəyişdi: siqnal təzədən oxunur
                alert = await _security.GetAsync(id);
                if (alert is null)
                {
                    return ServiceResult<SecurityAlertDto>.NotFound();
                }
            }

            alert.Status = freeze ? SecurityAlertStatus.Frozen : SecurityAlertStatus.Dismissed;
            alert.ResolvedByName = string.IsNullOrWhiteSpace(staffName) ? "Staff" : staffName;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolutionNote = note;
            await _security.SaveAsync(alert);

            return ServiceResult<SecurityAlertDto>.Ok(ToDto(alert));
        }

        private static SecurityAlertDto ToDto(SecurityAlert alert)
        {
            return new SecurityAlertDto
            {
                Id = alert.Id,
                CreatedAt = alert.CreatedAt,
                Kind = alert.Kind.ToString(),
                Severity = alert.Severity.ToString(),
                Status = alert.Status.ToString(),
                Description = alert.Description,
                Amount = alert.Amount,
                Reference = alert.Reference,
                CustomerId = alert.UserId ?? string.Empty,
                CustomerName = alert.User is null ? string.Empty : (alert.User.Name + " " + alert.User.Surname).Trim(),
                Email = alert.User?.Email ?? string.Empty,
                AccountRestricted = alert.User?.IsRestricted ?? false,
                ResolvedByName = alert.ResolvedByName,
                ResolvedAt = alert.ResolvedAt,
                ResolutionNote = alert.ResolutionNote
            };
        }
    }
}
