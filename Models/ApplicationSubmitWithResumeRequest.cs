namespace Models;

public class ApplicationSubmitWithResumeRequest
{
    public int JobId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? HasHcaPerId { get; set; }

    public string? HowHeardAboutUs { get; set; }

    public IFormFile? Resume { get; set; }
}