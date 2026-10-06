namespace Service.Helpers.Responses
{
    public class CheckEmailResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public bool Exists { get; set; }
    }
}
