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
    private readonly IWebHostEnvironment _environment;

    public ApplicationsController(
        CernaHomeCareDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpPost("submit-with-resume")]
    [AllowAnonymous]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> SubmitWithResume([FromForm] ApplicationSubmitWithResumeRequest request)
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

        if (request.Resume == null || request.Resume.Length == 0)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Resume is required."
            });
        }

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
        var extension = Path.GetExtension(request.Resume.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Only PDF, DOC, and DOCX resume files are allowed."
            });
        }

        if (request.Resume.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Resume cannot exceed 10 MB."
            });
        }

        var candidate = new Candidate
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address?.Trim(),
            HasHcaPerId = request.HasHcaPerId?.Trim(),
            HowHeardAboutUs = request.HowHeardAboutUs?.Trim(),
            Status = "New",
            Source = "Website",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        var webRoot = _environment.WebRootPath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var uploadFolder = Path.Combine(
            webRoot,
            "uploads",
            "candidates",
            candidate.CandidateId.ToString()
        );

        Directory.CreateDirectory(uploadFolder);

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadFolder, safeFileName);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
        {
            await request.Resume.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/candidates/{candidate.CandidateId}/{safeFileName}";

        var candidateFile = new CandidateFile
        {
            CandidateId = candidate.CandidateId,
            FileName = safeFileName,
            OriginalFileName = Path.GetFileName(request.Resume.FileName),
            FilePath = relativePath,
            FileContentType = request.Resume.ContentType,
            FileSizeBytes = request.Resume.Length,
            UploadedUtc = DateTime.UtcNow
        };

        _context.CandidateFiles.Add(candidateFile);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Application submitted successfully.",
            candidateId = candidate.CandidateId,
            candidateFileId = candidateFile.CandidateFileId
        });
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