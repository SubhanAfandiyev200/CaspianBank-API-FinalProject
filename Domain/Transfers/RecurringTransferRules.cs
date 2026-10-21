namespace Domain.Transfers
{
    // Aylıq təkrarlanan köçürmənin NƏ VAXT işləməsi. Bazaya toxunmayan təmiz funksiyalardır (asan sınanır). Günlər Bakı günüdür
    public static class RecurringTransferRules
    {
        public const int MinDay = 1;
        public const int MaxDay = 28;       // 29-31 bəzi aylarda yoxdur, ona görə 28-dən çox seçilmir

        public static string PeriodOf(DateTime day)
        {
            return day.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        }

        // Yaradılanda LastPeriod: seçilmiş gün bu ay artıq keçibsə (və ya bu gündürsə) ilk köçürmə gələn ayda olsun, yoxsa bu ay həmin gün
        public static string InitialLastPeriod(DateTime today, int dayOfMonth)
        {
            if (today.Day >= dayOfMonth)
            {
                return PeriodOf(today);
            }
            return PeriodOf(new DateTime(today.Year, today.Month, 1).AddMonths(-1));
        }

        // Bu gün köçürmə vaxtıdırmı: bu ay hələ edilməyib və seçilmiş gün gəlib (proqram bağlı qalıbsa gün keçsə də ay ərzində edilir)
        public static bool IsDue(DateTime today, int dayOfMonth, string lastPeriod)
        {
            return lastPeriod != PeriodOf(today) && today.Day >= dayOfMonth;
        }

        // Siyahıda göstərilən "növbəti köçürmə" günü
        public static DateTime NextRun(DateTime today, int dayOfMonth, string lastPeriod)
        {
            if (IsDue(today, dayOfMonth, lastPeriod))
            {
                return today;
            }
            var month = new DateTime(today.Year, today.Month, 1);
            if (lastPeriod != PeriodOf(today) && today.Day < dayOfMonth)
            {
                return month.AddDays(dayOfMonth - 1);
            }
            return month.AddMonths(1).AddDays(dayOfMonth - 1);
        }
    }
}
