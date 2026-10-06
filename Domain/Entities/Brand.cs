using Domain.Common;

namespace Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}
