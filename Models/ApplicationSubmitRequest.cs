using System.ComponentModel.DataAnnotations;

namespace Models;

public class ApplicationSubmitRequest
{
    [Range(1, int.MaxValue)]
    public int JobId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? HasHcaPerId { get; set; }

    [MaxLength(250)]
    public string? HowHeardAboutUs { get; set; }
}