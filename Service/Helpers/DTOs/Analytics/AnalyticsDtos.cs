namespace Service.Helpers.DTOs.Analytics
{
    public class SpendingCategoryDto
    {
        public string Code { get; set; } = string.Empty;        // Transfers, Bills, Loans, Fees, Deposits
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class SpendingMonthDto
    {
        public string Key { get; set; } = string.Empty;         // 2026-10
        public string Label { get; set; } = string.Empty;       // Oct 2026
        public decimal Total { get; set; }
        public List<SpendingCategoryDto> Categories { get; set; } = new List<SpendingCategoryDto>();
    }

    // Son aylarda kartlardan çıxan pul kateqoriyalar üzrə (öz kartları arasında köçürmə sayılmır)
    public class SpendingAnalyticsDto
    {
        public List<SpendingMonthDto> Months { get; set; } = new List<SpendingMonthDto>();     // köhnədən yeniyə, sonuncusu cari ay
        public decimal CurrentTotal { get; set; }
        public decimal PreviousTotal { get; set; }
        public decimal? ChangePercent { get; set; }             // keçən aya nisbətən, keçən ay 0-dırsa boş
        public string? TopCategory { get; set; }                // cari ayda ən çox pul gedən kateqoriya
        public decimal AverageMonthly { get; set; }             // göstərilən ayların orta aylıq xərci
    }
}
