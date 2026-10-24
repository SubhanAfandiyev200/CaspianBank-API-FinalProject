using Service.Helpers.DTOs.History;
using Service.Helpers.DTOs.Loans;
using Service.Helpers.Validators.History;
using Service.Helpers.Validators.Loans;
using Xunit;

namespace CaspianBank.Tests.Validators
{
    // Mətn kimi gələn enum dəyərləri yalnız ADLA qəbul edilməlidir: "99", "-1" və ya "Car,Mortgage" kimi yazılışlar
    // .NET-də enum-a çevrilir (təyin olunmamış və ya birləşmiş dəyər), sonra servisdə səhv davranışa gətirib çıxarır
    public class EnumInputTests
    {
        public static IEnumerable<object[]> NotNames()
        {
            yield return new object[] { "99" };
            yield return new object[] { "-1" };
            yield return new object[] { "8" };
            yield return new object[] { "0" };
            yield return new object[] { "1" };
            yield return new object[] { "Car,Mortgage" };
            yield return new object[] { "Consumer, Car" };
        }

        [Theory]
        [MemberData(nameof(NotNames))]
        public void ApplyLoan_purpose_must_be_a_name(string purpose)
        {
            var dto = new ApplyLoanDto { Amount = 1000m, Purpose = purpose, TermMonths = 12, TargetCardId = 1 };
            Assert.False(V.IsValid(new ApplyLoanDtoValidator(), dto));
        }

        [Theory]
        [InlineData("Consumer")]
        [InlineData("car")]
        [InlineData(" Mortgage ")]
        [InlineData("OTHER")]
        public void ApplyLoan_accepts_real_purpose_names(string purpose)
        {
            var dto = new ApplyLoanDto { Amount = 1000m, Purpose = purpose, TermMonths = 12, TargetCardId = 1, PurposeNote = "some details" };
            Assert.True(V.IsValid(new ApplyLoanDtoValidator(), dto));
        }

        [Theory]
        [MemberData(nameof(NotNames))]
        [InlineData("3")]
        [InlineData("Installment,FullEarly")]
        public void PayLoan_type_must_be_a_name(string type)
        {
            var dto = new PayLoanDto { CardId = 1, Type = type, Amount = 10m, RequestId = Guid.NewGuid().ToString() };
            Assert.False(V.IsValid(new PayLoanDtoValidator(), dto));
        }

        [Theory]
        [InlineData("Installment")]
        [InlineData("partialearly")]
        [InlineData("FullEarly")]
        public void PayLoan_accepts_the_real_type_names(string type)
        {
            var dto = new PayLoanDto { CardId = 1, Type = type, Amount = 10m, RequestId = Guid.NewGuid().ToString() };
            Assert.True(V.IsValid(new PayLoanDtoValidator(), dto));
        }

        [Theory]
        [InlineData("99")]
        [InlineData("-1")]
        [InlineData("1")]
        [InlineData("Pending,Active")]
        public void LoanReviewFilter_status_must_be_a_name(string status)
        {
            Assert.False(V.IsValid(new LoanReviewFilterDtoValidator(), new LoanReviewFilterDto { Status = status }));
        }

        [Theory]
        [InlineData("99")]
        [InlineData("-1")]
        [InlineData("1")]
        [InlineData("TopUp,TransferOut")]
        public void HistoryFilter_type_must_be_a_name(string type)
        {
            Assert.False(V.IsValid(new HistoryFilterDtoValidator(), new HistoryFilterDto { Type = type }));
        }
    }
}
