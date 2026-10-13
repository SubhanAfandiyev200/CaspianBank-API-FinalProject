namespace Service.Helpers.DTOs.HomeStats
{
    // Home səhifəsindəki "Trust indicators" bölməsi: bazadan canlı hesablanan rəqəmlər
    public class HomeStatsDto
    {
        public int CustomerCount { get; set; }          // Customer rolu olan hesablar
        public int TransferCount { get; set; }          // edilmiş köçürmələr
        public decimal TransferVolume { get; set; }     // köçürmələrin ümumi məbləği (AZN, komissiya daxil deyil)
        public int CardCount { get; set; }              // verilmiş kartlar (bütün növlər)
    }
}
