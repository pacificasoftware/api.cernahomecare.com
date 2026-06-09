namespace CernaHomeCare.AdminApi.Models;

public class AdminUser
{
    public int AdminUserId { get; set; }

    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int RoleId { get; set; }
    public string UserName { get; set; } = "";
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public int? FranchiseeId { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }

    public Role? Role { get; set; }
    public Franchisee? Franchisee { get; set; }
}