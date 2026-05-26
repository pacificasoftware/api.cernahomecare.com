namespace CernaHomeCare.AdminApi.Models;

public class AdminUser
{
    public int AdminUserId { get; set; }

    public int RoleId { get; set; }
    public int? FranchiseeId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordSalt { get; set; }

    public bool IsActive { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }

    public Role? Role { get; set; }
    public Franchisee? Franchisee { get; set; }
}