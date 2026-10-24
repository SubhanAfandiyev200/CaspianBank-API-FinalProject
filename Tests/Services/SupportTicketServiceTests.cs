using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Support;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class SupportTicketServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public SupportTicketServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<ISupportTicketService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<ISupportTicketService>());
        }

        private static CreateTicketDto Ticket(string subject = "Card problem", string message = "My card does not work")
        {
            return new CreateTicketDto { Subject = subject, Message = message };
        }

        private async Task<TicketDetailDto> CreateAsync(CustomerKit kit, string subject = "Card problem", string message = "My card does not work")
        {
            var result = await WithAsync(s => s.CreateAsync(kit.User.Id, Ticket(subject, message)));
            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            return result.Data!;
        }

        // ---------- Müştəri ----------

        [Fact]
        public async Task A_new_message_creates_a_pending_conversation_and_tells_the_desk()
        {
            var support = await _app.NewStaffAsync(Roles.CustomerSupport);
            var accountant = await _app.NewStaffAsync(Roles.Accountant);
            var kit = await _app.NewKitAsync(0m, "Elçin", "Hüseynov");

            var ticket = await CreateAsync(kit, "  Card problem  ", "  My card does not work  ");

            Assert.Equal("Card problem", ticket.Subject);
            Assert.Equal("Pending", ticket.Status);
            Assert.Equal("Elçin Hüseynov", ticket.CustomerName);
            var message = Assert.Single(ticket.Messages);
            Assert.Equal("My card does not work", message.Body);
            Assert.False(message.IsStaff);
            Assert.Equal("Elçin Hüseynov", message.AuthorName);

            var notice = Assert.Single(await _app.NotificationsAsync(support.Id), n => n.LinkUrl == $"/Admin/Tickets/Detail/{ticket.Id}");
            Assert.Equal("New message from Elçin Hüseynov", notice.Title);
            Assert.Equal("Card problem", notice.Message);
            Assert.DoesNotContain(await _app.NotificationsAsync(accountant.Id), n => n.LinkUrl == $"/Admin/Tickets/Detail/{ticket.Id}");
        }

        [Fact]
        public async Task A_message_is_validated_and_needs_a_known_user()
        {
            var kit = await _app.NewKitAsync();

            var bad = await WithAsync(s => s.CreateAsync(kit.User.Id, Ticket("ab", "hi")));
            var ghost = await WithAsync(s => s.CreateAsync("nobody", Ticket()));

            Assert.False(bad.IsSuccess);
            Assert.Contains("The subject must be at least 3 characters.", bad.Errors);
            Assert.True(ghost.IsNotFound);
        }

        [Fact]
        public async Task At_most_five_conversations_can_be_open_and_closing_one_makes_room()
        {
            var kit = await _app.NewKitAsync();
            var created = new List<TicketDetailDto>();
            for (var i = 0; i < 5; i++)
            {
                created.Add(await CreateAsync(kit, "Question " + i));
            }

            var sixth = await WithAsync(s => s.CreateAsync(kit.User.Id, Ticket("Question 6")));
            await WithAsync(s => s.CustomerCloseAsync(kit.User.Id, created[0].Id));
            var afterClose = await WithAsync(s => s.CreateAsync(kit.User.Id, Ticket("Question 6")));

            Assert.Equal("You already have 5 open messages. Wait for an answer or close one before writing a new one.", sixth.Errors[0]);
            Assert.True(afterClose.IsSuccess);
        }

        [Fact]
        public async Task The_customer_sees_only_their_own_conversations_newest_activity_included()
        {
            var kit = await _app.NewKitAsync();
            var other = await _app.NewKitAsync();
            var mine = await CreateAsync(kit, "Mine one");
            await CreateAsync(other, "Theirs");

            var list = (await WithAsync(s => s.GetMineAsync(kit.User.Id))).ToList();
            var detail = await WithAsync(s => s.GetMineDetailAsync(kit.User.Id, mine.Id));
            var foreign = await WithAsync(s => s.GetMineDetailAsync(other.User.Id, mine.Id));

            Assert.Equal("Mine one", Assert.Single(list).Subject);
            Assert.Equal(1, list[0].MessageCount);
            Assert.True(detail.IsSuccess);
            Assert.True(foreign.IsNotFound);
        }

        [Fact]
        public async Task The_list_preview_is_clipped_after_ninety_characters()
        {
            var kit = await _app.NewKitAsync();
            await CreateAsync(kit, "Long one", new string('w', 200));

            var item = Assert.Single(await WithAsync(s => s.GetMineAsync(kit.User.Id)));

            Assert.Equal(91, item.LastMessagePreview.Length);
            Assert.EndsWith("…", item.LastMessagePreview);
        }

        // ---------- Cavablar ----------

        [Fact]
        public async Task The_staff_reply_answers_the_conversation_and_notifies_the_customer()
        {
            var kit = await _app.NewKitAsync();
            var staff = await _app.NewStaffAsync(Roles.CustomerSupport);
            var ticket = await CreateAsync(kit, "Need help");

            var result = await WithAsync(s => s.StaffReplyAsync(staff.Id, ticket.Id, new ReplyTicketDto { Message = "  We are on it  " }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Answered", result.Data!.Status);
            Assert.Equal(2, result.Data.Messages.Count);
            var reply = result.Data.Messages.Last();
            Assert.True(reply.IsStaff);
            Assert.Equal("We are on it", reply.Body);
            Assert.Equal("Test User", reply.AuthorName);
            var notice = Assert.Single(await _app.NotificationsAsync(kit.User.Id));
            Assert.Equal("We answered your message", notice.Title);
            Assert.Equal("Re: Need help", notice.Message);
            Assert.Equal($"/App/Support/Ticket/{ticket.Id}", notice.LinkUrl);
            Assert.Equal(NotificationCategory.Support, notice.Category);
        }

        [Fact]
        public async Task A_customer_reply_puts_the_conversation_back_to_pending_and_tells_the_desk()
        {
            var kit = await _app.NewKitAsync(0m, "Aysel", "Mammadova");
            var support = await _app.NewStaffAsync(Roles.CustomerSupport);
            var ticket = await CreateAsync(kit, "Follow up");
            await WithAsync(s => s.StaffReplyAsync(support.Id, ticket.Id, new ReplyTicketDto { Message = "Please check again" }));

            var result = await WithAsync(s => s.CustomerReplyAsync(kit.User.Id, ticket.Id, new ReplyTicketDto { Message = "Still the same" }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Pending", result.Data!.Status);
            Assert.Equal(3, result.Data.Messages.Count);
            Assert.Equal(new[] { false, true, false }, result.Data.Messages.Select(m => m.IsStaff).ToArray());
            Assert.Contains(await _app.NotificationsAsync(support.Id), n => n.Title == "Aysel Mammadova replied" && n.LinkUrl == $"/Admin/Tickets/Detail/{ticket.Id}");
        }

        [Fact]
        public async Task Only_the_owner_can_reply_or_close_and_a_closed_conversation_takes_no_replies()
        {
            var kit = await _app.NewKitAsync();
            var stranger = await _app.NewKitAsync();
            var staff = await _app.NewStaffAsync(Roles.CustomerSupport);
            var ticket = await CreateAsync(kit);

            Assert.True((await WithAsync(s => s.CustomerReplyAsync(stranger.User.Id, ticket.Id, new ReplyTicketDto { Message = "hello" }))).IsNotFound);
            Assert.True((await WithAsync(s => s.CustomerCloseAsync(stranger.User.Id, ticket.Id))).IsNotFound);

            await WithAsync(s => s.CustomerCloseAsync(kit.User.Id, ticket.Id));
            var customerAfter = await WithAsync(s => s.CustomerReplyAsync(kit.User.Id, ticket.Id, new ReplyTicketDto { Message = "more" }));
            var staffAfter = await WithAsync(s => s.StaffReplyAsync(staff.Id, ticket.Id, new ReplyTicketDto { Message = "more" }));

            Assert.Equal("This conversation is closed. Write a new message instead.", customerAfter.Errors[0]);
            Assert.Equal("This conversation is closed.", staffAfter.Errors[0]);
        }

        [Fact]
        public async Task Replies_are_validated_and_the_staff_member_and_conversation_must_exist()
        {
            var kit = await _app.NewKitAsync();
            var staff = await _app.NewStaffAsync(Roles.CustomerSupport);
            var ticket = await CreateAsync(kit);

            var tooShort = await WithAsync(s => s.StaffReplyAsync(staff.Id, ticket.Id, new ReplyTicketDto { Message = "x" }));
            var noTicket = await WithAsync(s => s.StaffReplyAsync(staff.Id, 999999, new ReplyTicketDto { Message = "hello" }));
            var noStaff = await WithAsync(s => s.StaffReplyAsync("nobody", ticket.Id, new ReplyTicketDto { Message = "hello" }));
            var customerEmpty = await WithAsync(s => s.CustomerReplyAsync(kit.User.Id, ticket.Id, new ReplyTicketDto { Message = "" }));

            Assert.False(tooShort.IsSuccess);
            Assert.True(noTicket.IsNotFound);
            Assert.True(noStaff.IsNotFound);
            Assert.False(customerEmpty.IsSuccess);
        }

        [Fact]
        public async Task A_conversation_is_limited_to_one_hundred_messages()
        {
            var kit = await _app.NewKitAsync();
            var staff = await _app.NewStaffAsync(Roles.CustomerSupport);
            var ticket = await CreateAsync(kit);
            await _app.QueryAsync(async db =>
            {
                for (var i = 0; i < 99; i++)
                {
                    db.SupportMessages.Add(new Domain.Entities.SupportMessage { TicketId = ticket.Id, AuthorUserId = kit.User.Id, AuthorName = "x", Body = "filler " + i });
                }
                await db.SaveChangesAsync();
                return 0;
            });

            var customer = await WithAsync(s => s.CustomerReplyAsync(kit.User.Id, ticket.Id, new ReplyTicketDto { Message = "one more" }));
            var staffReply = await WithAsync(s => s.StaffReplyAsync(staff.Id, ticket.Id, new ReplyTicketDto { Message = "one more" }));

            Assert.Equal("This conversation is too long. Please write a new message.", customer.Errors[0]);
            Assert.Equal("This conversation is too long. Close it and ask the customer to write again.", staffReply.Errors[0]);
        }

        // ---------- Bağlama ----------

        [Fact]
        public async Task The_customer_can_close_and_closing_twice_is_harmless()
        {
            var kit = await _app.NewKitAsync();
            var ticket = await CreateAsync(kit);

            var first = await WithAsync(s => s.CustomerCloseAsync(kit.User.Id, ticket.Id));
            var again = await WithAsync(s => s.CustomerCloseAsync(kit.User.Id, ticket.Id));

            Assert.Equal("Closed", first.Data!.Status);
            Assert.True(again.IsSuccess);
            Assert.Equal("Closed", again.Data!.Status);
        }

        [Fact]
        public async Task Staff_closing_tells_the_customer_and_a_second_close_is_refused()
        {
            var kit = await _app.NewKitAsync();
            var staff = await _app.NewStaffAsync(Roles.CustomerSupport);
            var ticket = await CreateAsync(kit, "Done soon");

            var closed = await WithAsync(s => s.StaffCloseAsync(staff.Id, ticket.Id));
            var again = await WithAsync(s => s.StaffCloseAsync(staff.Id, ticket.Id));
            var unknown = await WithAsync(s => s.StaffCloseAsync(staff.Id, 999999));

            Assert.Equal("Closed", closed.Data!.Status);
            Assert.Equal("This conversation is already closed.", again.Errors[0]);
            Assert.True(unknown.IsNotFound);
            var notice = Assert.Single(await _app.NotificationsAsync(kit.User.Id));
            Assert.Equal("Your message was closed", notice.Title);
            Assert.Equal("Re: Done soon", notice.Message);
        }

        // ---------- İşçi siyahısı ----------

        [Fact]
        public async Task The_staff_list_filters_by_status_and_search_and_counts_pending()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(0m, "Zaur", "Ticketov");
                var other = await app.NewKitAsync(0m, "Other", "Person");
                var staff = await app.NewStaffAsync(Roles.CustomerSupport);
                using var scope = app.NewScope();
                var service = scope.ServiceProvider.GetRequiredService<ISupportTicketService>();
                var a = (await service.CreateAsync(kit.User.Id, Ticket("Alpha topic"))).Data!;
                var b = (await service.CreateAsync(kit.User.Id, Ticket("Beta topic"))).Data!;
                var c = (await service.CreateAsync(other.User.Id, Ticket("Gamma topic"))).Data!;
                await service.StaffReplyAsync(staff.Id, b.Id, new ReplyTicketDto { Message = "answered" });
                await service.StaffCloseAsync(staff.Id, c.Id);

                var all = await service.GetPageAsync(new TicketFilterDto());
                Assert.Equal(3, all.Data!.TotalCount);
                Assert.Equal(1, all.Data.PendingCount);
                Assert.Equal(1, (await service.GetPageAsync(new TicketFilterDto { Status = "pending" })).Data!.TotalCount);
                Assert.Equal(1, (await service.GetPageAsync(new TicketFilterDto { Status = "Answered" })).Data!.TotalCount);
                Assert.Equal(1, (await service.GetPageAsync(new TicketFilterDto { Status = "Closed" })).Data!.TotalCount);
                Assert.Equal(2, (await service.GetPageAsync(new TicketFilterDto { Search = "Ticketov" })).Data!.TotalCount);
                Assert.Equal(1, (await service.GetPageAsync(new TicketFilterDto { Search = "Gamma" })).Data!.TotalCount);
                Assert.Equal("Zaur Ticketov", all.Data.Items.First(i => i.Id == a.Id).CustomerName);
                Assert.Equal(1, (await service.GetPageAsync(new TicketFilterDto { PageSize = 1, Page = 3 })).Data!.Items.Count);
            }
        }

        [Fact]
        public async Task The_staff_list_rejects_unknown_statuses_including_numbers_and_clamps_paging()
        {
            var unknown = await WithAsync(s => s.GetPageAsync(new TicketFilterDto { Status = "Gone" }));
            var numeric = await WithAsync(s => s.GetPageAsync(new TicketFilterDto { Status = "99" }));
            var clamped = await WithAsync(s => s.GetPageAsync(new TicketFilterDto { Page = -3, PageSize = 100000 }));

            Assert.Equal("Choose Pending, Answered or Closed.", unknown.Errors[0]);
            Assert.Equal("Choose Pending, Answered or Closed.", numeric.Errors[0]);
            Assert.Equal(1, clamped.Data!.Page);
            Assert.Equal(50, clamped.Data.PageSize);
        }

        [Fact]
        public async Task Staff_can_open_any_conversation_by_id()
        {
            var kit = await _app.NewKitAsync();
            var ticket = await CreateAsync(kit);

            var detail = await WithAsync(s => s.GetDetailAsync(ticket.Id));
            var missing = await WithAsync(s => s.GetDetailAsync(999999));

            Assert.True(detail.IsSuccess);
            Assert.Equal(kit.User.Id, detail.Data!.CustomerId);
            Assert.True(missing.IsNotFound);
        }
    }
}
