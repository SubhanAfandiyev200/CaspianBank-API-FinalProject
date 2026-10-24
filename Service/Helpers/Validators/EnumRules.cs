namespace Service.Helpers.Validators
{
    // Mətn kimi gələn enum dəyərini yalnız ADLA qəbul edir. Enum.TryParse "99", "-1" və ya "Car,Mortgage" kimi yazılışları da çevirir
    // (təyin olunmamış və ya birləşdirilmiş dəyər alınır), bu isə sonra səhv davranışa gətirib çıxarır (məs. ödəniş növü "99" tam bağlama sayılırdı)
    public static class EnumRules
    {
        public static bool IsName<TEnum>(string? value) where TEnum : struct, Enum
        {
            var text = value?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            return Enum.GetNames<TEnum>().Any(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));
        }
    }
}
