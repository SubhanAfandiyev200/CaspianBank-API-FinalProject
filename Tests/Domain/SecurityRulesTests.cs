using Domain.Entities;
using Domain.Security;
using Xunit;

namespace CaspianBank.Tests.DomainRules
{
    public class SecurityRulesTests
    {
        [Theory]
        [InlineData(0, null)]
        [InlineData(2999.99, null)]
        [InlineData(3000, SecurityAlertSeverity.Medium)]
        [InlineData(7999.99, SecurityAlertSeverity.Medium)]
        [InlineData(8000, SecurityAlertSeverity.High)]
        [InlineData(50000, SecurityAlertSeverity.High)]
        public void LargeTransferSeverity_follows_the_thresholds(double amount, SecurityAlertSeverity? expected)
        {
            Assert.Equal(expected, SecurityRules.LargeTransferSeverity((decimal)amount));
        }

        [Fact]
        public void IsNewAccount_is_true_before_seven_days()
        {
            var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
            Assert.True(SecurityRules.IsNewAccount(now.AddDays(-6).AddHours(-23), now));
            Assert.True(SecurityRules.IsNewAccount(now, now));
        }

        [Fact]
        public void IsNewAccount_is_false_from_exactly_seven_days()
        {
            var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
            Assert.False(SecurityRules.IsNewAccount(now.AddDays(-7), now));
            Assert.False(SecurityRules.IsNewAccount(now.AddDays(-30), now));
        }
    }
}
