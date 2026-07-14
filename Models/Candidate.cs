namespace Models;

public class Candidate
{
    public int CandidateId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? HasHcaPerId { get; set; }

    public string? HowHeardAboutUs { get; set; }

    public string? Source { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedUtc { get; set; }

    public ICollection<CandidateFile> CandidateFiles { get; set; } =
        new List<CandidateFile>();

    public ICollection<CandidateApplication> CandidateApplications { get; set; } =
        new List<CandidateApplication>();
}