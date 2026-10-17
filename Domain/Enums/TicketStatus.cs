namespace Domain.Enums
{
    // Əlaqə mesajının (ticket) vəziyyəti: müştəri yazır (Pending), işçi cavab verir (Answered), müştəri yenə yazsa yenidən Pending, sonda Closed
    public enum TicketStatus
    {
        Pending,    // işçinin cavabı gözlənilir
        Answered,   // işçi cavab verib, müştərinin cavabı gözlənilmir
        Closed      // bağlanıb
    }
}
