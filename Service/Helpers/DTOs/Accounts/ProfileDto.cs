namespace Service.Helpers.DTOs.Accounts
{
    // Müştərinin öz profili: yalnız ad, soyad, FİN, telefon, email və şəkil (başqa reyestr məlumatı göstərilmir)
    public class ProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? Fin { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? PhotoUrl { get; set; }                   // məs. /citizens/5KM2R9A.jpg (reyestrdən)
    }
}
