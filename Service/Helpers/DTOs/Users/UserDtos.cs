namespace Service.Helpers.DTOs.Users
{
    public class UserFilterDto
    {
        public string? Search { get; set; }     // ad, soyad, email, telefon və ya FİN
        public string? Status { get; set; }     // Active, Restricted, boşdursa hamısı
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Müştərilərin siyahısının sətri
    public class UserListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Fin { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsRestricted { get; set; }
        public int CardCount { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class UserPageDto
    {
        public List<UserListItemDto> Items { get; set; } = new List<UserListItemDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int RestrictedCount { get; set; }    // bütün müştərilər arasında donmuşların sayı
    }

    public class UserCardDto
    {
        public int Id { get; set; }
        public string Tier { get; set; } = string.Empty;
        public string Last4 { get; set; } = string.Empty;
        public string Expiry { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsBlocked { get; set; }
    }

    public class UserLoanDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal OutstandingPrincipal { get; set; }
    }

    public class UserActivityDto
    {
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string CardLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; }
    }

    public class UserDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Fin { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsRestricted { get; set; }
        public string? RestrictionReason { get; set; }
        public DateTime? RestrictedAt { get; set; }
        public string? RestrictedBy { get; set; }
        public decimal TotalSent { get; set; }          // köçürmələrlə göndərilən cəmi
        public int BillPaymentCount { get; set; }
        public decimal BillPaymentTotal { get; set; }
        public List<UserCardDto> Cards { get; set; } = new List<UserCardDto>();
        public List<UserLoanDto> Loans { get; set; } = new List<UserLoanDto>();
        public List<UserActivityDto> RecentActivity { get; set; } = new List<UserActivityDto>();
    }

    public class RestrictUserDto
    {
        public string? Reason { get; set; }
    }
}
