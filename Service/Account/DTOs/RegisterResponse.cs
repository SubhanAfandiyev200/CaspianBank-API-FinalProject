namespace Service.Account.DTOs
{
    public class RegisterResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}
