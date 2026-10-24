namespace Domain.Transfers
{
    // Köçürmə komissiyası: kartın AYLIQ pulsuz limiti var (tier-ə görə). Komissiya yalnız limiti keçən hissəyə tutulur.
    // Bu qayda bütün köçürmələrə eynidir: öz kartlarına, başqa Caspian müştərisinə və başqa banka. Ayrıca banklararası komissiya yoxdur.
    // Misal (Standard: limit 500, komissiya 1%): ay ərzində hələ heç nə göndərilməyib, 600 göndərilir -> artıq 100-ə 1.00 komissiya.
    // Sonrakı köçürmələrdə limit artıq keçilib, ona görə hər köçürmənin TAM məbləğinə komissiya tutulur
    public static class TransferFees
    {
        // Bu köçürmənin neçə manatı limitdən yuxarıdır. usedThisMonth: bu ay həmin kartdan artıq göndərilənlər (komissiyasız)
        public static decimal ChargeableAmount(decimal usedThisMonth, decimal amount, decimal monthlyLimit)
        {
            var overBefore = Math.Max(0m, usedThisMonth - monthlyLimit);
            var overAfter = Math.Max(0m, usedThisMonth + amount - monthlyLimit);
            return overAfter - overBefore;
        }

        public static decimal Commission(decimal usedThisMonth, decimal amount, decimal monthlyLimit, decimal commissionPercent)
        {
            var chargeable = ChargeableAmount(usedThisMonth, amount, monthlyLimit);
            return Math.Round(chargeable * commissionPercent / 100m, 2, MidpointRounding.AwayFromZero);
        }
    }
}
