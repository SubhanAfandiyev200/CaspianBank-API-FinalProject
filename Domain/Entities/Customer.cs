using Domain.Common;

namespace Domain.Entities
{
    // Saxta dövlət reyestrindəki vətəndaş. Login və parolu yoxdur: Caspian-da qeydiyyatdan keçəndə AppUser.CustomerId ona bağlanır.
    // Burada email və telefon yoxdur, onları qeydiyyatdan keçən adam özü yazır.
    public class Customer : BaseEntity
    {
        public string Fin { get; set; } = string.Empty;                 // 7 simvol (məs. 5KM2R9A)
        public string IdCardNumber { get; set; } = string.Empty;        // məs. AZE10482931
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string BirthPlace { get; set; } = string.Empty;
        public string ResidenceAddress { get; set; } = string.Empty;
        public DateTime? DeathDate { get; set; }                        // doludursa vətəndaş vəfat edib
        public bool IsBlacklisted { get; set; }                         // true olanda kredit verilmir (qeydiyyat olar)
        public string? BlacklistReason { get; set; }
        public string? PhotoUrl { get; set; }                           // məs. /citizens/5KM2R9A.jpg

        public ICollection<Employment> Employments { get; set; } = new List<Employment>();
        public ICollection<ExternalLoan> ExternalLoans { get; set; } = new List<ExternalLoan>();
    }
}
