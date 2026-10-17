namespace Service.Helpers.DTOs.Bank
{
    public class TreasuryFilterDto
    {
        public string? Kind { get; set; }       // TreasuryEntryKind adı
        public string? From { get; set; }       // yyyy-MM-dd (Bakı günü)
        public string? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class TreasuryEntryDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }             // giriş müsbət, çıxış mənfi
        public decimal BalanceAfter { get; set; }
    }

    // Bankın xəzinəsi: hazırkı balans, məcburi ehtiyat, kredit üçün qalan vəsait və jurnal
    public class TreasuryPageDto
    {
        public decimal Balance { get; set; }
        public decimal CustomerMoney { get; set; }          // kartlardakı pul + depozitlər
        public decimal ReservePercent { get; set; }
        public decimal RequiredReserve { get; set; }
        public decimal AvailableForLoans { get; set; }      // balans - məcburi ehtiyat (mənfi ola bilməz)
        public decimal MonthIn { get; set; }
        public decimal MonthOut { get; set; }
        public string MonthLabel { get; set; } = string.Empty;
        public List<TreasuryEntryDto> Items { get; set; } = new List<TreasuryEntryDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
