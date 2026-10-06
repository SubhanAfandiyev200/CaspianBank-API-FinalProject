using Microsoft.AspNetCore.Identity;

namespace Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public DateTime BirthDay { get; set; }
        public string? FinKod { get; set; }
        public bool IsRestricted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // İstifadəçinin kartları (one-to-many)
        public ICollection<Card> Cards { get; set; } = new List<Card>();
    }
}
