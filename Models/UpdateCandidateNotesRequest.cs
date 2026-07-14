namespace Models;
public class CreateCandidateRequest
{
	public int JobId { get; set; }

	public int? AssignedStaffId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string Phone { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

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
	public int? AssignedStaffId { get; set; }
}

public class UpdateCandidateNotesRequest
{
    public string? Notes { get; set; }
}