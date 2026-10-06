namespace Service.Helpers.DTOs.Accounts
{
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public DateTime BirthDay { get; set; }
        public string Password { get; set; } = string.Empty;

        // verify-otp-dan qayıdan token: emailin təsdiqləndiyini sübut edir
        public string VerificationToken { get; set; } = string.Empty;
    }
}
