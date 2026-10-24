using Service.Helpers;
using Service.Helpers.DTOs.CardTiers;
using Service.Helpers.DTOs.Cards;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Validators.CardTiers;
using Service.Helpers.Validators.Cards;
using Service.Helpers.Validators.Transfers;
using Xunit;

namespace CaspianBank.Tests.Validators
{
    public class CardAndTransferValidatorTests
    {
        // ---------- AddCard ----------

        [Theory]
        [InlineData("Standard", true)]
        [InlineData("silver", true)]
        [InlineData("GOLD", true)]
        [InlineData("Cashback", false)]      // Cashback ayrıca açılmır
        [InlineData("Platinum", false)]
        [InlineData("", false)]
        [InlineData("7", false)]             // ədəd kimi də qəbul olunmamalıdır
        public void AddCard_tier_must_be_standard_silver_or_gold(string tier, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new AddCardDtoValidator(), new AddCardDto { Tier = tier, FundingCardId = 1 }));
        }

        [Fact]
        public void AddCard_needs_a_funding_card()
        {
            Assert.False(V.IsValid(new AddCardDtoValidator(), new AddCardDto { Tier = "Gold", FundingCardId = 0 }));
            Assert.False(V.IsValid(new AddCardDtoValidator(), new AddCardDto { Tier = "Gold", FundingCardId = -3 }));
        }

        // ---------- Block / Unblock ----------

        [Theory]
        [InlineData("123456", true)]
        [InlineData("12345", false)]
        [InlineData("1234567", false)]
        [InlineData("abcdef", false)]
        [InlineData("", false)]
        public void Block_and_unblock_code_is_six_digits(string code, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new BlockCardDtoValidator(), new BlockCardDto { Code = code }));
            Assert.Equal(valid, V.IsValid(new UnblockCardDtoValidator(), new UnblockCardDto { Code = code }));
        }

        // ---------- TopUp ----------

        private static TopUpDto ValidTopUp()
        {
            return new TopUpDto { Amount = 100m, SourceCardNumber = "4111 1111 1111 1111", Expiry = DateTime.UtcNow.AddYears(2).ToString("MM/yy"), Cvv = "123" };
        }

        [Fact]
        public void TopUp_accepts_a_valid_demo_card()
        {
            Assert.True(V.IsValid(new TopUpDtoValidator(), ValidTopUp()));
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(5000, true)]
        [InlineData(0.99, false)]
        [InlineData(5000.01, false)]
        [InlineData(0, false)]
        [InlineData(-5, false)]
        [InlineData(10.123, false)]
        [InlineData(10.12, true)]
        public void TopUp_amount_is_between_one_and_five_thousand_with_two_decimals(double amount, bool valid)
        {
            var dto = ValidTopUp();
            dto.Amount = (decimal)amount;
            Assert.Equal(valid, V.IsValid(new TopUpDtoValidator(), dto));
        }

        [Theory]
        [InlineData("4111111111111111", true)]
        [InlineData("4111-1111-1111-1111", true)]
        [InlineData("4111111111111112", false)]      // Luhn səhvdir
        [InlineData("411111111111", false)]          // qısa
        [InlineData("", false)]
        [InlineData("abcdabcdabcdabcd", false)]
        public void TopUp_card_number_must_pass_the_luhn_check(string number, bool valid)
        {
            var dto = ValidTopUp();
            dto.SourceCardNumber = number;
            Assert.Equal(valid, V.IsValid(new TopUpDtoValidator(), dto));
        }

        [Fact]
        public void TopUp_expiry_must_not_have_passed()
        {
            var validator = new TopUpDtoValidator();
            var dto = ValidTopUp();

            dto.Expiry = DateTime.UtcNow.AddMonths(-2).ToString("MM/yy");
            Assert.False(V.IsValid(validator, dto));

            dto.Expiry = DateTime.UtcNow.ToString("MM/yy");      // bu ay hələ etibarlıdır
            Assert.True(V.IsValid(validator, dto));
        }

        [Theory]
        [InlineData("13/30")]
        [InlineData("1230")]
        [InlineData("12/2030")]
        [InlineData("")]
        [InlineData("ab/cd")]
        public void TopUp_expiry_format_is_MM_slash_YY(string expiry)
        {
            var dto = ValidTopUp();
            dto.Expiry = expiry;
            Assert.False(V.IsValid(new TopUpDtoValidator(), dto));
        }

        [Theory]
        [InlineData("123", true)]
        [InlineData("1234", true)]
        [InlineData("12", false)]
        [InlineData("12345", false)]
        [InlineData("12a", false)]
        [InlineData("", false)]
        public void TopUp_cvv_is_three_or_four_digits(string cvv, bool valid)
        {
            var dto = ValidTopUp();
            dto.Cvv = cvv;
            Assert.Equal(valid, V.IsValid(new TopUpDtoValidator(), dto));
        }

        [Fact]
        public void TopUp_Digits_keeps_only_ascii_digits()
        {
            Assert.Equal("4111111111111111", TopUpDtoValidator.Digits("4111 1111-1111 1111"));
            Assert.Equal(string.Empty, TopUpDtoValidator.Digits(null!));
        }

        // ---------- Kart nömrəsi ----------

        [Fact]
        public void Generated_card_numbers_are_sixteen_digits_start_with_the_bin_and_pass_luhn()
        {
            for (var i = 0; i < 200; i++)
            {
                var number = CardNumberGenerator.Generate();
                Assert.Equal(16, number.Length);
                Assert.StartsWith(CardNumberGenerator.Prefix, number);
                Assert.True(CardNumberGenerator.IsValid(number));
            }
        }

        [Fact]
        public void Generated_card_numbers_are_not_all_the_same()
        {
            var numbers = Enumerable.Range(0, 50).Select(_ => CardNumberGenerator.Generate()).Distinct().Count();
            Assert.True(numbers > 40);
        }

        [Theory]
        [InlineData("4111111111111111", true)]
        [InlineData("5555555555554444", true)]
        [InlineData("378282246310005", true)]          // 15 rəqəm (Amex)
        [InlineData("4111111111111112", false)]
        [InlineData("123456789012", false)]            // 12 rəqəm
        [InlineData("12345678901234567890", false)]    // 20 rəqəm
        [InlineData("4111x11111111111", false)]
        public void IsValid_checks_length_digits_and_luhn(string number, bool expected)
        {
            Assert.Equal(expected, CardNumberGenerator.IsValid(number));
        }

        // ---------- Transfer ----------

        private static TransferDto OwnCardTransfer()
        {
            return new TransferDto { FromCardId = 1, ToCardId = 2, Amount = 10m, RequestId = Guid.NewGuid().ToString() };
        }

        [Fact]
        public void Transfer_to_an_own_card_is_valid()
        {
            Assert.True(V.IsValid(new TransferDtoValidator(), OwnCardTransfer()));
        }

        [Fact]
        public void Transfer_to_a_card_number_is_valid()
        {
            var dto = new TransferDto { FromCardId = 1, ToCardNumber = "9630 1020 1234 5678", Amount = 10m };
            Assert.True(V.IsValid(new TransferDtoValidator(), dto));
        }

        [Fact]
        public void Transfer_needs_exactly_one_recipient()
        {
            var validator = new TransferDtoValidator();

            var none = new TransferDto { FromCardId = 1, Amount = 10m };
            Assert.Contains("Choose one of your cards or enter the recipient's card number.", V.Errors(validator, none));

            var both = new TransferDto { FromCardId = 1, ToCardId = 2, ToCardNumber = "9630102012345678", Amount = 10m };
            Assert.Contains("Choose one of your cards or enter the recipient's card number.", V.Errors(validator, both));
        }

        [Theory]
        [InlineData("96301020123456")]       // 14 rəqəm
        [InlineData("96301020123456789")]    // 17 rəqəm
        [InlineData("abcdabcdabcdabcd")]
        public void Transfer_card_number_must_have_sixteen_digits(string number)
        {
            var dto = new TransferDto { FromCardId = 1, ToCardNumber = number, Amount = 10m };
            Assert.Contains("Enter a 16-digit card number.", V.Errors(new TransferDtoValidator(), dto));
        }

        [Theory]
        [InlineData(0.01, true)]
        [InlineData(100000, true)]
        [InlineData(100000.01, false)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(1.005, false)]
        public void Transfer_amount_is_positive_at_most_100000_with_two_decimals(double amount, bool valid)
        {
            var dto = OwnCardTransfer();
            dto.Amount = (decimal)amount;
            Assert.Equal(valid, V.IsValid(new TransferDtoValidator(), dto));
        }

        [Fact]
        public void Transfer_needs_a_source_card()
        {
            var dto = OwnCardTransfer();
            dto.FromCardId = 0;
            Assert.Contains("Choose a card to send from.", V.Errors(new TransferDtoValidator(), dto));
        }

        [Fact]
        public void Transfer_note_is_at_most_140_characters()
        {
            var dto = OwnCardTransfer();
            dto.Note = V.Text(140);
            Assert.True(V.IsValid(new TransferDtoValidator(), dto));
            dto.Note = V.Text(141);
            Assert.False(V.IsValid(new TransferDtoValidator(), dto));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("not-a-guid", false)]
        public void Transfer_request_id_is_empty_or_a_guid(string? requestId, bool valid)
        {
            var dto = OwnCardTransfer();
            dto.RequestId = requestId;
            Assert.Equal(valid, V.IsValid(new TransferDtoValidator(), dto));
        }

        // ---------- Kart növünün qaydaları ----------

        private static CardTierUpdateDto ValidTier()
        {
            return new CardTierUpdateDto { IssueFee = 5m, CashbackPercent = 2m, TransferLimit = 500m, CommissionPercent = 1m, CardDesignId = 1 };
        }

        [Fact]
        public void CardTier_accepts_normal_values()
        {
            Assert.True(V.IsValid(new UpdateCardTierDtoValidator(), ValidTier()));
        }

        [Fact]
        public void CardTier_amounts_cannot_be_empty()
        {
            var dto = new CardTierUpdateDto();
            var errors = V.Errors(new UpdateCardTierDtoValidator(), dto);
            Assert.Contains("Enter the issue fee.", errors);
            Assert.Contains("Enter the cashback percent.", errors);
            Assert.Contains("Enter the transfer limit.", errors);
            Assert.Contains("Enter the commission percent.", errors);
            Assert.Contains("Choose a card design.", errors);
        }

        [Fact]
        public void CardTier_limits_are_enforced()
        {
            var validator = new UpdateCardTierDtoValidator();

            var dto = ValidTier();
            dto.IssueFee = 10000.01m;
            Assert.False(V.IsValid(validator, dto));

            dto = ValidTier();
            dto.CashbackPercent = 100.01m;
            Assert.False(V.IsValid(validator, dto));

            dto = ValidTier();
            dto.TransferLimit = 100000.01m;
            Assert.False(V.IsValid(validator, dto));

            dto = ValidTier();
            dto.CommissionPercent = -0.01m;
            Assert.False(V.IsValid(validator, dto));

            dto = ValidTier();
            dto.CommissionPercent = 1.234m;
            Assert.Contains("The commission percent can have at most 2 decimal places.", V.Errors(validator, dto));
        }

        [Fact]
        public void CardTier_boundaries_are_allowed()
        {
            var dto = new CardTierUpdateDto { IssueFee = 0m, CashbackPercent = 100m, TransferLimit = 100000m, CommissionPercent = 0m, CardDesignId = 1 };
            Assert.True(V.IsValid(new UpdateCardTierDtoValidator(), dto));
        }

        [Fact]
        public void CardTier_design_must_be_chosen()
        {
            var dto = ValidTier();
            dto.CardDesignId = 0;
            Assert.False(V.IsValid(new UpdateCardTierDtoValidator(), dto));
        }
    }
}
