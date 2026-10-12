namespace Service.Helpers.DTOs.Accounts
{
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // Vətəndaşı reyestrdə tapmaq üçün. Hamısı eyni vətəndaşa uyğun olmalıdır; hesaba yazılan ad, soyad və doğum tarixi reyestrdən gəlir
        public string Fin { get; set; } = string.Empty;
        public string IdCardNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        // verify-otp-dan qayıdan token: emailin təsdiqləndiyini sübut edir
        public string VerificationToken { get; set; } = string.Empty;
    }
}
