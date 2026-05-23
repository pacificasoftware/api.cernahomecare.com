namespace CernaHomeCare.AdminApi.Models;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
