using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Support;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    // "Help" pəncərəsi: qeydiyyatsız ziyarətçi emailini yazır (cavab emailə gedir), daxil olmuş müştəri üçün email lazım deyil, hər işçi rolu bildiriş alır
    public class HelpMessageTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public HelpMessageTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<ISupportTicketService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<ISupportTicketService>());
        }

        [Fact]
        public async Task A_visitor_must_write_an_email()
        {
            var result = await WithAsync(s => s.SendHelpAsync(null, new SendHelpDto { Message = "My card is blocked" }));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task A_visitor_message_reaches_every_staff_role_and_the_answer_goes_to_the_email()
        {
            var support = await _app.NewStaffAsync(Roles.CustomerSupport);
            var security = await _app.NewStaffAsync(Roles.Security);
            var accountant = await _app.NewStaffAsync(Roles.Accountant);

            var sent = await WithAsync(s => s.SendHelpAsync(null, new SendHelpDto { Email = "Visitor@Example.com", Message = "I can't register" }));

            Assert.True(sent.IsSuccess, string.Join(";", sent.Errors));
            Assert.True(sent.Data!.IsGuest);
            Assert.Null(sent.Data.TicketId);
            foreach (var user in new[] { support, security, accountant })
            {
                var notice = Assert.Single(await _app.NotificationsAsync(user.Id), n => n.Title == "New help message from a visitor");
                Assert.Contains("visitor@example.com", notice.Message);
                Assert.StartsWith("/Admin/Tickets/Detail/", notice.LinkUrl);
            }

            var page = await WithAsync(s => s.GetPageAsync(new TicketFilterDto { Search = "visitor@example.com" }));
            var item = Assert.Single(page.Data!.Items);
            Assert.True(item.IsGuest);
            Assert.Equal("visitor@example.com", item.CustomerEmail);

            var reply = await WithAsync(s => s.StaffReplyAsync(support.Id, item.Id, new ReplyTicketDto { Message = "Please try again now." }));

            Assert.True(reply.IsSuccess, string.Join(";", reply.Errors));
            var email = _app.Emails.LastTo("visitor@example.com");
            Assert.NotNull(email);
            Assert.Contains("Please try again now.", email!.Body);
        }

        [Fact]
        public async Task A_visitor_cannot_keep_more_than_three_open_messages()
        {
            await _app.NewStaffAsync(Roles.CustomerSupport);
            for (var i = 0; i < 3; i++)
            {
                var ok = await WithAsync(s => s.SendHelpAsync(null, new SendHelpDto { Email = "many@example.com", Message = "Question " + i }));
                Assert.True(ok.IsSuccess, string.Join(";", ok.Errors));
            }

            var fourth = await WithAsync(s => s.SendHelpAsync(null, new SendHelpDto { Email = "many@example.com", Message = "Question 4" }));

            Assert.False(fourth.IsSuccess);
        }

        [Fact]
        public async Task A_signed_in_customer_needs_no_email_and_gets_the_answer_in_notifications()
        {
            var support = await _app.NewStaffAsync(Roles.CustomerSupport);
            var kit = await _app.NewKitAsync(0m, "Nigar", "Əliyeva");

            var first = await WithAsync(s => s.SendHelpAsync(kit.User.Id, new SendHelpDto { Message = "Where is my card?" }));
            var second = await WithAsync(s => s.SendHelpAsync(kit.User.Id, new SendHelpDto { Message = "Any news?" }));

            Assert.True(first.IsSuccess, string.Join(";", first.Errors));
            Assert.False(first.Data!.IsGuest);
            Assert.NotNull(first.Data.TicketId);
            Assert.Equal(first.Data.TicketId, second.Data!.TicketId);   // 24 saat ərzində eyni yazışma davam edir

            var reply = await WithAsync(s => s.StaffReplyAsync(support.Id, first.Data.TicketId!.Value, new ReplyTicketDto { Message = "It is on its way." }));

            Assert.True(reply.IsSuccess, string.Join(";", reply.Errors));
            var answered = Assert.Single(await _app.NotificationsAsync(kit.User.Id), n => n.Title == "We answered your message");
            Assert.Equal("/App/Support/Ticket/" + first.Data.TicketId, answered.LinkUrl);
        }
    }
}
