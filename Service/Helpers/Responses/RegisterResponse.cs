namespace Service.Helpers.Responses
{
    public class RegisterResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}
