namespace CernaHomeCare.AdminApi.Models;

public class UpdateCandidateRequest
{
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? HasHcaPerId { get; set; }
    public string? HowHeardAboutUs { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? Source { get; set; }
}