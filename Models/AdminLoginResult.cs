namespace Models;

public class AdminLoginResult
{
    public int AdminUserId { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? FranchiseeId { get; set; }
    public string? FranchiseeName { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
}
