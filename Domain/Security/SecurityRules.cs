using Domain.Entities;

namespace Domain.Security
{
    // Şübhəli fəaliyyət qaydalarının hədləri bir yerdədir: dəyişmək üçün yalnız bura baxmaq kifayətdir.
    // Qaydalar yalnız başqasına (başqa müştəriyə və ya başqa banka) göndərilən pula aiddir; öz kartları arasında köçürmə sayılmır
    public static class SecurityRules
    {
        public const decimal LargeTransferMedium = 3000m;       // bu və ya çox: orta
        public const decimal LargeTransferHigh = 8000m;         // bu və ya çox: yüksək

        public const int RapidTransferCount = 5;                // bu qədər köçürmə...
        public const int RapidTransferMinutes = 10;             // ...bu müddətdə: orta

        public const decimal DailyVolumeHigh = 15000m;          // Bakı günü ərzində göndərilən cəm: yüksək

        public const int NewAccountDays = 7;                    // bundan gənc hesab
        public const decimal NewAccountAmount = 1000m;          // bu və ya çox köçürsə: orta

        public static SecurityAlertSeverity? LargeTransferSeverity(decimal amount)
        {
            if (amount >= LargeTransferHigh)
            {
                return SecurityAlertSeverity.High;
            }
            if (amount >= LargeTransferMedium)
            {
                return SecurityAlertSeverity.Medium;
            }
            return null;
        }

        public static bool IsNewAccount(DateTime createdAtUtc, DateTime nowUtc)
        {
            return nowUtc - createdAtUtc < TimeSpan.FromDays(NewAccountDays);
        }
    }
}
