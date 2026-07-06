namespace api.cernahomecare.com.Models
{
    public class UpdateAdminUserRequest
    {
        public int? AdminId { get; set; }
        public int? AdminUserId { get; set; }
        public string? FullName { get; set; }
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public string Email { get; set; } = "";
        public string? Password { get; set; }
        public string? RoleName { get; set; } = "Admin";
        public int? FranchiseeId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
