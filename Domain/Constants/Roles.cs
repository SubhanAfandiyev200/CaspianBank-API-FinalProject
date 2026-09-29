namespace Domain.Constants
{
    public static class Roles
    {
        public const string Customer = "Customer";
        public const string SuperAdmin = "SuperAdmin";
        public const string Accountant = "Accountant";       // Mühasibat
        public const string WebDesigner = "WebDesigner";     // Sayt dizaynı (logo, brend, kart dizaynı)
        public const string CustomerSupport = "CustomerSupport"; // Müştəri məmnuniyyəti
        public const string Security = "Security";           // Şübhəli əməliyyatlar

        public static readonly string[] All =
        {
            Customer, SuperAdmin, Accountant, WebDesigner, CustomerSupport, Security
        };
    }
}
