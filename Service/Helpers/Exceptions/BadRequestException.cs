namespace Service.Helpers.Exceptions
{
    // İstifadəçinin göndərdiyi məlumat yanlışdır (məs. dəstəklənməyən fayl). Middleware 400 qaytarır
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message) { }
    }
}
