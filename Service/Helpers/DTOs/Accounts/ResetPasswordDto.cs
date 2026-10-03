namespace Service.Helpers.DTOs.Accounts
{
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;

        // Emaildəki linkdən gələn token (base64url ilə kodlanmış; API özü açır)
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
