namespace api.cernahomecare.com.Models;

public class ApplicationSubmitWithResumeRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Address { get; set; }
    public string? HasHcaPerId { get; set; }
    public string? HowHeardAboutUs { get; set; }

    public IFormFile? Resume { get; set; }
}