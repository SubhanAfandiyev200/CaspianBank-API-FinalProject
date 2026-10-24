using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Domain.Security;
using Microsoft.Extensions.Logging;
using Repository.Repositories.Interfaces;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    // Pul əməliyyatından sonra şübhəli fəaliyyət qaydalarına baxır (hədlər Domain/Security/SecurityRules.cs-dədir).
    // Qaydalar yalnız siqnal yaradır, heç nəyi bloklamır: qərarı Security işçisi verir
    public class SecurityMonitor : ISecurityMonitor
    {
        private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);
        private static readonly string[] AlertReaders = { Roles.Security, Roles.Admin, Roles.SuperAdmin };

        private readonly ISecurityRepository _security;
        private readonly IAccountRepository _accounts;
        private readonly INotificationService _notifications;
        private readonly ILogger<SecurityMonitor> _logger;

        public SecurityMonitor(ISecurityRepository security,
                               IAccountRepository accounts,
                               INotificationService notifications,
                               ILogger<SecurityMonitor> logger)
        {
            _security = security;
            _accounts = accounts;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task EvaluateTransferAsync(string userId, decimal amount, string reference)
        {
            try
            {
                var now = DateTime.UtcNow;

                // 1) Tək böyük köçürmə
                var largeSeverity = SecurityRules.LargeTransferSeverity(amount);
                if (largeSeverity.HasValue)
                {
                    await RaiseAsync(userId, SecurityAlertKind.LargeTransfer, largeSeverity.Value, reference, amount,
                        "Sent " + Money(amount) + " in one transfer.");
                }

                // 2) Qısa müddətdə çoxlu köçürmə (10 dəqiqəlik pəncərə: eyni pəncərədə təkrar siqnal yaranmır)
                var window = TimeSpan.FromMinutes(SecurityRules.RapidTransferMinutes);
                var recent = await _security.CountOutgoingSinceAsync(userId, now - window);
                if (recent >= SecurityRules.RapidTransferCount)
                {
                    var windowKey = now.Ticks / window.Ticks;
                    await RaiseAsync(userId, SecurityAlertKind.RapidTransfers, SecurityAlertSeverity.Medium, "rapid:" + userId + ":" + windowKey, null,
                        recent + " transfers to other people in " + SecurityRules.RapidTransferMinutes + " minutes.");
                }

                // 3) Bir gündə (Bakı günü) göndərilən cəm
                var bakuDay = (now + BakuOffset).Date;
                var dayStartUtc = DateTime.SpecifyKind(bakuDay - BakuOffset, DateTimeKind.Utc);
                var daySum = await _security.SumOutgoingSinceAsync(userId, dayStartUtc);
                if (daySum >= SecurityRules.DailyVolumeHigh)
                {
                    await RaiseAsync(userId, SecurityAlertKind.DailyVolume, SecurityAlertSeverity.High,
                        "daily:" + userId + ":" + bakuDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture), daySum,
                        "Sent " + Money(daySum) + " to other people today.");
                }

                // 4) Yeni hesabdan nisbətən böyük köçürmə (artıq "böyük köçürmə" siqnalı yaranıbsa eyni köçürməyə ikinci siqnal yazılmır)
                if (!largeSeverity.HasValue && amount >= SecurityRules.NewAccountAmount)
                {
                    var user = await _accounts.GetByIdAsync(userId);
                    if (user is not null && SecurityRules.IsNewAccount(user.CreatedAt, now))
                    {
                        await RaiseAsync(userId, SecurityAlertKind.NewAccountOutflow, SecurityAlertSeverity.Medium, reference, amount,
                            "Account is less than " + SecurityRules.NewAccountDays + " days old and sent " + Money(amount) + ".");
                    }
                }
            }
            catch (Exception ex)
            {
                // Nəzarət xətası köçürməni pozmasın
                _logger.LogError(ex, "Şübhəli fəaliyyət yoxlaması işləmədi: {UserId}", userId);
            }
        }

        public async Task OnAccountLockedAsync(string userId)
        {
            try
            {
                var user = await _accounts.GetByIdAsync(userId);
                if (user is null)
                {
                    return;
                }

                // İşçilərin bloku bu siyahıya düşmür (siqnal müştəri hesabları üçündür)
                var roles = await _accounts.GetRolesAsync(user);
                if (!roles.Contains(Roles.Customer))
                {
                    return;
                }

                var hourKey = DateTime.UtcNow.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
                await RaiseAsync(userId, SecurityAlertKind.AccountLockout, SecurityAlertSeverity.Medium, "lock:" + userId + ":" + hourKey, null,
                    "The account was locked for a few minutes after repeated wrong passwords.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bloklanmış hesab siqnalı yazılmadı: {UserId}", userId);
            }
        }

        public async Task OnDeceasedRegistrationAttemptAsync(string fin, string name)
        {
            try
            {
                // Eyni FİN üçün bir saatda bir siqnal (təkrar cəhd siqnalı doldurmasın)
                var hourKey = DateTime.UtcNow.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
                var description = "Someone tried to open an account with the ID record of a deceased citizen: " + name + ", FIN " + fin + ".";
                var created = await _security.TryAddAsync(new SecurityAlert
                {
                    UserId = null,
                    Kind = SecurityAlertKind.DeceasedIdentity,
                    Severity = SecurityAlertSeverity.High,
                    Reference = "deceased:" + fin + ":" + hourKey,
                    Description = description
                });
                if (!created)
                {
                    return;
                }

                await _notifications.NotifyRolesAsync(AlertReaders, NotificationCategory.Staff,
                    "Registration attempt with a deceased citizen's ID", description, "/Admin/Security",
                    "security-alert:" + SecurityAlertKind.DeceasedIdentity + ":" + fin + ":" + hourKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vəfat etmiş vətəndaş siqnalı yazılmadı");
            }
        }

        private async Task RaiseAsync(string userId, SecurityAlertKind kind, SecurityAlertSeverity severity, string reference, decimal? amount, string description)
        {
            var created = await _security.TryAddAsync(new SecurityAlert
            {
                UserId = userId,
                Kind = kind,
                Severity = severity,
                Reference = reference,
                Amount = amount,
                Description = description
            });
            if (!created)
            {
                return;
            }

            var user = await _accounts.GetByIdAsync(userId);
            var who = user is null ? "A customer" : user.Name + " " + user.Surname;
            await _notifications.NotifyRolesAsync(AlertReaders, NotificationCategory.Staff,
                severity == SecurityAlertSeverity.High ? "Suspicious activity (high)" : "Suspicious activity",
                who + ": " + description, "/Admin/Security", "security-alert:" + kind + ":" + reference);
        }

        private static string Money(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture) + " AZN";
        }
    }
}
