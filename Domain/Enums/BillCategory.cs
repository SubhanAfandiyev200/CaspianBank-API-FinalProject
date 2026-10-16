namespace Domain.Enums
{
    // Ödəniş təchizatçısının növü. Mobil operatora nömrəyə, qalanına abunəçi koduna görə ödənilir
    public enum BillCategory
    {
        Mobile,         // Nar, Azercell, Bakcell: balans artırma
        Electricity,    // Azərişıq: aylıq işlətməyə görə hesab
        Gas,            // Azəriqaz: aylıq işlətməyə görə hesab
        Water,          // Azərsu: aylıq işlətməyə görə hesab
        Internet        // 20, 30 və ya 50 AZN-lik aylıq paket
    }
}
