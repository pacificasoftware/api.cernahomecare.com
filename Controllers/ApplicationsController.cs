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

    //[HttpGet("test")]
    //[AllowAnonymous]
    //public IActionResult Test()
    //{
    //    return Ok(new
    //    {
    //        statusCode = 200,
    //        statusMessage = "Applications API is working.",
    //        utc = DateTime.UtcNow
    //    });
    //}

    [HttpPost("upload-debug")]
    [AllowAnonymous]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public IActionResult UploadDebug([FromForm] ApplicationSubmitWithResumeRequest request)
    {
        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Upload debug reached API.",
            fullName = request.FullName,
            email = request.Email,
            phone = request.Phone,
            resumeIsNull = request.Resume == null,
            resumeFileName = request.Resume?.FileName,
            resumeLength = request.Resume?.Length,
            resumeContentType = request.Resume?.ContentType
        });
    }

    //[HttpPost("post-test")]
    //[AllowAnonymous]
    //public IActionResult PostTest()
    //{
    //    return Ok(new
    //    {
    //        statusCode = 200,
    //        statusMessage = "POST test worked.",
    //        utc = DateTime.UtcNow
    //    });
    //}


    [HttpPost("submit-with-resume")]
    [AllowAnonymous]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> SubmitWithResume([FromForm] ApplicationSubmitWithResumeRequest request)
    {
        var step = "Started";

        try
        {
            step = "Validating required fields";

            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    statusMessage = "Full name, email, and phone are required.",
                    step
                });
            }

            step = "Validating resume exists";

            if (request.Resume == null || request.Resume.Length == 0)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    statusMessage = "Resume is required.",
                    step
                });
            }

            step = "Validating resume extension";

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(request.Resume.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    statusMessage = "Only PDF, DOC, and DOCX resume files are allowed.",
                    step,
                    fileName = request.Resume.FileName,
                    extension
                });
            }

            step = "Validating resume size";

            if (request.Resume.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    statusMessage = "Resume cannot exceed 10 MB.",
                    step,
                    resumeLength = request.Resume.Length
                });
            }

            step = "Creating candidate object";

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

            step = "Saving candidate to database";

            _context.Candidates.Add(candidate);
            await _context.SaveChangesAsync();

            step = "Preparing upload folder";

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var uploadFolder = Path.Combine(
                webRoot,
                "uploads",
                "candidates",
                candidate.CandidateId.ToString()
            );

            Directory.CreateDirectory(uploadFolder);

            step = "Saving resume file to disk";

            var safeFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadFolder, safeFileName);

            await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            {
                await request.Resume.CopyToAsync(stream);
            }

            step = "Creating CandidateFile object";

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

            step = "Saving CandidateFile to database";

            _context.CandidateFiles.Add(candidateFile);
            await _context.SaveChangesAsync();

            step = "Completed";

            return Ok(new
            {
                statusCode = 200,
                statusMessage = "Application submitted successfully.",
                step,
                candidateId = candidate.CandidateId,
                candidateFileId = candidateFile.CandidateFileId,
                filePath = relativePath
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                statusCode = 500,
                statusMessage = "Application submit failed on the API.",
                step,
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
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