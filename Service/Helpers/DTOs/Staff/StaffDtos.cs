namespace Service.Helpers.DTOs.Staff
{
    public class StaffDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;        // SuperAdmin, Admin, Accountant, CustomerSupport, Security
        public bool IsActive { get; set; }
        public bool IsSuperAdmin { get; set; }
        public DateTime CreatedAt { get; set; }

        // Yeni işçi yaradılanda: SMTP qurulmayıbsa (yalnız Development) şifrə seçmə linki buradadır
        public string? InviteLink { get; set; }
    }

    public class StaffListDto
    {
        public List<StaffDto> Items { get; set; } = new List<StaffDto>();
        public List<string> AssignableRoles { get; set; } = new List<string>();
    }

    public class AddStaffDto
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ChangeStaffRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }
}
