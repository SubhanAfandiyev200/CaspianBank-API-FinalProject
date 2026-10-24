using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Support;
using Service.Helpers.Responses;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class SupportTicketService : ISupportTicketService
    {
        private const int MaxOpenPerCustomer = 5;
        private const int MaxMessagesPerTicket = 100;
        private const int MaxPageSize = 50;

        private const int MaxOpenPerGuest = 3;

        // Yeni mesajdan xəbər tutan işçilər: hələlik bütün işçi rolları (hamı bildirişi görür və yazışmanı aça bilir)
        private static readonly string[] DeskRoles = Roles.AllStaff;

        private readonly ISupportTicketRepository _tickets;
        private readonly IAccountRepository _accounts;
        private readonly INotificationService _notifications;
        private readonly IValidator<CreateTicketDto> _createValidator;
        private readonly IValidator<ReplyTicketDto> _replyValidator;
        private readonly IValidator<SendHelpDto> _helpValidator;
        private readonly IFileService _files;
        private readonly IEmailSender _email;
        private readonly ILogger<SupportTicketService> _logger;

        public SupportTicketService(ISupportTicketRepository tickets,
                                    IAccountRepository accounts,
                                    INotificationService notifications,
                                    IValidator<CreateTicketDto> createValidator,
                                    IValidator<ReplyTicketDto> replyValidator,
                                    IValidator<SendHelpDto> helpValidator,
                                    IFileService files,
                                    IEmailSender email,
                                    ILogger<SupportTicketService> logger)
        {
            _tickets = tickets;
            _accounts = accounts;
            _notifications = notifications;
            _createValidator = createValidator;
            _replyValidator = replyValidator;
            _helpValidator = helpValidator;
            _files = files;
            _email = email;
            _logger = logger;
        }

        // ---------- "Help" pəncərəsi (hər səhifədəki köməkçi) ----------

        // Daxil olmuş müştəri: mesaj onun yazışmasına düşür (son 24 saatda açıq "Help" yazışması varsa ona əlavə olunur), cavab bildirişlərinə gəlir.
        // Qeydiyyatsız ziyarətçi: email məcburidir, işçi cavabı həmin emailə gedir. İşçilər hər halda bildiriş alır
        public async Task<ServiceResult<HelpSentDto>> SendHelpAsync(string? userId, SendHelpDto model)
        {
            var validation = await _helpValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<HelpSentDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            AppUser? user = null;
            string? email = null;
            if (!string.IsNullOrEmpty(userId))
            {
                user = await _accounts.GetByIdAsync(userId);
                if (user is null)
                {
                    return ServiceResult<HelpSentDto>.NotFound();
                }
                if (user.IsRestricted)
                {
                    return ServiceResult<HelpSentDto>.Fail("Your account is restricted. Please contact support.");
                }
            }
            else
            {
                email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (email.Length == 0)
                {
                    return ServiceResult<HelpSentDto>.Fail("Enter your email so we can answer you.");
                }
                if (await _tickets.CountOpenByGuestAsync(email) >= MaxOpenPerGuest)
                {
                    return ServiceResult<HelpSentDto>.Fail("You already have " + MaxOpenPerGuest + " open messages. We will answer them by email.");
                }
            }

            var body = model.Message!.Trim();
            string? imagePath = null;
            if (model.Image is not null)
            {
                imagePath = await _files.UploadFileAsync(model.Image, "help");
            }

            var now = DateTime.UtcNow;
            var authorId = user?.Id ?? "guest";
            var authorName = user is null ? email! : (user.Name + " " + user.Surname).Trim();

            SupportTicket? ticket = null;
            if (user is not null)
            {
                ticket = await _tickets.FindOpenHelpTicketAsync(user.Id, now.AddHours(-24));
            }

            if (ticket is not null)
            {
                if (ticket.Messages.Count >= MaxMessagesPerTicket)
                {
                    return ServiceResult<HelpSentDto>.Fail("This conversation is too long. Please write a new message.");
                }
                AddMessage(ticket, authorId, authorName, false, body);
                ticket.Messages.Last().ImagePath = imagePath;
                ticket.Status = TicketStatus.Pending;
                await _tickets.SaveAsync();
            }
            else
            {
                if (user is not null && await _tickets.CountOpenByUserAsync(user.Id) >= MaxOpenPerCustomer)
                {
                    return ServiceResult<HelpSentDto>.Fail("You already have " + MaxOpenPerCustomer + " open messages. Wait for an answer or close one before writing a new one.");
                }

                var subject = "Help: " + (body.Length > 60 ? body.Substring(0, 60) : body).Replace('\r', ' ').Replace('\n', ' ');
                ticket = new SupportTicket
                {
                    UserId = user?.Id,
                    GuestEmail = email,
                    Subject = subject,
                    Status = TicketStatus.Pending,
                    LastActivityAt = now
                };
                ticket.Messages.Add(new SupportMessage
                {
                    AuthorUserId = authorId,
                    AuthorName = authorName,
                    IsStaff = false,
                    Body = body,
                    ImagePath = imagePath,
                    CreatedAt = now
                });
                await _tickets.AddAsync(ticket);
            }

            var preview = body.Length > 140 ? body.Substring(0, 140) + "…" : body;
            await _notifications.NotifyRolesAsync(DeskRoles, NotificationCategory.Staff,
                user is null ? "New help message from a visitor" : "New help message",
                authorName + (user is null ? " (not registered)" : string.Empty) + ": " + preview,
                "/Admin/Tickets/Detail/" + ticket.Id);

            return ServiceResult<HelpSentDto>.Ok(new HelpSentDto { TicketId = user is null ? null : ticket.Id, IsGuest = user is null });
        }

        // ---------- Müştəri ----------

        public async Task<IEnumerable<TicketListItemDto>> GetMineAsync(string userId)
        {
            var items = await _tickets.GetByUserAsync(userId);
            return items.Select(ToListItem).ToList();
        }

        public async Task<ServiceResult<TicketDetailDto>> GetMineDetailAsync(string userId, int id)
        {
            var ticket = await _tickets.GetWithMessagesAsync(id, userId);
            if (ticket is null)
            {
                return ServiceResult<TicketDetailDto>.NotFound();
            }
            return ServiceResult<TicketDetailDto>.Ok(ToDetail(ticket));
        }

        public async Task<ServiceResult<TicketDetailDto>> CreateAsync(string userId, CreateTicketDto model)
        {
            var validation = await _createValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<TicketDetailDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<TicketDetailDto>.NotFound();
            }

            if (await _tickets.CountOpenByUserAsync(userId) >= MaxOpenPerCustomer)
            {
                return ServiceResult<TicketDetailDto>.Fail("You already have " + MaxOpenPerCustomer + " open messages. Wait for an answer or close one before writing a new one.");
            }

            var name = (user.Name + " " + user.Surname).Trim();
            var now = DateTime.UtcNow;
            var ticket = new SupportTicket
            {
                UserId = userId,
                Subject = model.Subject!.Trim(),
                Status = TicketStatus.Pending,
                LastActivityAt = now
            };
            ticket.Messages.Add(new SupportMessage
            {
                AuthorUserId = userId,
                AuthorName = name,
                IsStaff = false,
                Body = model.Message!.Trim(),
                CreatedAt = now
            });
            await _tickets.AddAsync(ticket);

            await NotifyDeskAsync(ticket, name, "New message from " + name);

            ticket.User = user;
            return ServiceResult<TicketDetailDto>.Ok(ToDetail(ticket));
        }

        public async Task<ServiceResult<TicketDetailDto>> CustomerReplyAsync(string userId, int id, ReplyTicketDto model)
        {
            var validation = await _replyValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<TicketDetailDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var ticket = await _tickets.GetWithMessagesAsync(id, userId);
            if (ticket is null)
            {
                return ServiceResult<TicketDetailDto>.NotFound();
            }
            if (ticket.Status == TicketStatus.Closed)
            {
                return ServiceResult<TicketDetailDto>.Fail("This conversation is closed. Write a new message instead.");
            }
            if (ticket.Messages.Count >= MaxMessagesPerTicket)
            {
                return ServiceResult<TicketDetailDto>.Fail("This conversation is too long. Please write a new message.");
            }

            var name = (ticket.User!.Name + " " + ticket.User.Surname).Trim();
            AddMessage(ticket, userId, name, false, model.Message!);
            ticket.Status = TicketStatus.Pending;
            await _tickets.SaveAsync();

            await NotifyDeskAsync(ticket, name, name + " replied");
            return ServiceResult<TicketDetailDto>.Ok(ToDetail(ticket));
        }

        public async Task<ServiceResult<TicketDetailDto>> CustomerCloseAsync(string userId, int id)
        {
            var ticket = await _tickets.GetWithMessagesAsync(id, userId);
            if (ticket is null)
            {
                return ServiceResult<TicketDetailDto>.NotFound();
            }
            if (ticket.Status != TicketStatus.Closed)
            {
                ticket.Status = TicketStatus.Closed;
                ticket.LastActivityAt = DateTime.UtcNow;
                await _tickets.SaveAsync();
            }
            return ServiceResult<TicketDetailDto>.Ok(ToDetail(ticket));
        }

        // ---------- İşçi ----------

        public async Task<ServiceResult<TicketPageDto>> GetPageAsync(TicketFilterDto filter)
        {
            TicketStatus? status = null;
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (!EnumRules.IsName<TicketStatus>(filter.Status) || !Enum.TryParse<TicketStatus>(filter.Status, true, out var parsed))
                {
                    return ServiceResult<TicketPageDto>.Fail("Choose Pending, Answered or Closed.");
                }
                status = parsed;
            }

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Min(MaxPageSize, Math.Max(1, filter.PageSize));
            var (items, total, pending) = await _tickets.GetPageAsync(status, filter.Search, (page - 1) * pageSize, pageSize);

            return ServiceResult<TicketPageDto>.Ok(new TicketPageDto
            {
                Items = items.Select(ToListItem).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                PendingCount = pending
            });
        }

        public async Task<ServiceResult<TicketDetailDto>> GetDetailAsync(int id)
        {
            var ticket = await _tickets.GetWithMessagesAsync(id, null);
            if (ticket is null)
            {
                return ServiceResult<TicketDetailDto>.NotFound();
            }
            return ServiceResult<TicketDetailDto>.Ok(ToDetail(ticket));
        }

        public async Task<ServiceResult<TicketDetailDto>> StaffReplyAsync(string staffUserId, int id, ReplyTicketDto model)
        {
            var validation = await _replyValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<TicketDetailDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var staff = await _accounts.GetByIdAsync(staffUserId);
            if (staff is null)
            {
                return ServiceResult<TicketDetailDto>.NotFound();
            }

            var ticket = await _tickets.GetWithMessagesAsync(id, null);
            if (ticket is null)
            {
                return ServiceResult<TicketDetailDto>.NotFound();
            }
            if (ticket.Status == TicketStatus.Closed)
            {
                return ServiceResult<TicketDetailDto>.Fail("This conversation is closed.");
            }
            if (ticket.Messages.Count >= MaxMessagesPerTicket)
            {
                return ServiceResult<TicketDetailDto>.Fail("This conversation is too long. Close it and ask the customer to write again.");
            }

            AddMessage(ticket, staffUserId, (staff.Name + " " + staff.Surname).Trim(), true, model.Message!);
            ticket.Status = TicketStatus.Answered;
            await _tickets.SaveAsync();

            if (ticket.UserId is not null)
            {
                await _notifications.NotifyAsync(ticket.UserId, NotificationCategory.Support,
                    "We answered your message",
                    "Re: " + ticket.Subject,
                    "/App/Support/Ticket/" + ticket.Id);
            }
            else if (!string.IsNullOrEmpty(ticket.GuestEmail))
            {
                // Qeydiyyatsız ziyarətçinin bildirişi yoxdur: cavab emailə gedir. Email getməsə yazışma qalır, işçi bunu jurnaldan görür
                await SendGuestEmailAsync(ticket, "Re: " + ticket.Subject, model.Message!.Trim());
            }
            return ServiceResult<TicketDetailDto>.Ok(ToDetail(ticket));
        }

        public async Task<ServiceResult<TicketDetailDto>> StaffCloseAsync(string staffUserId, int id)
        {
            var ticket = await _tickets.GetWithMessagesAsync(id, null);
            if (ticket is null)
            {
                return ServiceResult<TicketDetailDto>.NotFound();
            }
            if (ticket.Status == TicketStatus.Closed)
            {
                return ServiceResult<TicketDetailDto>.Fail("This conversation is already closed.");
            }

            ticket.Status = TicketStatus.Closed;
            ticket.LastActivityAt = DateTime.UtcNow;
            await _tickets.SaveAsync();

            if (ticket.UserId is not null)
            {
                await _notifications.NotifyAsync(ticket.UserId, NotificationCategory.Support,
                    "Your message was closed",
                    "Re: " + ticket.Subject,
                    "/App/Support/Ticket/" + ticket.Id);
            }
            return ServiceResult<TicketDetailDto>.Ok(ToDetail(ticket));
        }

        // ---------- Köməkçilər ----------

        private async Task NotifyDeskAsync(SupportTicket ticket, string customerName, string title)
        {
            await _notifications.NotifyRolesAsync(DeskRoles, NotificationCategory.Staff, title, ticket.Subject, "/Admin/Tickets/Detail/" + ticket.Id);
        }

        // Qeydiyyatsız ziyarətçiyə işçinin cavabı email kimi gedir (HTML-ə təhlükəsiz çevrilir)
        private async Task SendGuestEmailAsync(SupportTicket ticket, string subject, string reply)
        {
            try
            {
                var safe = System.Net.WebUtility.HtmlEncode(reply).Replace("\n", "<br />");
                var html = "<p>" + safe + "</p><p style=\"color:#667\">Caspian Bank. You can answer this email to write to us again.</p>";
                await _email.SendAsync(ticket.GuestEmail!, "Caspian Bank: " + subject, html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Qeydiyyatsız ziyarətçiyə cavab emaili göndərilmədi: ticket {TicketId}", ticket.Id);
            }
        }

        private static void AddMessage(SupportTicket ticket, string authorId, string authorName, bool isStaff, string body)
        {
            var now = DateTime.UtcNow;
            ticket.Messages.Add(new SupportMessage
            {
                TicketId = ticket.Id,
                AuthorUserId = authorId,
                AuthorName = authorName,
                IsStaff = isStaff,
                Body = body.Trim(),
                CreatedAt = now
            });
            ticket.LastActivityAt = now;
        }

        private static TicketListItemDto ToListItem(SupportTicket ticket)
        {
            var last = ticket.Messages.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).FirstOrDefault();
            var preview = last is null ? string.Empty : last.Body;
            if (preview.Length > 90)
            {
                preview = preview.Substring(0, 90) + "…";
            }

            return new TicketListItemDto
            {
                Id = ticket.Id,
                Subject = ticket.Subject,
                Status = ticket.Status.ToString(),
                CreatedAt = ticket.CreatedAt,
                LastActivityAt = ticket.LastActivityAt,
                MessageCount = ticket.Messages.Count,
                LastMessagePreview = preview,
                IsGuest = ticket.User is null,
                CustomerName = ticket.User is null ? "Guest" : (ticket.User.Name + " " + ticket.User.Surname).Trim(),
                CustomerEmail = ticket.User?.Email ?? ticket.GuestEmail ?? string.Empty
            };
        }

        private static TicketDetailDto ToDetail(SupportTicket ticket)
        {
            return new TicketDetailDto
            {
                Id = ticket.Id,
                Subject = ticket.Subject,
                Status = ticket.Status.ToString(),
                CreatedAt = ticket.CreatedAt,
                CustomerId = ticket.UserId ?? string.Empty,
                IsGuest = ticket.User is null,
                CustomerName = ticket.User is null ? "Guest" : (ticket.User.Name + " " + ticket.User.Surname).Trim(),
                CustomerEmail = ticket.User?.Email ?? ticket.GuestEmail ?? string.Empty,
                Messages = ticket.Messages
                    .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
                    .Select(m => new TicketMessageDto
                    {
                        Id = m.Id,
                        AuthorName = m.AuthorName,
                        IsStaff = m.IsStaff,
                        Body = m.Body,
                        ImageUrl = m.ImagePath,
                        CreatedAt = m.CreatedAt
                    }).ToList()
            };
        }
    }
}
