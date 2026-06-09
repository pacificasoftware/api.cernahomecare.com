using api.cernahomecare.com.Data;
using api.cernahomecare.com.Models;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public ApplicationsController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    [HttpPost("submit")]
    [AllowAnonymous]
    public async Task<IActionResult> Submit(ApplicationSubmitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Full name, email, and phone are required."
            });
        }

        var candidate = new Candidate
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address?.Trim(),
            HasHcaPerId = request.HasHcaPerId,
            HowHeardAboutUs = request.HowHeardAboutUs,
            Status = "New",
            Source = "Website",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Application submitted successfully.",
            candidateId = candidate.CandidateId
        });
    }
}