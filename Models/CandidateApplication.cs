namespace Models;

public class CandidateApplication
{
    public int CandidateApplicationId { get; set; }

    public int CandidateId { get; set; }

    public int JobId { get; set; }

    public int FranchiseeId { get; set; }

    public int? AssignedStaffId { get; set; }

    public string Status { get; set; } = "New";

    public string? Notes { get; set; }

    public string? Source { get; set; }

    public DateTime AppliedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public Candidate? Candidate { get; set; }

    public Job? Job { get; set; }

    public Franchisee? Franchisee { get; set; }

    public Staff? AssignedStaff { get; set; }
}