namespace Service.Helpers.Responses
{
    // Məlumat qaytaran əməliyyatların nəticəsi
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public bool IsNotFound { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data)
        {
            return new()
            {
                IsSuccess = true,
                Data = data
            };
        }
        public static ServiceResult<T> Fail(params string[] errors)
        {
            return new()
            {
                Errors = errors
            };
        }
        public static ServiceResult<T> NotFound()
        {
            return new() { IsNotFound = true, Errors = new[] { "Not found." } };
        }
    }
}
