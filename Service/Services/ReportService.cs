using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Reports;
using Service.Helpers.DTOs.Users;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class ReportService : IReportService, IReportAdminService
    {
        private const int MaxPageSize = 100;
        private const int MinReasonLength = 10;
        private const int MaxReasonLength = 500;
        private const int MaxNoteLength = 300;
        private static readonly string[] Readers = { Roles.Security, Roles.Admin, Roles.SuperAdmin };

        private readonly IReportRepository _reports;
        private readonly IAccountRepository _accounts;
        private readonly IUserAdminService _users;
        private readonly INotificationService _notifications;

        public ReportService(IReportRepository reports,
                             IAccountRepository accounts,
                             IUserAdminService users,
                             INotificationService notifications)
        {
            _reports = reports;
            _accounts = accounts;
            _users = users;
            _notifications = notifications;
        }

        // ---------- Müştəri ----------

        public async Task<ServiceResult<MyReportDto>> ReportAsync(string userId, ReportTransactionDto model)
        {
            var reason = model.Reason?.Trim() ?? string.Empty;
            if (reason.Length < MinReasonLength)
            {
                return ServiceResult<MyReportDto>.Fail("Tell us what looks wrong (at least " + MinReasonLength + " characters).");
            }
            if (reason.Length > MaxReasonLength)
            {
                return ServiceResult<MyReportDto>.Fail("The reason can be at most " + MaxReasonLength + " characters.");
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<MyReportDto>.NotFound();
            }

            var transaction = await _reports.FindTransactionForUserAsync(model.TransactionId, userId);
            if (transaction is null)
            {
                return ServiceResult<MyReportDto>.Fail("We couldn't find this transaction on your cards.");
            }
            if (transaction.IsIncome)
            {
                return ServiceResult<MyReportDto>.Fail("Only money that left your card can be reported.");
            }

            var report = new TransactionReport
            {
                UserId = userId,
                TransactionId = transaction.Id,
                TransactionReference = transaction.Reference,
                Description = transaction.Description.Length > 200 ? transaction.Description.Substring(0, 200) : transaction.Description,
                Amount = transaction.Amount,
                TransactionAt = transaction.CreatedAt,
                Reason = reason
            };
            if (!await _reports.TryAddAsync(report))
            {
                return ServiceResult<MyReportDto>.Fail("You already reported this transaction.");
            }

            await _notifications.NotifyRolesAsync(Readers, NotificationCategory.Staff, "Suspicious transaction reported",
                user.Name + " " + user.Surname + " reported " + transaction.Reference + " (" + Money(transaction.Amount) + ").",
                "/Admin/Reports", "report:" + report.Id);

            return ServiceResult<MyReportDto>.Ok(ToMine(report));
        }

        public async Task<IEnumerable<MyReportDto>> GetMineAsync(string userId)
        {
            var reports = await _reports.GetByUserAsync(userId);
            return reports.Select(ToMine).ToList();
        }

        // ---------- İşçi ----------

        public async Task<ServiceResult<AdminReportPageDto>> GetPageAsync(ReportFilterDto filter)
        {
            ReportStatus? status = null;
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (!Enum.TryParse<ReportStatus>(filter.Status, true, out var parsed) || !Enum.IsDefined(parsed))
                {
                    return ServiceResult<AdminReportPageDto>.Fail("Unknown status.");
                }
                status = parsed;
            }

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, MaxPageSize);
            var (items, total) = await _reports.GetPagedAsync(status, filter.Search, page, pageSize);
            var (pending, resolvedWeek) = await _reports.GetCountsAsync(DateTime.UtcNow.AddDays(-7));

            var rows = new List<AdminReportDto>();
            foreach (var report in items)
            {
                var dto = ToAdmin(report);
                if (report.Status == ReportStatus.Pending)
                {
                    dto.ProposedAction = await DescribeActionAsync(report);
                }
                rows.Add(dto);
            }

            return ServiceResult<AdminReportPageDto>.Ok(new AdminReportPageDto
            {
                Items = rows,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)),
                PendingCount = pending,
                ResolvedThisWeek = resolvedWeek
            });
        }

        public async Task<ServiceResult<AdminReportDto>> ResolveAsync(string staffName, int id, ResolveReportDto model)
        {
            var outcome = (model.Outcome ?? string.Empty).Trim();
            if (outcome != "Review" && outcome != "Action" && outcome != "Dismiss")
            {
                return ServiceResult<AdminReportDto>.Fail("Choose what to do with this report.");
            }

            var note = model.Note?.Trim();
            if (note is not null && note.Length > MaxNoteLength)
            {
                return ServiceResult<AdminReportDto>.Fail("The note can be at most " + MaxNoteLength + " characters.");
            }
            if (outcome == "Action" && (note is null || note.Length < 5))
            {
                return ServiceResult<AdminReportDto>.Fail("Write why action is taken (at least 5 characters).");
            }

            var report = await _reports.GetAsync(id);
            if (report is null)
            {
                return ServiceResult<AdminReportDto>.NotFound();
            }
            if (report.Status != ReportStatus.Pending)
            {
                return ServiceResult<AdminReportDto>.Fail("This report was already handled.");
            }

            string? actionTaken = null;
            if (outcome == "Action")
            {
                var (taken, error) = await TakeActionAsync(report, staffName, note!);
                if (taken is null)
                {
                    return ServiceResult<AdminReportDto>.Fail(error!);
                }
                actionTaken = taken;

                // Əməliyyat zamanı kontekst təmizləndi: bildiriş təzədən oxunur
                report = await _reports.GetAsync(id);
                if (report is null)
                {
                    return ServiceResult<AdminReportDto>.NotFound();
                }
            }

            report.Status = outcome == "Action" ? ReportStatus.ActionTaken : outcome == "Review" ? ReportStatus.Reviewed : ReportStatus.Dismissed;
            report.ResolvedByName = string.IsNullOrWhiteSpace(staffName) ? "Staff" : staffName;
            report.ResolvedAt = DateTime.UtcNow;
            report.ResolutionNote = note;
            report.ActionTaken = actionTaken;
            await _reports.SaveAsync();

            var message = report.Status == ReportStatus.Dismissed
                ? "We reviewed your report about " + report.TransactionReference + " and found no problem."
                : "We reviewed your report about " + report.TransactionReference + (actionTaken is null ? "." : " and took action.");
            await _notifications.NotifyAsync(report.UserId, NotificationCategory.Card, "Your report was reviewed", message, "/App/History", "report-resolved:" + report.Id);

            return ServiceResult<AdminReportDto>.Ok(ToAdmin(report));
        }

        // Qarşı tərəf başqa Caspian müştərisidirsə onun hesabı dondurulur; deyilsə (başqa bank, ödəniş) bildirilən kart bloklanır
        private async Task<(string? Taken, string? Error)> TakeActionAsync(TransactionReport report, string staffName, string note)
        {
            var recipientId = await _reports.FindRecipientUserIdAsync(report.TransactionReference);
            if (recipientId is not null && recipientId != report.UserId)
            {
                var recipient = await _accounts.GetByIdAsync(recipientId);
                if (recipient is not null && !recipient.IsRestricted)
                {
                    var restricted = await _users.RestrictAsync(staffName, recipientId, new RestrictUserDto { Reason = "Report " + report.TransactionReference + ": " + note });
                    if (restricted.IsSuccess)
                    {
                        return ("The account of " + recipient.Name + " " + recipient.Surname + " was frozen.", null);
                    }
                    if (!restricted.IsNotFound)
                    {
                        return (null, string.Join(" ", restricted.Errors));
                    }
                    // Qarşı tərəf müştəri deyil (işçi hesabı): kartı bloklamağa keçilir
                }
                else if (recipient is not null && recipient.IsRestricted)
                {
                    return ("The account of " + recipient.Name + " " + recipient.Surname + " was already frozen.", null);
                }
            }

            var transaction = await _reports.FindTransactionForUserAsync(report.TransactionId, report.UserId);
            if (transaction is null)
            {
                return (null, "The reported transaction can't be found.");
            }
            var label = await _reports.BlockCardAsync(transaction.CardId);
            if (label is null)
            {
                return (null, "No automatic step is possible here (the Cashback card can't be blocked). Use \"Mark reviewed\" instead.");
            }
            return ("The card " + label + " was blocked.", null);
        }

        private async Task<string?> DescribeActionAsync(TransactionReport report)
        {
            var recipientId = await _reports.FindRecipientUserIdAsync(report.TransactionReference);
            if (recipientId is not null && recipientId != report.UserId)
            {
                var recipient = await _accounts.GetByIdAsync(recipientId);
                if (recipient is not null)
                {
                    return recipient.IsRestricted
                        ? "The account of " + recipient.Name + " " + recipient.Surname + " is already frozen."
                        : "Freeze the account of " + recipient.Name + " " + recipient.Surname + " (the receiver).";
                }
            }

            var transaction = await _reports.FindTransactionForUserAsync(report.TransactionId, report.UserId);
            if (transaction is null)
            {
                return null;
            }
            if (transaction.Card.Tier == CardTier.Cashback)
            {
                return "No automatic step (Cashback card).";
            }
            return "Block the card " + transaction.Card.Tier + " " + "•••• " + transaction.Card.CardNumber.Substring(transaction.Card.CardNumber.Length - 4) + ".";
        }

        private static MyReportDto ToMine(TransactionReport report)
        {
            return new MyReportDto
            {
                Id = report.Id,
                TransactionReference = report.TransactionReference,
                Description = report.Description,
                Amount = report.Amount,
                TransactionAt = report.TransactionAt,
                Reason = report.Reason,
                Status = report.Status.ToString(),
                CreatedAt = report.CreatedAt,
                ResolvedAt = report.ResolvedAt
            };
        }

        private static AdminReportDto ToAdmin(TransactionReport report)
        {
            return new AdminReportDto
            {
                Id = report.Id,
                CreatedAt = report.CreatedAt,
                ReporterId = report.UserId,
                ReporterName = (report.User.Name + " " + report.User.Surname).Trim(),
                ReporterEmail = report.User.Email ?? string.Empty,
                TransactionReference = report.TransactionReference,
                Description = report.Description,
                Amount = report.Amount,
                TransactionAt = report.TransactionAt,
                Reason = report.Reason,
                Status = report.Status.ToString(),
                ActionTaken = report.ActionTaken,
                ResolvedByName = report.ResolvedByName,
                ResolvedAt = report.ResolvedAt,
                ResolutionNote = report.ResolutionNote
            };
        }

        private static string Money(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture) + " AZN";
        }
    }
}
