namespace Models;

using System.ComponentModel.DataAnnotations;

public class CreateStaffRequest
{
    [Required]
    public int FranchiseeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? JobTitle { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateStaffRequest
{
    [Required]
    public int FranchiseeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? JobTitle { get; set; }

    public bool IsActive { get; set; }
}

public class UpdateStaffStatusRequest
{
    public bool IsActive { get; set; }
} 