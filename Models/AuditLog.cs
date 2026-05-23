namespace CernaHomeCare.AdminApi.Models;

public class AuditLog
{
    public long AuditLogId { get; set; }

    public int? AdminUserId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }

    public DateTime CreatedUtc { get; set; }

    public AdminUser? AdminUser { get; set; }
}