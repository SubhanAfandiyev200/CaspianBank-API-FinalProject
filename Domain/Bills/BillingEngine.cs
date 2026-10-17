using Domain.Entities;
using Domain.Enums;

namespace Domain.Bills
{
    // Ödənişlərin saf hesablaması (bazaya toxunmur, ona görə test etmək asandır): telefon nömrəsi, operator prefiksi və abunəçinin aylıq hesabı
    public static class BillingEngine
    {
        public static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        // ---------- Telefon nömrəsi ----------

        // "055 123 45 67", "+994 55 123 45 67", "551234567" kimi yazılışları 0551234567 formasına gətirir. Nömrəyə oxşamırsa null
        public static string? NormalizePhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var digits = new string(input.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("994", StringComparison.Ordinal) && digits.Length == 12)
            {
                digits = digits.Substring(3);
            }
            if (digits.Length == 9)
            {
                digits = "0" + digits;
            }
            if (digits.Length != 10 || digits[0] != '0')
            {
                return null;
            }
            return digits;
        }

        // 0551234567 -> "55" (operator kodu: 0-dan sonrakı iki rəqəm)
        public static string PhonePrefix(string canonicalPhone)
        {
            return canonicalPhone.Substring(1, 2);
        }

        // 0551234567 -> "055 123 45 67"
        public static string FormatPhone(string canonicalPhone)
        {
            return canonicalPhone.Substring(0, 3) + " " + canonicalPhone.Substring(3, 3) + " " + canonicalPhone.Substring(6, 2) + " " + canonicalPhone.Substring(8, 2);
        }

        public static bool PrefixBelongs(string? providerPrefixes, string phonePrefix)
        {
            if (string.IsNullOrWhiteSpace(providerPrefixes))
            {
                return false;
            }
            return providerPrefixes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(phonePrefix);
        }

        // "70,77" -> "070 or 077" (xəta mesajı üçün)
        public static string PrefixesText(string? providerPrefixes)
        {
            if (string.IsNullOrWhiteSpace(providerPrefixes))
            {
                return string.Empty;
            }
            var list = providerPrefixes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(p => "0" + p).ToList();
            if (list.Count == 1)
            {
                return list[0];
            }
            return string.Join(", ", list.Take(list.Count - 1)) + " or " + list[list.Count - 1];
        }

        // ---------- Aylıq hesab ----------

        // 2026-10 (Bakı ayı)
        public static string PeriodOf(DateTime bakuDay)
        {
            return bakuDay.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        }

        // Abunəçiyə bu ay üçün yazılacaq hesab. Mobil nömrələrə aylıq hesab yoxdur (məbləğ 0).
        // İnternet: paketin qiyməti. İşıq, qaz, su: işlətmə x təchizatçının tarifi (paket yoxdur, işlətmə fəslə və abunəçiyə görə dəyişir)
        public static BillChargeResult ComputeCharge(BillProvider provider, BillSubscriber subscriber, string period)
        {
            if (provider.Category == BillCategory.Mobile)
            {
                return new BillChargeResult(0m, null, string.Empty);
            }

            if (provider.Category == BillCategory.Internet)
            {
                var fee = subscriber.MonthlyFee ?? 0m;
                var package = string.IsNullOrWhiteSpace(subscriber.PackageName) ? "Internet" : subscriber.PackageName;
                return new BillChargeResult(Round(fee), null, package + " · " + period);
            }

            var month = int.Parse(period.Substring(5, 2));
            var year = int.Parse(period.Substring(0, 4));
            var average = subscriber.AverageUsage ?? 0m;
            var usage = Round(average * SeasonFactor(provider.Category, month) * Variation(subscriber.Id, year, month));
            var amount = Round(usage * provider.UnitPrice);
            var unit = string.IsNullOrWhiteSpace(provider.UnitName) ? "units" : provider.UnitName;
            return new BillChargeResult(amount, usage, usage.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " " + unit + " · " + period);
        }

        // Qaz qışda çox, yayda az işlənir; işıq yayda (kondisioner) və qışda bir qədər artır; su yayda bir qədər çox
        public static decimal SeasonFactor(BillCategory category, int month)
        {
            switch (category)
            {
                case BillCategory.Gas:
                    if (month == 12 || month == 1 || month == 2)
                    {
                        return 1.8m;
                    }
                    if (month == 11 || month == 3)
                    {
                        return 1.4m;
                    }
                    if (month == 4 || month == 10)
                    {
                        return 1.0m;
                    }
                    return 0.35m;
                case BillCategory.Electricity:
                    if (month == 7 || month == 8)
                    {
                        return 1.15m;
                    }
                    if (month == 12 || month == 1 || month == 2)
                    {
                        return 1.1m;
                    }
                    return 1.0m;
                case BillCategory.Water:
                    if (month == 7 || month == 8)
                    {
                        return 1.15m;
                    }
                    return 1.0m;
                default:
                    return 1.0m;
            }
        }

        // Hər abunəçi hər ay bir qədər fərqli işlədir (0.90 - 1.10). Təsadüfi deyil: eyni abunəçi və ay həmişə eyni nəticə verir
        public static decimal Variation(int subscriberId, int year, int month)
        {
            var index = (int)(((long)subscriberId * 7919 + year * 12 + month) % 21);
            return 0.90m + index * 0.01m;
        }
    }

    public record BillChargeResult(decimal Amount, decimal? Usage, string Description);
}
