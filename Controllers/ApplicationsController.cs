using api.cernahomecare.com.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] AllowedResumeExtensions =
    {
        ".pdf",
        ".doc",
        ".docx"
    };

    private const long MaximumResumeSizeBytes = 10 * 1024 * 1024;

    public ApplicationsController(
        CernaHomeCareDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpPost("upload-debug")]
    [AllowAnonymous]
    [RequestSizeLimit(MaximumResumeSizeBytes)]
    public IActionResult UploadDebug(
        [FromForm] ApplicationSubmitWithResumeRequest request)
    {
        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Upload debug reached API.",
            jobId = request.JobId,
            fullName = request.FullName,
            email = request.Email,
            phone = request.Phone,
            resumeIsNull = request.Resume == null,
            resumeFileName = request.Resume?.FileName,
            resumeLength = request.Resume?.Length,
            resumeContentType = request.Resume?.ContentType
        });
    }

    [HttpPost("submit-with-resume")]
    [AllowAnonymous]
    [RequestSizeLimit(MaximumResumeSizeBytes)]
    public async Task<IActionResult> SubmitWithResume(
        [FromForm] ApplicationSubmitWithResumeRequest request)
    {
        string step = "Started";
        string? savedPhysicalFilePath = null;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            step = "Validating request";

            IActionResult? validationError = ValidateApplicationRequest(
                request.JobId,
                request.FullName,
                request.Email,
                request.Phone);

            if (validationError != null)
            {
                return validationError;
            }

            step = "Validating resume";

            IActionResult? resumeValidationError =
                ValidateResume(request.Resume);

            if (resumeValidationError != null)
            {
                return resumeValidationError;
            }

            step = "Loading selected job";

            Job? job = await _context.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.JobId == request.JobId &&
                    x.IsActive);

            if (job == null)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    statusMessage =
                        "The selected job could not be found or is no longer active.",
                    step
                });
            }

            step = "Finding or creating candidate";

            Candidate candidate = await FindOrCreateCandidateAsync(
                request.FullName,
                request.Email,
                request.Phone,
                request.Address,
                request.HasHcaPerId,
                request.HowHeardAboutUs);

            step = "Checking for duplicate application";

            bool alreadyApplied =
                await _context.CandidateApplications.AnyAsync(x =>
                    x.CandidateId == candidate.CandidateId &&
                    x.JobId == job.JobId &&
                    x.IsActive);

            if (alreadyApplied)
            {
                return Conflict(new
                {
                    statusCode = 409,
                    statusMessage =
                        "This candidate has already applied for the selected job.",
                    candidateId = candidate.CandidateId,
                    jobId = job.JobId,
                    step
                });
            }

            step = "Creating candidate application";

            var candidateApplication = new CandidateApplication
            {
                CandidateId = candidate.CandidateId,
                JobId = job.JobId,

                // Always derive this from the job.
                FranchiseeId = job.FranchiseeId,

                AssignedStaffId = null,
                Status = "New",
                Notes = null,
                Source = "Website",
                AppliedUtc = DateTime.UtcNow,
                IsActive = true
            };

            _context.CandidateApplications.Add(candidateApplication);
            await _context.SaveChangesAsync();

            step = "Preparing upload folder";

            IFormFile resume = request.Resume!;

            string extension = Path
                .GetExtension(resume.FileName)
                .ToLowerInvariant();

            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot");

            string uploadFolder = Path.Combine(
                webRoot,
                "uploads",
                "candidates",
                candidate.CandidateId.ToString());

            Directory.CreateDirectory(uploadFolder);

            step = "Saving resume file";

            string safeFileName = $"{Guid.NewGuid():N}{extension}";

            savedPhysicalFilePath = Path.Combine(
                uploadFolder,
                safeFileName);

            await using (var stream = new FileStream(
                savedPhysicalFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await resume.CopyToAsync(stream);
            }

            step = "Creating candidate file";

            string relativePath =
                $"/uploads/candidates/" +
                $"{candidate.CandidateId}/" +
                $"{safeFileName}";

            var candidateFile = new CandidateFile
            {
                CandidateId = candidate.CandidateId,
                FileName = safeFileName,
                OriginalFileName =
                    Path.GetFileName(resume.FileName),
                FilePath = relativePath,
                FileContentType = resume.ContentType,
                FileSizeBytes = resume.Length,
                UploadedUtc = DateTime.UtcNow
            };

            _context.CandidateFiles.Add(candidateFile);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            step = "Completed";

            return Ok(new
            {
                statusCode = 200,
                statusMessage =
                    "Application submitted successfully.",
                step,
                candidateId = candidate.CandidateId,
                candidateApplicationId =
                    candidateApplication.CandidateApplicationId,
                candidateFileId =
                    candidateFile.CandidateFileId,
                jobId = job.JobId,
                franchiseeId = job.FranchiseeId,
                filePath = relativePath
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            if (!string.IsNullOrWhiteSpace(savedPhysicalFilePath) &&
                System.IO.File.Exists(savedPhysicalFilePath))
            {
                try
                {
                    System.IO.File.Delete(savedPhysicalFilePath);
                }
                catch
                {
                    // Do not hide the original application error
                    // if file cleanup fails.
                }
            }

            return StatusCode(500, new
            {
                statusCode = 500,
                statusMessage =
                    "Application submit failed on the API.",
                step,
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("submit")]
    [AllowAnonymous]
    public async Task<IActionResult> Submit(
        [FromBody] ApplicationSubmitRequest request)
    {
        string step = "Started";

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            step = "Validating request";

            IActionResult? validationError = ValidateApplicationRequest(
                request.JobId,
                request.FullName,
                request.Email,
                request.Phone);

            if (validationError != null)
            {
                return validationError;
            }

            step = "Loading selected job";

            Job? job = await _context.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.JobId == request.JobId &&
                    x.IsActive);

            if (job == null)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    statusMessage =
                        "The selected job could not be found or is no longer active.",
                    step
                });
            }

            step = "Finding or creating candidate";

            Candidate candidate = await FindOrCreateCandidateAsync(
                request.FullName,
                request.Email,
                request.Phone,
                request.Address,
                request.HasHcaPerId,
                request.HowHeardAboutUs);

            step = "Checking for duplicate application";

            bool alreadyApplied =
                await _context.CandidateApplications.AnyAsync(x =>
                    x.CandidateId == candidate.CandidateId &&
                    x.JobId == job.JobId &&
                    x.IsActive);

            if (alreadyApplied)
            {
                return Conflict(new
                {
                    statusCode = 409,
                    statusMessage =
                        "This candidate has already applied for the selected job.",
                    candidateId = candidate.CandidateId,
                    jobId = job.JobId,
                    step
                });
            }

            step = "Creating candidate application";

            var candidateApplication = new CandidateApplication
            {
                CandidateId = candidate.CandidateId,
                JobId = job.JobId,
                FranchiseeId = job.FranchiseeId,
                AssignedStaffId = null,
                Status = "New",
                Source = "Website",
                AppliedUtc = DateTime.UtcNow,
                IsActive = true
            };

            _context.CandidateApplications.Add(candidateApplication);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            step = "Completed";

            return Ok(new
            {
                statusCode = 200,
                statusMessage =
                    "Application submitted successfully.",
                step,
                candidateId = candidate.CandidateId,
                candidateApplicationId =
                    candidateApplication.CandidateApplicationId,
                jobId = job.JobId,
                franchiseeId = job.FranchiseeId
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new
            {
                statusCode = 500,
                statusMessage =
                    "Application submit failed on the API.",
                step,
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    private IActionResult? ValidateApplicationRequest(
        int jobId,
        string? fullName,
        string? email,
        string? phone)
    {
        if (jobId <= 0)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "A valid job is required."
            });
        }

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(phone))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage =
                    "Full name, email, and phone are required."
            });
        }

        return null;
    }

    private IActionResult? ValidateResume(IFormFile? resume)
    {
        if (resume == null || resume.Length == 0)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Resume is required."
            });
        }

        string extension = Path
            .GetExtension(resume.FileName)
            .ToLowerInvariant();

        if (!AllowedResumeExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage =
                    "Only PDF, DOC, and DOCX resume files are allowed.",
                fileName = resume.FileName,
                extension
            });
        }

        if (resume.Length > MaximumResumeSizeBytes)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage =
                    "Resume cannot exceed 10 MB.",
                resumeLength = resume.Length
            });
        }

        return null;
    }

    private async Task<Candidate> FindOrCreateCandidateAsync(
        string fullName,
        string email,
        string phone,
        string? address,
        string? hasHcaPerId,
        string? howHeardAboutUs)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();

        Candidate? candidate = await _context.Candidates
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == normalizedEmail &&
                x.IsActive);

        if (candidate == null)
        {
            candidate = new Candidate
            {
                FullName = fullName.Trim(),
                Email = normalizedEmail,
                Phone = phone.Trim(),
                Address = NormalizeOptional(address),
                HasHcaPerId = NormalizeOptional(hasHcaPerId),
                HowHeardAboutUs =
                    NormalizeOptional(howHeardAboutUs),
                Source = "Website",
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            _context.Candidates.Add(candidate);
            await _context.SaveChangesAsync();

            return candidate;
        }

        /*
         * Refresh the candidate's basic information from the
         * most recent submission.
         */
        candidate.FullName = fullName.Trim();
        candidate.Phone = phone.Trim();
        candidate.Address =
            NormalizeOptional(address) ?? candidate.Address;
        candidate.HasHcaPerId =
            NormalizeOptional(hasHcaPerId)
            ?? candidate.HasHcaPerId;
        candidate.HowHeardAboutUs =
            NormalizeOptional(howHeardAboutUs)
            ?? candidate.HowHeardAboutUs;
        candidate.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return candidate;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}