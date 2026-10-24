using Domain.Entities;

namespace Repository.Results
{
    // Xəzinə jurnalının sətri və həmin sətirdən sonrakı balans
    public record TreasuryRow(TreasuryEntry Entry, decimal BalanceAfter);
}
