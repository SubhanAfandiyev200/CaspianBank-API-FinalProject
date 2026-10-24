using Domain.Bills;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace CaspianBank.Tests.DomainRules
{
    public class BillingEngineTests
    {
        [Theory]
        [InlineData("055 123 45 67", "0551234567")]
        [InlineData("+994 55 123 45 67", "0551234567")]
        [InlineData("+994551234567", "0551234567")]
        [InlineData("994551234567", "0551234567")]
        [InlineData("551234567", "0551234567")]
        [InlineData("0551234567", "0551234567")]
        [InlineData("(055) 123-45-67", "0551234567")]
        public void NormalizePhone_accepts_the_usual_ways_to_write_a_number(string input, string expected)
        {
            Assert.Equal(expected, BillingEngine.NormalizePhone(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("12345")]
        [InlineData("abcdefghij")]
        [InlineData("05512345678")]        // 11 rəqəm
        [InlineData("1551234567")]         // 10 rəqəm, 0 ilə başlamır
        [InlineData("99455123456")]        // 994 var, uzunluq səhvdir
        public void NormalizePhone_returns_null_for_things_that_are_not_numbers(string? input)
        {
            Assert.Null(BillingEngine.NormalizePhone(input));
        }

        [Fact]
        public void PhonePrefix_is_the_two_digits_after_zero()
        {
            Assert.Equal("55", BillingEngine.PhonePrefix("0551234567"));
            Assert.Equal("77", BillingEngine.PhonePrefix("0771234567"));
        }

        [Fact]
        public void FormatPhone_groups_the_digits()
        {
            Assert.Equal("055 123 45 67", BillingEngine.FormatPhone("0551234567"));
        }

        [Fact]
        public void PrefixBelongs_checks_the_provider_list()
        {
            Assert.True(BillingEngine.PrefixBelongs("70,77", "77"));
            Assert.True(BillingEngine.PrefixBelongs("70, 77", "77"));
            Assert.False(BillingEngine.PrefixBelongs("70,77", "55"));
            Assert.False(BillingEngine.PrefixBelongs("", "55"));
            Assert.False(BillingEngine.PrefixBelongs(null, "55"));
        }

        [Fact]
        public void PrefixesText_writes_a_readable_list()
        {
            Assert.Equal("077", BillingEngine.PrefixesText("77"));
            Assert.Equal("070 or 077", BillingEngine.PrefixesText("70,77"));
            Assert.Equal("050, 051 or 010", BillingEngine.PrefixesText("50,51,10"));
            Assert.Equal(string.Empty, BillingEngine.PrefixesText(null));
        }

        [Fact]
        public void PeriodOf_is_year_and_month()
        {
            Assert.Equal("2026-10", BillingEngine.PeriodOf(new DateTime(2026, 10, 3)));
        }

        [Fact]
        public void Round_uses_two_decimals_away_from_zero()
        {
            Assert.Equal(1.01m, BillingEngine.Round(1.005m));
            Assert.Equal(1.00m, BillingEngine.Round(1.004m));
        }

        [Fact]
        public void Mobile_numbers_have_no_monthly_charge()
        {
            var provider = new BillProvider { Category = BillCategory.Mobile };
            var subscriber = new BillSubscriber { Id = 1 };
            var charge = BillingEngine.ComputeCharge(provider, subscriber, "2026-10");
            Assert.Equal(0m, charge.Amount);
            Assert.Null(charge.Usage);
            Assert.Equal(string.Empty, charge.Description);
        }

        [Fact]
        public void Internet_charge_is_the_package_fee()
        {
            var provider = new BillProvider { Category = BillCategory.Internet };
            var subscriber = new BillSubscriber { Id = 1, MonthlyFee = 30m, PackageName = "Home 30" };
            var charge = BillingEngine.ComputeCharge(provider, subscriber, "2026-10");
            Assert.Equal(30m, charge.Amount);
            Assert.Null(charge.Usage);
            Assert.Equal("Home 30 · 2026-10", charge.Description);
        }

        [Fact]
        public void Internet_charge_without_a_package_falls_back_to_a_generic_name()
        {
            var provider = new BillProvider { Category = BillCategory.Internet };
            var subscriber = new BillSubscriber { Id = 1, MonthlyFee = null, PackageName = null };
            var charge = BillingEngine.ComputeCharge(provider, subscriber, "2026-10");
            Assert.Equal(0m, charge.Amount);
            Assert.Equal("Internet · 2026-10", charge.Description);
        }

        [Fact]
        public void Utility_charge_is_usage_times_the_unit_price()
        {
            var provider = new BillProvider { Category = BillCategory.Electricity, UnitPrice = 0.10m, UnitName = "kWh" };
            var subscriber = new BillSubscriber { Id = 3, AverageUsage = 200m };
            var charge = BillingEngine.ComputeCharge(provider, subscriber, "2026-10");

            var expectedUsage = BillingEngine.Round(200m * BillingEngine.SeasonFactor(BillCategory.Electricity, 10) * BillingEngine.Variation(3, 2026, 10));
            Assert.Equal(expectedUsage, charge.Usage);
            Assert.Equal(BillingEngine.Round(expectedUsage * 0.10m), charge.Amount);
            Assert.EndsWith("kWh · 2026-10", charge.Description);
        }

        [Fact]
        public void Utility_charge_is_the_same_every_time_for_the_same_subscriber_and_month()
        {
            var provider = new BillProvider { Category = BillCategory.Gas, UnitPrice = 0.25m, UnitName = "m³" };
            var subscriber = new BillSubscriber { Id = 42, AverageUsage = 80m };
            var first = BillingEngine.ComputeCharge(provider, subscriber, "2026-01");
            var second = BillingEngine.ComputeCharge(provider, subscriber, "2026-01");
            Assert.Equal(first, second);
        }

        [Fact]
        public void Utility_charge_without_a_unit_name_says_units()
        {
            var provider = new BillProvider { Category = BillCategory.Water, UnitPrice = 1m, UnitName = null };
            var subscriber = new BillSubscriber { Id = 1, AverageUsage = 10m };
            var charge = BillingEngine.ComputeCharge(provider, subscriber, "2026-05");
            Assert.Contains("units", charge.Description);
        }

        [Fact]
        public void Utility_charge_without_average_usage_is_zero()
        {
            var provider = new BillProvider { Category = BillCategory.Water, UnitPrice = 1m, UnitName = "m³" };
            var subscriber = new BillSubscriber { Id = 1, AverageUsage = null };
            var charge = BillingEngine.ComputeCharge(provider, subscriber, "2026-05");
            Assert.Equal(0m, charge.Amount);
            Assert.Equal(0m, charge.Usage);
        }

        [Theory]
        [InlineData(BillCategory.Gas, 1, 1.8)]
        [InlineData(BillCategory.Gas, 12, 1.8)]
        [InlineData(BillCategory.Gas, 3, 1.4)]
        [InlineData(BillCategory.Gas, 11, 1.4)]
        [InlineData(BillCategory.Gas, 4, 1.0)]
        [InlineData(BillCategory.Gas, 10, 1.0)]
        [InlineData(BillCategory.Gas, 7, 0.35)]
        [InlineData(BillCategory.Electricity, 7, 1.15)]
        [InlineData(BillCategory.Electricity, 1, 1.1)]
        [InlineData(BillCategory.Electricity, 5, 1.0)]
        [InlineData(BillCategory.Water, 8, 1.15)]
        [InlineData(BillCategory.Water, 3, 1.0)]
        [InlineData(BillCategory.Internet, 1, 1.0)]
        [InlineData(BillCategory.Mobile, 7, 1.0)]
        public void SeasonFactor_follows_the_season(BillCategory category, int month, double expected)
        {
            Assert.Equal((decimal)expected, BillingEngine.SeasonFactor(category, month));
        }

        [Fact]
        public void Variation_stays_between_ninety_and_one_hundred_ten_percent()
        {
            for (var id = 1; id <= 200; id++)
            {
                for (var month = 1; month <= 12; month++)
                {
                    var value = BillingEngine.Variation(id, 2026, month);
                    Assert.InRange(value, 0.90m, 1.10m);
                }
            }
        }

        [Fact]
        public void Variation_is_deterministic()
        {
            Assert.Equal(BillingEngine.Variation(5, 2026, 3), BillingEngine.Variation(5, 2026, 3));
        }
    }
}
