namespace CernaHomeCare.AdminApi.Models;

public class Candidate
{
    public int CandidateId { get; set; }

    public int? FranchiseeId { get; set; }
    public int? AssignedAdminUserId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }

    public string? HasHcaPerId { get; set; }
    public string? HowHeardAboutUs { get; set; }

    public string Status { get; set; } = "New";
    public string? Notes { get; set; }

    public string? Source { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }

    public Franchisee? Franchisee { get; set; }
    public AdminUser? AssignedAdminUser { get; set; }

    public ICollection<CandidateFile> CandidateFiles { get; set; } = new List<CandidateFile>();
}