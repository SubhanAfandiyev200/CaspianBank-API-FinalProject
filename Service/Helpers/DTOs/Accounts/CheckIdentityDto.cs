namespace Service.Helpers.DTOs.Accounts
{
    // Qeydiyyatın 1-ci addımı: istifadəçi dörd məlumatı yazır, hamısı reyestrdəki eyni vətəndaşa uyğun olmalıdır
    public class CheckIdentityDto
    {
        public string Fin { get; set; } = string.Empty;
        public string IdCardNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
    }
}
