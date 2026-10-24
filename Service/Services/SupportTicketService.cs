using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
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

        // Yeni mesajdan xəbər tutan işçilər
        private static readonly string[] DeskRoles = { Roles.CustomerSupport, Roles.Admin, Roles.SuperAdmin };

        private readonly ISupportTicketRepository _tickets;
        private readonly IAccountRepository _accounts;
        private readonly INotificationService _notifications;
        private readonly IValidator<CreateTicketDto> _createValidator;
        private readonly IValidator<ReplyTicketDto> _replyValidator;

        public SupportTicketService(ISupportTicketRepository tickets,
                                    IAccountRepository accounts,
                                    INotificationService notifications,
                                    IValidator<CreateTicketDto> createValidator,
                                    IValidator<ReplyTicketDto> replyValidator)
        {
            _tickets = tickets;
            _accounts = accounts;
            _notifications = notifications;
            _createValidator = createValidator;
            _replyValidator = replyValidator;
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

            var name = (ticket.User.Name + " " + ticket.User.Surname).Trim();
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

            await _notifications.NotifyAsync(ticket.UserId, NotificationCategory.Support,
                "We answered your message",
                "Re: " + ticket.Subject,
                "/App/Support/Ticket/" + ticket.Id);
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

            await _notifications.NotifyAsync(ticket.UserId, NotificationCategory.Support,
                "Your message was closed",
                "Re: " + ticket.Subject,
                "/App/Support/Ticket/" + ticket.Id);
            return ServiceResult<TicketDetailDto>.Ok(ToDetail(ticket));
        }

        // ---------- Köməkçilər ----------

        private async Task NotifyDeskAsync(SupportTicket ticket, string customerName, string title)
        {
            await _notifications.NotifyRolesAsync(DeskRoles, NotificationCategory.Staff, title, ticket.Subject, "/Admin/Tickets/Detail/" + ticket.Id);
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
                CustomerName = ticket.User is null ? string.Empty : (ticket.User.Name + " " + ticket.User.Surname).Trim(),
                CustomerEmail = ticket.User?.Email ?? string.Empty
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
                CustomerId = ticket.UserId,
                CustomerName = (ticket.User.Name + " " + ticket.User.Surname).Trim(),
                CustomerEmail = ticket.User.Email ?? string.Empty,
                Messages = ticket.Messages
                    .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
                    .Select(m => new TicketMessageDto
                    {
                        Id = m.Id,
                        AuthorName = m.AuthorName,
                        IsStaff = m.IsStaff,
                        Body = m.Body,
                        CreatedAt = m.CreatedAt
                    }).ToList()
            };
        }
    }
}
