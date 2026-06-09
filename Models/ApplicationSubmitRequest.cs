namespace api.cernahomecare.com.Models
{
    public class ApplicationSubmitRequest
    {
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Address { get; set; }
        public string? HasHcaPerId { get; set; }
        public string? HowHeardAboutUs { get; set; }
    }
}
