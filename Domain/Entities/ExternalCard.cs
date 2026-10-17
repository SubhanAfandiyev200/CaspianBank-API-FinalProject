using Domain.Common;

namespace Domain.Entities
{
    // Başqa bankın saxta müştəri kartı: Caspian müştərisi bu nömrəyə pul göndərə bilir. Balans saxlanmır (pul "başqa banka gedib")
    // Bank kartın özündə yox, ilk 8 rəqəmin (BIN) uyğun gəldiyi ExternalBank-dan tapılır, amma sorğunu sürətləndirmək üçün açar kimi də saxlanır
    public class ExternalCard : BaseEntity
    {
        public int ExternalBankId { get; set; }
        public ExternalBank ExternalBank { get; set; } = null!;

        public string CardNumber { get; set; } = string.Empty;      // 16 rəqəm, ilk 8-i bankın BIN-i
        public string HolderName { get; set; } = string.Empty;      // "Elnur Məmmədov": alıcıya göstəriləndə "Elnur M." kimi qısaldılır
        public bool IsActive { get; set; } = true;
    }
}
