using Domain.Transfers;
using Xunit;

namespace CaspianBank.Tests.DomainRules
{
    public class TransferFeesTests
    {
        [Theory]
        [InlineData(0, 400, 500, 0)]        // limitin içində
        [InlineData(0, 500, 500, 0)]        // dəqiq limit: hələ komissiya yoxdur
        [InlineData(0, 600, 500, 100)]      // yalnız limiti keçən hissə
        [InlineData(450, 100, 500, 50)]     // limit köçürmənin ortasında keçilir
        [InlineData(600, 100, 500, 100)]    // limit artıq keçilib: tam məbləğ
        [InlineData(500, 1, 500, 1)]
        [InlineData(0, 0, 500, 0)]
        [InlineData(0, 100, 0, 100)]        // limit yoxdur: hamısı komissiyalı
        public void ChargeableAmount_is_only_the_part_above_the_monthly_limit(double used, double amount, double limit, double expected)
        {
            Assert.Equal((decimal)expected, TransferFees.ChargeableAmount((decimal)used, (decimal)amount, (decimal)limit));
        }

        [Fact]
        public void Commission_of_the_documented_example_is_one_manat()
        {
            // Standard: limit 500, komissiya 1%: 600 göndərilir -> artıq 100-ə 1.00
            Assert.Equal(1.00m, TransferFees.Commission(0m, 600m, 500m, 1m));
        }

        [Fact]
        public void Commission_is_zero_inside_the_limit()
        {
            Assert.Equal(0m, TransferFees.Commission(100m, 300m, 500m, 1m));
        }

        [Fact]
        public void Commission_rounds_half_away_from_zero()
        {
            // 0.5 * 1% = 0.005 -> 0.01
            Assert.Equal(0.01m, TransferFees.Commission(0m, 500.5m, 500m, 1m));
        }

        [Fact]
        public void Commission_charges_the_whole_amount_after_the_limit_is_used()
        {
            Assert.Equal(2.00m, TransferFees.Commission(1000m, 200m, 500m, 1m));
        }

        [Fact]
        public void Commission_is_zero_when_the_percent_is_zero()
        {
            Assert.Equal(0m, TransferFees.Commission(1000m, 200m, 500m, 0m));
        }

        [Fact]
        public void Successive_transfers_pay_the_same_total_as_one_big_transfer()
        {
            var first = TransferFees.Commission(0m, 300m, 500m, 1m);
            var second = TransferFees.Commission(300m, 300m, 500m, 1m);
            var single = TransferFees.Commission(0m, 600m, 500m, 1m);
            Assert.Equal(single, first + second);
        }
    }
}
