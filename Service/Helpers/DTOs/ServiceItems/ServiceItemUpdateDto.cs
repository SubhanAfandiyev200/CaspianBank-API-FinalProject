using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.ServiceItems
{
    // Mətnlər və ikon dəyişir. Number (kartın hansı səhifəyə getdiyini müəyyən edir) sabit qalır
    public class ServiceItemUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }

        // İstəyə bağlıdır: boş olarsa köhnə ikon qalır
        public IFormFile? Icon { get; set; }
    }
}
