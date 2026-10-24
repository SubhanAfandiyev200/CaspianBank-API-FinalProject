using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Validators.Accounts;
using Xunit;

namespace CaspianBank.Tests.Validators
{
    public class AccountValidatorTests
    {
        private static RegisterDto ValidRegister()
        {
            return new RegisterDto
            {
                Email = "aysel@mail.az",
                PhoneNumber = "0501110001",
                Fin = "5KM2R9A",
                IdCardNumber = "AZE10482931",
                Name = "Aysel",
                Surname = "Məmmədova",
                Password = "Salam123!",
                VerificationToken = "token"
            };
        }

        // ---------- CheckEmail, SendOtp, ForgotPassword (eyni qayda) ----------

        [Theory]
        [InlineData("a@b.az", true)]
        [InlineData("first.last@bank.az", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("not-an-email", false)]
        [InlineData("a@", false)]
        public void Email_only_validators_check_the_address(string email, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new CheckEmailDtoValidator(), new CheckEmailDto { Email = email }));
            Assert.Equal(valid, V.IsValid(new SendOtpDtoValidator(), new SendOtpDto { Email = email }));
            Assert.Equal(valid, V.IsValid(new ForgotPasswordDtoValidator(), new ForgotPasswordDto { Email = email }));
        }

        [Fact]
        public void Email_longer_than_256_characters_is_rejected()
        {
            var email = V.Text(252) + "@b.az";      // 257 simvol
            Assert.False(V.IsValid(new CheckEmailDtoValidator(), new CheckEmailDto { Email = email }));
            Assert.False(V.IsValid(new SendOtpDtoValidator(), new SendOtpDto { Email = email }));
            Assert.False(V.IsValid(new ForgotPasswordDtoValidator(), new ForgotPasswordDto { Email = email }));
        }

        // ---------- Login ----------

        [Fact]
        public void Login_needs_email_and_password()
        {
            var validator = new LoginDtoValidator();
            Assert.True(V.IsValid(validator, new LoginDto { Email = "a@b.az", Password = "x" }));
            Assert.Contains("Enter your email.", V.Errors(validator, new LoginDto { Email = "", Password = "x" }));
            Assert.Contains("Enter a valid email address.", V.Errors(validator, new LoginDto { Email = "oops", Password = "x" }));
            Assert.Contains("Enter your password.", V.Errors(validator, new LoginDto { Email = "a@b.az", Password = "" }));
        }

        // ---------- VerifyOtp ----------

        [Theory]
        [InlineData("123456", true)]
        [InlineData("000000", true)]
        [InlineData("12345", false)]
        [InlineData("1234567", false)]
        [InlineData("12345a", false)]
        [InlineData("", false)]
        public void VerifyOtp_code_must_be_six_digits(string code, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new VerifyOtpDtoValidator(), new VerifyOtpDto { Email = "a@b.az", Code = code }));
        }

        [Fact]
        public void VerifyOtp_needs_a_valid_email()
        {
            Assert.False(V.IsValid(new VerifyOtpDtoValidator(), new VerifyOtpDto { Email = "bad", Code = "123456" }));
            Assert.False(V.IsValid(new VerifyOtpDtoValidator(), new VerifyOtpDto { Email = "", Code = "123456" }));
        }

        // ---------- ResetPassword ----------

        [Fact]
        public void ResetPassword_accepts_a_good_request()
        {
            Assert.True(V.IsValid(new ResetPasswordDtoValidator(), new ResetPasswordDto { Email = "a@b.az", Token = "t", NewPassword = "Salam123!" }));
        }

        [Fact]
        public void ResetPassword_bad_link_gives_the_expired_message()
        {
            var errors = V.Errors(new ResetPasswordDtoValidator(), new ResetPasswordDto { Email = "", Token = "", NewPassword = "Salam123!" });
            Assert.All(errors, e => Assert.Equal("This reset link is invalid or has expired.", e));
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ResetPassword_weak_password_is_rejected()
        {
            Assert.False(V.IsValid(new ResetPasswordDtoValidator(), new ResetPasswordDto { Email = "a@b.az", Token = "t", NewPassword = "weak" }));
        }

        // ---------- CheckIdentity ----------

        [Fact]
        public void CheckIdentity_accepts_registry_data()
        {
            var dto = new CheckIdentityDto { Fin = "5KM2R9A", IdCardNumber = "AZE10482931", Name = "Aysel", Surname = "Məmmədova" };
            Assert.True(V.IsValid(new CheckIdentityDtoValidator(), dto));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("5KM2R9", false)]       // 6 simvol
        [InlineData("5KM2R9AA", false)]     // 8 simvol
        [InlineData("5KM2R9!", false)]      // simvol
        [InlineData("5km2r9a", true)]       // kiçik hərf qəbul olunur
        [InlineData("1234567", true)]
        public void CheckIdentity_fin_is_seven_letters_or_digits(string fin, bool valid)
        {
            var dto = new CheckIdentityDto { Fin = fin, IdCardNumber = "AZE10482931", Name = "A", Surname = "B" };
            Assert.Equal(valid, V.IsValid(new CheckIdentityDtoValidator(), dto));
        }

        [Theory]
        [InlineData("AZE10482931", true)]
        [InlineData("aze10482931", true)]
        [InlineData("AZE1048293", false)]
        [InlineData("AZE104829311", false)]
        [InlineData("AZ10482931", false)]
        [InlineData("10482931", false)]
        [InlineData("", false)]
        public void CheckIdentity_id_card_looks_like_AZE_and_eight_digits(string card, bool valid)
        {
            var dto = new CheckIdentityDto { Fin = "5KM2R9A", IdCardNumber = card, Name = "A", Surname = "B" };
            Assert.Equal(valid, V.IsValid(new CheckIdentityDtoValidator(), dto));
        }

        [Fact]
        public void CheckIdentity_needs_names_of_at_most_100_characters()
        {
            var validator = new CheckIdentityDtoValidator();
            Assert.False(V.IsValid(validator, new CheckIdentityDto { Fin = "5KM2R9A", IdCardNumber = "AZE10482931", Name = "", Surname = "B" }));
            Assert.False(V.IsValid(validator, new CheckIdentityDto { Fin = "5KM2R9A", IdCardNumber = "AZE10482931", Name = "A", Surname = "" }));
            Assert.False(V.IsValid(validator, new CheckIdentityDto { Fin = "5KM2R9A", IdCardNumber = "AZE10482931", Name = V.Text(101), Surname = "B" }));
            Assert.True(V.IsValid(validator, new CheckIdentityDto { Fin = "5KM2R9A", IdCardNumber = "AZE10482931", Name = V.Text(100), Surname = V.Text(100) }));
        }

        // ---------- Register ----------

        [Fact]
        public void Register_accepts_a_complete_request()
        {
            Assert.True(V.IsValid(new RegisterDtoValidator(), ValidRegister()));
        }

        [Theory]
        [InlineData("0501110001", true)]
        [InlineData("501110001", true)]
        [InlineData("0777610606", true)]
        [InlineData("050111000", false)]       // 9 rəqəm, 0 ilə
        [InlineData("05011100011", false)]     // 11 rəqəm
        [InlineData("0 501110001", false)]
        [InlineData("+994501110001", false)]
        [InlineData("abcdefghij", false)]
        [InlineData("", false)]
        public void Register_phone_is_a_ten_digit_number_with_zero_or_nine_digits_without(string phone, bool valid)
        {
            var dto = ValidRegister();
            dto.PhoneNumber = phone;
            Assert.Equal(valid, V.IsValid(new RegisterDtoValidator(), dto));
        }

        [Theory]
        [InlineData("Salam123!", true)]
        [InlineData("Ab1!xy", true)]
        [InlineData("Ab1!x", false)]           // 5 simvol
        [InlineData("salam123!", false)]       // böyük hərf yoxdur
        [InlineData("SALAM123!", false)]       // kiçik hərf yoxdur
        [InlineData("Salamabc!", false)]       // rəqəm yoxdur
        [InlineData("Salam1234", false)]       // simvol yoxdur
        [InlineData("", false)]
        public void Register_password_follows_the_identity_rules(string password, bool valid)
        {
            var dto = ValidRegister();
            dto.Password = password;
            Assert.Equal(valid, V.IsValid(new RegisterDtoValidator(), dto));
        }

        [Fact]
        public void Register_needs_the_email_verification_token()
        {
            var dto = ValidRegister();
            dto.VerificationToken = "";
            Assert.Contains("Email verification is required.", V.Errors(new RegisterDtoValidator(), dto));
        }

        [Fact]
        public void Register_needs_email_names_fin_and_card()
        {
            var validator = new RegisterDtoValidator();
            var dto = ValidRegister();
            dto.Email = "";
            dto.Name = "";
            dto.Surname = "";
            dto.Fin = "";
            dto.IdCardNumber = "";
            var errors = V.Errors(validator, dto);
            Assert.Contains("Enter your email.", errors);
            Assert.Contains("Enter your first name.", errors);
            Assert.Contains("Enter your last name.", errors);
            Assert.Contains("Enter the 7-character FIN code.", errors);
            Assert.Contains("Enter your ID card number.", errors);
        }

        [Fact]
        public void Register_fin_and_card_formats_are_checked()
        {
            var validator = new RegisterDtoValidator();
            var dto = ValidRegister();
            dto.Fin = "SHORT";
            dto.IdCardNumber = "12345";
            var errors = V.Errors(validator, dto);
            Assert.Contains("The FIN code must be 7 letters or digits.", errors);
            Assert.Contains("The ID card number looks like AZE12345678.", errors);
        }
    }
}
