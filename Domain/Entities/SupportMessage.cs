using Domain.Common;

namespace Domain.Entities
{
    // Yazışmanın bir mesajı. Müəllifin adı həmin anın surətidir (işçi sonra silinsə də yazışma qalır)
    public class SupportMessage : BaseEntity
    {
        public int TicketId { get; set; }
        public SupportTicket Ticket { get; set; } = null!;

        public string AuthorUserId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public bool IsStaff { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
