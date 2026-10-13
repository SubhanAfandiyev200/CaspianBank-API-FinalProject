namespace Repository.Repositories.Interfaces
{
    // Home səhifəsinin ümumi rəqəmləri: bir neçə cədvəldən yalnız say və cəm oxunur (heç nə dəyişmir)
    public interface IHomeStatsRepository
    {
        Task<int> CountCustomersAsync();

        // Köçürmə sayı: hər köçürmənin göndərənin çıxışı (TransferOut) sətri sayılır
        Task<int> CountTransfersAsync();

        // Köçürülən ümumi məbləğ (komissiya daxil deyil). Köçürmə yoxdursa 0
        Task<decimal> SumTransferAmountAsync();

        Task<int> CountCardsAsync();
    }
}
