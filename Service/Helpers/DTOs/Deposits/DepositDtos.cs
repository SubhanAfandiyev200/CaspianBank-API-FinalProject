namespace Service.Helpers.DTOs.Deposits
{
    // Depozit variantı: müddət, illik faiz və məbləğ hədləri
    public class DepositPlanDto
    {
        public int Id { get; set; }
        public int TermMonths { get; set; }
        public decimal AnnualRate { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public bool IsActive { get; set; }
    }

    public class OpenDepositDto
    {
        public int PlanId { get; set; }
        public decimal Amount { get; set; }
        public int SourceCardId { get; set; }       // pul bu kartdan çıxır (Cashback kartı da ola bilər)
        public int PayoutCardId { get; set; }       // müddət bitəndə pul bu karta qayıdır (Cashback olmaz)

        // Forma hər açılanda yeni GUID yaranır: eyni sorğu iki dəfə göndərilsə depozit bir dəfə açılır
        public string? RequestId { get; set; }
    }

    public class DepositDto
    {
        public string Reference { get; set; } = string.Empty;
        public int TermMonths { get; set; }
        public decimal AnnualRate { get; set; }
        public decimal Principal { get; set; }
        public decimal ExpectedInterest { get; set; }       // müddət sonunda veriləcək faiz
        public decimal AccruedInterest { get; set; }        // bu günə qədər yazılmış faiz (ay-ay artır; pul müddət bitəndə ödənilir)
        public List<DepositPostingDto> Postings { get; set; } = new List<DepositPostingDto>();
        public decimal TotalAtMaturity { get; set; }        // əsas məbləğ + faiz
        public int ProgressPercent { get; set; }            // müddətin neçə faizi keçib
        public int DaysLeft { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime MaturesAt { get; set; }
        public string Status { get; set; } = string.Empty;  // Active, Paid
        public DateTime? PaidAt { get; set; }
        public string SourceCardLabel { get; set; } = string.Empty;
        public string PayoutCardLabel { get; set; } = string.Empty;
    }

    // Bir ayın faiz yazılışı
    public class DepositPostingDto
    {
        public int Number { get; set; }
        public decimal Amount { get; set; }
        public DateTime PostedAt { get; set; }
    }

    public class DepositsOverviewDto
    {
        public List<DepositPlanDto> Plans { get; set; } = new List<DepositPlanDto>();
        public List<DepositDto> Deposits { get; set; } = new List<DepositDto>();
        public decimal ActivePrincipal { get; set; }
        public decimal ActiveExpectedInterest { get; set; }
        public decimal InterestEarned { get; set; }         // artıq ödənilmiş faizin cəmi
    }

    // Admin: bütün müştərilərin depozitləri
    public class DepositFilterDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }                 // Active, Paid
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminDepositDto
    {
        public string Reference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TermMonths { get; set; }
        public decimal AnnualRate { get; set; }
        public decimal Principal { get; set; }
        public decimal ExpectedInterest { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime MaturesAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }

    public class DepositPageDto
    {
        public List<AdminDepositDto> Items { get; set; } = new List<AdminDepositDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int ActiveCount { get; set; }
        public decimal ActivePrincipal { get; set; }
        public decimal ExpectedInterest { get; set; }
        public decimal PaidInterest { get; set; }
        public List<DepositPlanDto> Plans { get; set; } = new List<DepositPlanDto>();
    }

    public class UpdateDepositPlanDto
    {
        public decimal AnnualRate { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
