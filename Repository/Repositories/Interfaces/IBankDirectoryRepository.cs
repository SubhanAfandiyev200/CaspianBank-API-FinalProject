using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    // Kart nömrəsinin ilk 8 rəqəmindən (BIN) bankı tanımaq və başqa bankların saxta kartlarını tapmaq. Hamısı izləmir
    public interface IBankDirectoryRepository
    {
        // 8 rəqəmli BIN-ə uyğun başqa bank (yoxdursa null)
        Task<ExternalBank?> FindExternalBankByBinAsync(string bin);

        // Tam kart nömrəsi ilə başqa bankın saxta kartı (bankı ilə). Aktiv olmayan və ya yoxdursa null
        Task<ExternalCard?> FindExternalCardAsync(string cardNumber);

        // Caspian kartlarından biri bu 8 rəqəmlə başlayırmı (BIN hələ bank qaydalarında yazılmayıbsa da Caspian kartını tanımaq üçün)
        Task<bool> AnyCaspianCardStartsWithAsync(string bin);
    }
}
