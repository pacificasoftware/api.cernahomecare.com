namespace CernaHomeCare.AdminApi.Models;

public class CreateCandidateRequest
{
    public int? FranchiseeId { get; set; }
    public int? AssignedAdminUserId { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Address { get; set; }
    public string? HasHcaPerId { get; set; }
    public string? HowHeardAboutUs { get; set; }
    public string? Notes { get; set; }
    public string? Source { get; set; }
}

public class UpdateCandidateStatusRequest
{
    public string Status { get; set; } = "";
}

public class AssignCandidateRequest
{
    public int AssignedAdminUserId { get; set; }
}

public class UpdateCandidateNotesRequest
{
    public string? Notes { get; set; }
}