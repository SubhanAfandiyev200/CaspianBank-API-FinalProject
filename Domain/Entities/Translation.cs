using Domain.Common;

namespace Domain.Entities
{
    // Saytın mətn tərcüməsi (verilənlər bazasından gəlir). Açar ingilis mətnin özüdür ("Transfer money"), ona görə tərcümə olmayanda səhifədə ingilis mətn qalır.
    // Language: "az", "ru" (ingilis üçün sətir lazım deyil, açar özü mətndir). Admin dəyişdirə bilər
    public class Translation : BaseEntity
    {
        public string Language { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
