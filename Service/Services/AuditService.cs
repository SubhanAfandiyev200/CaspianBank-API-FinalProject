using Domain.Constants;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Audit;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AuditService : IAuditService
    {
        private const int MaxPageSize = 100;
        private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);

        private readonly IAuditLogRepository _logs;
        private readonly IAccountRepository _accounts;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IAuditLogRepository logs, IAccountRepository accounts, ILogger<AuditService> logger)
        {
            _logs = logs;
            _accounts = accounts;
            _logger = logger;
        }

        public async Task LogAsync(string? actorUserId, string action, string targetType, string targetLabel, string? details = null)
        {
            try
            {
                var name = "System";
                var role = string.Empty;
                if (!string.IsNullOrEmpty(actorUserId))
                {
                    var actor = await _accounts.GetByIdAsync(actorUserId);
                    if (actor is not null)
                    {
                        name = (actor.Name + " " + actor.Surname).Trim();
                        var roles = await _accounts.GetRolesAsync(actor);
                        role = string.Join(", ", roles);
                    }
                }

                await _logs.AddAsync(new AuditLog
                {
                    ActorUserId = string.IsNullOrEmpty(actorUserId) ? null : actorUserId,
                    ActorName = Cut(name, 120),
                    ActorRole = Cut(role, 50),
                    Action = Cut(action, 60),
                    TargetType = Cut(targetType, 40),
                    TargetLabel = Cut(targetLabel, 150),
                    Details = Cut(details ?? string.Empty, 400)
                });
            }
            catch (Exception ex)
            {
                // Audit yazıla bilməsə də əsas əməliyyat (dondurma, kredit qərarı) uğurlu qalır
                _logger.LogError(ex, "Audit qeydi yazılmadı: {Action} {Target}", action, targetLabel);
            }
        }

        public async Task<ServiceResult<AuditPageDto>> GetPageAsync(AuditFilterDto filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Action) && !AuditActions.All.Contains(filter.Action))
            {
                return ServiceResult<AuditPageDto>.Fail("Choose one of the listed actions.");
            }
            if (filter.From.HasValue && filter.To.HasValue && filter.From.Value.Date > filter.To.Value.Date)
            {
                return ServiceResult<AuditPageDto>.Fail("The start date can't be after the end date.");
            }

            // Bakı günü UTC aralığına çevrilir: [From 00:00, To+1 00:00) Bakı vaxtı
            DateTime? fromUtc = filter.From.HasValue ? DateTime.SpecifyKind(filter.From.Value.Date - BakuOffset, DateTimeKind.Utc) : null;
            DateTime? toUtc = filter.To.HasValue ? DateTime.SpecifyKind(filter.To.Value.Date.AddDays(1) - BakuOffset, DateTimeKind.Utc) : null;

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Min(MaxPageSize, Math.Max(1, filter.PageSize));

            var (items, total) = await _logs.GetPageAsync(filter.Search, filter.Action, fromUtc, toUtc, (page - 1) * pageSize, pageSize);
            return ServiceResult<AuditPageDto>.Ok(new AuditPageDto
            {
                Items = items.Select(a => new AuditItemDto
                {
                    Id = a.Id,
                    CreatedAt = a.CreatedAt,
                    ActorName = a.ActorName,
                    ActorRole = a.ActorRole,
                    Action = a.Action,
                    TargetType = a.TargetType,
                    TargetLabel = a.TargetLabel,
                    Details = a.Details
                }).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        private static string Cut(string value, int max)
        {
            return value.Length > max ? value.Substring(0, max) : value;
        }
    }
}
