namespace Service.Helpers.DTOs.ServiceItems
{
    // Yalnız mətnlər dəyişir. Number (kartın hansı səhifəyə getdiyini müəyyən edir) və ikon sabit qalır
    public class ServiceItemUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
