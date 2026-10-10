namespace Service.Helpers.DTOs.History
{
    // GET api/history/statement sorğusunun parametrləri (hamısı məcburidir)
    public class StatementFilterDto
    {
        public int? CardId { get; set; }

        // Bakı vaxtı ilə gün: From günün əvvəlindən, To günün sonuna qədər daxildir
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
