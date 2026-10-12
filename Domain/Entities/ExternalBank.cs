using Domain.Common;

namespace Domain.Entities
{
    // Başqa bank. Bin kartın ilk 8 rəqəmidir: köçürmədə alıcı kartının hansı banka aid olduğunu bununla tanıyırıq
    public class ExternalBank : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Bin { get; set; } = string.Empty;         // 8 rəqəm

        public ICollection<ExternalLoan> ExternalLoans { get; set; } = new List<ExternalLoan>();
    }
}
