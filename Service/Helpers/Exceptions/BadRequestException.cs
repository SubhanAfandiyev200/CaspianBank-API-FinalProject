namespace Service.Helpers.Exceptions
{
    // İstifadəçinin göndərdiyi məlumat yanlışdır (məs. boş ad, dəstəklənməyən fayl). Middleware 400 qaytarır
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
            Errors = new[] { message };
        }

        // Validator bir neçə xəta tapıbsa hamısı birlikdə qaytarılır
        public BadRequestException(string[] errors) : base(errors.Length > 0 ? errors[0] : "The request is not valid.")
        {
            Errors = errors.Length > 0 ? errors : new[] { "The request is not valid." };
        }

        public string[] Errors { get; }
    }
}
