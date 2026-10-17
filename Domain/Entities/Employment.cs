using Domain.Common;

namespace Domain.Entities
{
    // Vətəndaşın bir iş yerindəki əmək fəaliyyəti. EndDate boşdursa hazırda işləyir
    public class Employment : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public int WorkplaceId { get; set; }
        public Workplace Workplace { get; set; } = null!;

        public string Position { get; set; } = string.Empty;
        public decimal MonthlySalary { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? LeaveReason { get; set; }
    }
}
