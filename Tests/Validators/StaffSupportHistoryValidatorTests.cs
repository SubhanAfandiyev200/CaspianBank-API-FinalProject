using Domain.Constants;
using Service.Helpers.DTOs.History;
using Service.Helpers.DTOs.Notifications;
using Service.Helpers.DTOs.Staff;
using Service.Helpers.DTOs.Support;
using Service.Helpers.DTOs.Users;
using Service.Helpers.Validators.History;
using Service.Helpers.Validators.Notifications;
using Service.Helpers.Validators.Staff;
using Service.Helpers.Validators.Support;
using Service.Helpers.Validators.Users;
using Xunit;

namespace CaspianBank.Tests.Validators
{
    public class StaffSupportHistoryValidatorTests
    {
        // ---------- Staff ----------

        [Theory]
        [InlineData("Admin", true)]
        [InlineData("Accountant", true)]
        [InlineData("CustomerSupport", true)]
        [InlineData("Security", true)]
        [InlineData(" Security ", true)]
        [InlineData("SuperAdmin", false)]
        [InlineData("Customer", false)]
        [InlineData("admin", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void StaffRules_only_the_four_roles_can_be_assigned(string? role, bool expected)
        {
            Assert.Equal(expected, StaffRules.IsAssignable(role));
        }

        [Fact]
        public void StaffRules_assignable_roles_never_include_super_admin()
        {
            Assert.DoesNotContain(Roles.SuperAdmin, StaffRules.AssignableRoles);
            Assert.Equal(4, StaffRules.AssignableRoles.Length);
        }

        private static AddStaffDto ValidStaff()
        {
            return new AddStaffDto { Name = "Nurlan", Surname = "Efendiyev", Email = "nurlan@caspianbank.az", Role = Roles.Admin };
        }

        [Fact]
        public void AddStaff_accepts_a_normal_staff_member()
        {
            Assert.True(V.IsValid(new AddStaffDtoValidator(), ValidStaff()));
        }

        [Fact]
        public void AddStaff_names_are_required_and_limited_to_40_characters()
        {
            var validator = new AddStaffDtoValidator();
            var dto = ValidStaff();
            dto.Name = "";
            Assert.Contains("Enter the first name.", V.Errors(validator, dto));

            dto = ValidStaff();
            dto.Surname = "";
            Assert.Contains("Enter the last name.", V.Errors(validator, dto));

            dto = ValidStaff();
            dto.Name = V.Text(41);
            Assert.False(V.IsValid(validator, dto));
            dto.Name = V.Text(40);
            Assert.True(V.IsValid(validator, dto));
        }

        [Theory]
        [InlineData("a@b.az", true)]
        [InlineData("first.last@bank.az", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("plain", false)]
        [InlineData("a@b", false)]               // nöqtə yoxdur
        [InlineData("Name <a@b.az>", false)]     // ad ilə ünvan
        [InlineData("a@b.az, c@d.az", false)]
        public void AddStaff_email_must_be_a_plain_address(string email, bool valid)
        {
            var dto = ValidStaff();
            dto.Email = email;
            Assert.Equal(valid, V.IsValid(new AddStaffDtoValidator(), dto));
        }

        [Fact]
        public void AddStaff_email_is_at_most_100_characters()
        {
            var dto = ValidStaff();
            dto.Email = V.Text(96) + "@b.az";
            Assert.False(V.IsValid(new AddStaffDtoValidator(), dto));
        }

        [Fact]
        public void AddStaff_role_must_be_assignable()
        {
            var dto = ValidStaff();
            dto.Role = Roles.SuperAdmin;
            Assert.Contains("Choose Admin, Accountant, Customer support or Security.", V.Errors(new AddStaffDtoValidator(), dto));
        }

        [Fact]
        public void ChangeStaffRole_only_accepts_assignable_roles()
        {
            var validator = new ChangeStaffRoleDtoValidator();
            Assert.True(V.IsValid(validator, new ChangeStaffRoleDto { Role = Roles.Security }));
            Assert.False(V.IsValid(validator, new ChangeStaffRoleDto { Role = Roles.SuperAdmin }));
            Assert.False(V.IsValid(validator, new ChangeStaffRoleDto { Role = "" }));
        }

        // ---------- Support ----------

        [Theory]
        [InlineData("abc", "hello", true)]
        [InlineData("ab", "hello", false)]          // mövzu 3-dən qısa
        [InlineData("abc", "hell", false)]          // mesaj 5-dən qısa
        [InlineData("", "hello", false)]
        [InlineData("abc", "", false)]
        [InlineData(null, "hello", false)]
        [InlineData("abc", null, false)]
        public void CreateTicket_subject_and_message_have_minimum_lengths(string? subject, string? message, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new CreateTicketDtoValidator(), new CreateTicketDto { Subject = subject, Message = message }));
        }

        [Fact]
        public void CreateTicket_maximum_lengths_are_120_and_2000()
        {
            var validator = new CreateTicketDtoValidator();
            Assert.True(V.IsValid(validator, new CreateTicketDto { Subject = V.Text(120), Message = V.Text(2000) }));
            Assert.False(V.IsValid(validator, new CreateTicketDto { Subject = V.Text(121), Message = "hello" }));
            Assert.False(V.IsValid(validator, new CreateTicketDto { Subject = "abc", Message = V.Text(2001) }));
        }

        [Theory]
        [InlineData("ok", true)]
        [InlineData("o", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void ReplyTicket_message_is_at_least_two_characters(string? message, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new ReplyTicketDtoValidator(), new ReplyTicketDto { Message = message }));
        }

        [Fact]
        public void ReplyTicket_message_is_at_most_2000_characters()
        {
            Assert.True(V.IsValid(new ReplyTicketDtoValidator(), new ReplyTicketDto { Message = V.Text(2000) }));
            Assert.False(V.IsValid(new ReplyTicketDtoValidator(), new ReplyTicketDto { Message = V.Text(2001) }));
        }

        // ---------- Restrict ----------

        [Theory]
        [InlineData("Fraud suspected", true)]
        [InlineData("abcde", true)]
        [InlineData("abcd", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void RestrictUser_reason_is_at_least_five_characters(string? reason, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new RestrictUserDtoValidator(), new RestrictUserDto { Reason = reason }));
        }

        [Fact]
        public void RestrictUser_reason_is_at_most_300_characters()
        {
            Assert.True(V.IsValid(new RestrictUserDtoValidator(), new RestrictUserDto { Reason = V.Text(300) }));
            Assert.False(V.IsValid(new RestrictUserDtoValidator(), new RestrictUserDto { Reason = V.Text(301) }));
        }

        // ---------- History ----------

        [Fact]
        public void HistoryFilter_defaults_are_valid()
        {
            Assert.True(V.IsValid(new HistoryFilterDtoValidator(), new HistoryFilterDto()));
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(5, 50, true)]
        [InlineData(0, 20, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 51, false)]
        public void HistoryFilter_paging_is_limited(int page, int pageSize, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new HistoryFilterDtoValidator(), new HistoryFilterDto { Page = page, PageSize = pageSize }));
        }

        [Theory]
        [InlineData("TopUp", true)]
        [InlineData("transferout", true)]
        [InlineData("DepositPayout", true)]
        [InlineData("", true)]
        [InlineData("Refund", false)]
        public void HistoryFilter_type_is_empty_or_a_transaction_type(string type, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new HistoryFilterDtoValidator(), new HistoryFilterDto { Type = type }));
        }

        [Theory]
        [InlineData("in", true)]
        [InlineData("OUT", true)]
        [InlineData("", true)]
        [InlineData("sideways", false)]
        public void HistoryFilter_direction_is_in_or_out(string direction, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new HistoryFilterDtoValidator(), new HistoryFilterDto { Direction = direction }));
        }

        [Fact]
        public void HistoryFilter_search_is_at_most_50_characters()
        {
            Assert.True(V.IsValid(new HistoryFilterDtoValidator(), new HistoryFilterDto { Search = V.Text(50) }));
            Assert.False(V.IsValid(new HistoryFilterDtoValidator(), new HistoryFilterDto { Search = V.Text(51) }));
        }

        [Fact]
        public void HistoryFilter_start_date_cannot_be_after_the_end_date()
        {
            var validator = new HistoryFilterDtoValidator();
            Assert.True(V.IsValid(validator, new HistoryFilterDto { From = new DateTime(2026, 10, 1), To = new DateTime(2026, 10, 1) }));
            Assert.True(V.IsValid(validator, new HistoryFilterDto { From = new DateTime(2026, 10, 1), To = null }));
            Assert.False(V.IsValid(validator, new HistoryFilterDto { From = new DateTime(2026, 10, 2), To = new DateTime(2026, 10, 1) }));
        }

        // ---------- Statement ----------

        [Fact]
        public void Statement_needs_card_and_dates()
        {
            var validator = new StatementFilterDtoValidator();
            var errors = V.Errors(validator, new StatementFilterDto());
            Assert.Contains("Choose a card.", errors);
            Assert.Contains("Choose the start date.", errors);
            Assert.Contains("Choose the end date.", errors);
        }

        [Fact]
        public void Statement_range_is_at_most_one_year()
        {
            var validator = new StatementFilterDtoValidator();
            var from = new DateTime(2026, 1, 1);

            Assert.True(V.IsValid(validator, new StatementFilterDto { CardId = 1, From = from, To = from.AddDays(365) }));
            Assert.False(V.IsValid(validator, new StatementFilterDto { CardId = 1, From = from, To = from.AddDays(366) }));
        }

        [Fact]
        public void Statement_start_cannot_be_after_the_end()
        {
            var validator = new StatementFilterDtoValidator();
            Assert.False(V.IsValid(validator, new StatementFilterDto { CardId = 1, From = new DateTime(2026, 5, 2), To = new DateTime(2026, 5, 1) }));
            Assert.True(V.IsValid(validator, new StatementFilterDto { CardId = 1, From = new DateTime(2026, 5, 1), To = new DateTime(2026, 5, 1) }));
        }

        // ---------- Notifications ----------

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(1, 50, true)]
        [InlineData(0, 20, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 51, false)]
        public void NotificationFilter_paging_is_limited(int page, int pageSize, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new NotificationFilterDtoValidator(), new NotificationFilterDto { Page = page, PageSize = pageSize }));
        }
    }
}
