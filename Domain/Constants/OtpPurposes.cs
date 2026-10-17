namespace Domain.Constants
{
    // Email OTP kodunun məqsədi. Kod yalnız göndərildiyi məqsəd üçün işləyir:
    // qeydiyyat kodu kartı blokdan çıxara bilmir, bir kartın kodu başqa kart üçün keçmir
    public static class OtpPurposes
    {
        public const string Register = "Register";

        public static string CardBlock(int cardId)
        {
            return "CardBlock:" + cardId;
        }

        public static string CardUnblock(int cardId)
        {
            return "CardUnblock:" + cardId;
        }
    }
}
