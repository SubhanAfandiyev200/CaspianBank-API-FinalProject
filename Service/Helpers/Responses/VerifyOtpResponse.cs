namespace Service.Helpers.Responses
{
    public class VerifyOtpResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();

        // Kod düzgün olanda qayıdır; Register bu tokeni tələb edir
        public string? VerificationToken { get; set; }
    }
}
