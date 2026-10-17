namespace Domain.Bills
{
    // Aylıq təkrarlanan ödənişin NƏ VAXT işləməsi. Bazaya toxunmayan təmiz funksiyalardır (asan sınanır).
    // Mobil, kommunal və internet: hər ayın 1-i (ay hesabı yazıldıqdan sonra). Kredit: taksit ayın SON günü vaxtı çatır,
    // ona görə həmin gün ödənilir. 1-də ödənilsəydi taksit artıq 1 gün gecikmiş olardı və 10 AZN cərimə + gündə 0.5% düşərdi
    public static class RecurringRules
    {
        private static string PeriodOf(DateTime day)
        {
            return day.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static DateTime LastDay(DateTime day)
        {
            return new DateTime(day.Year, day.Month, DateTime.DaysInMonth(day.Year, day.Month));
        }

        // today: Bakı günü. lastPeriod: "2026-10" (bu ay üçün artıq işlənib)
        public static bool IsDue(bool isLoan, DateTime today, string lastPeriod)
        {
            var period = PeriodOf(today);
            if (lastPeriod == period)
            {
                return false;
            }
            if (!isLoan)
            {
                return true;
            }

            // Kredit: ayın son günü, və ya keçən ay buraxılıbsa (proqram bağlı qalıb) dərhal
            var previous = PeriodOf(new DateTime(today.Year, today.Month, 1).AddMonths(-1));
            var isLastDay = today.Day == DateTime.DaysInMonth(today.Year, today.Month);
            return isLastDay || string.CompareOrdinal(lastPeriod, previous) < 0;
        }

        // Cədvəldə göstərilən "növbəti ödəniş" günü
        public static DateTime NextRun(bool isLoan, DateTime today, string lastPeriod)
        {
            if (IsDue(isLoan, today, lastPeriod))
            {
                return today.Date;
            }

            var monthStart = new DateTime(today.Year, today.Month, 1);
            if (isLoan)
            {
                // Bu ay hələ işlənməyib (son gün gözlənilir) və ya artıq işlənib (gələn ayın son günü)
                return lastPeriod == PeriodOf(today) ? LastDay(monthStart.AddMonths(1)) : LastDay(monthStart);
            }
            return monthStart.AddMonths(1);
        }

        // Yaradılanda hansı ay "işlənmiş" sayılsın. Mobil/kommunal/internet: ilk ödəniş elə indi edilir, bu ay işlənmiş sayılır.
        // Kredit: bu ayın son günü hələ gələcək, ona görə keçən ay yazılır (həmin gün nə qədər borc varsa ödənilir, yoxdursa səssiz keçilir)
        public static string StartingPeriod(bool isLoan, DateTime today)
        {
            return isLoan
                ? PeriodOf(new DateTime(today.Year, today.Month, 1).AddMonths(-1))
                : PeriodOf(today);
        }
    }
}
