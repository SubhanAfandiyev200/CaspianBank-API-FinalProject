namespace Domain.Deposits
{
    // Depozitin hesabı (sadə faiz): faiz = məbləğ x illik faiz x ay / 12. Həm açılışda göstərmək, həm də ödəmək üçün eyni düstur
    public static class DepositRules
    {
        public static decimal Interest(decimal principal, decimal annualRatePercent, int months)
        {
            return Math.Round(principal * annualRatePercent / 100m * months / 12m, 2, MidpointRounding.AwayFromZero);
        }

        public static DateTime MaturityDate(DateTime startedAtUtc, int months)
        {
            return startedAtUtc.AddMonths(months);
        }

        // Müddətin neçə faizi keçib (0-1): səhifədəki irəliləyiş xətti üçün
        public static decimal Progress(DateTime startedAtUtc, DateTime maturesAtUtc, DateTime nowUtc)
        {
            var total = (maturesAtUtc - startedAtUtc).TotalSeconds;
            if (total <= 0)
            {
                return 1m;
            }
            var done = (nowUtc - startedAtUtc).TotalSeconds / total;
            return (decimal)Math.Max(0d, Math.Min(1d, done));
        }

        // Faizin aylıq yazılışı: N-ci ayın məbləği. Son ay yuvarlaqlaşdırma fərqini götürür ki, cəm tam ExpectedInterest olsun
        public static decimal MonthlyInterest(decimal expectedInterest, int termMonths, int number)
        {
            var regular = Math.Round(expectedInterest / termMonths, 2, MidpointRounding.AwayFromZero);
            if (number < termMonths)
            {
                return regular;
            }
            return expectedInterest - regular * (termMonths - 1);
        }

        // Bu günə qədər yığılmış faiz (yalnız göstəriş, ödəniş müddət sonunda olur)
        public static decimal Accrued(decimal expectedInterest, decimal progress)
        {
            return Math.Round(expectedInterest * progress, 2, MidpointRounding.AwayFromZero);
        }
    }
}
