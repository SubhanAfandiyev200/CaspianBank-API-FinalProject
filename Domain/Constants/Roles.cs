namespace Domain.Constants
{
    public static class Roles
    {
        public const string Customer = "Customer";
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";                 // Rol verə/silə bilməz, qalan hər şeyi edə bilər
        public const string Accountant = "Accountant";       // Mühasibat
        public const string CustomerSupport = "CustomerSupport"; // Müştəri məmnuniyyəti
        public const string Security = "Security";           // Şübhəli əməliyyatlar

        // Bütün işçi rolları (Customer-dən başqa): hamıya gedən bildirişlər və hamının açdığı səhifələr üçün
        public const string AllStaffCsv = SuperAdmin + "," + Admin + "," + Accountant + "," + CustomerSupport + "," + Security;
        public static readonly string[] AllStaff = { SuperAdmin, Admin, Accountant, CustomerSupport, Security };

        public static readonly string[] All =
        {
            Customer, SuperAdmin, Admin, Accountant, CustomerSupport, Security
        };
    }
}
