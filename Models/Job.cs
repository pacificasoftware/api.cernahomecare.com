using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Models;

public class Job
{
    [Key]
    public int JobId { get; set; }

    public int FranchiseeId { get; set; }

    public string JobTitle { get; set; } = string.Empty;
    public string? JobType { get; set; }
    public string? ShiftType { get; set; }
    public string? JobDescription { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public string? PayRange { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedUtc { get; set; }

    public Franchisee? Franchisee { get; set; }

    [JsonIgnore]
    public ICollection<CandidateApplication> CandidateApplications { get; set; } =
        new List<CandidateApplication>();
}