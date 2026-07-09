using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CandidateFilesController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] AllowedExtensions =
    {
        ".pdf",
        ".doc",
        ".docx",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public CandidateFilesController(
        CernaHomeCareDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("candidate/{candidateId:int}")]
    public async Task<IActionResult> GetByCandidateId(int candidateId)
    {
        var candidateExists = await _context.Candidates
            .AnyAsync(x => x.CandidateId == candidateId && x.IsActive);

        if (!candidateExists)
        {
            return NotFound(new
            {
                statusCode = 404,
                statusMessage = "Candidate not found."
            });
        }

        var files = await _context.CandidateFiles
            .Where(x => x.CandidateId == candidateId)
            .OrderByDescending(x => x.UploadedUtc)
            .Select(x => new
            {
                x.CandidateFileId,
                x.CandidateId,
                x.FileName,
                x.OriginalFileName,
                x.FilePath,
                x.FileContentType,
                x.FileSizeBytes,
                x.UploadedUtc
            })
            .ToListAsync();

        return Ok(files);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var file = await _context.CandidateFiles
            .Where(x => x.CandidateFileId == id)
             .Select(x => new
             {
                 x.CandidateFileId,
                 x.CandidateId,
                 x.FileName,
                 x.OriginalFileName,
                 x.FilePath,
                 x.FileContentType,
                 x.FileSizeBytes,
                 x.UploadedUtc
             })
            .FirstOrDefaultAsync();

        return file == null ? NotFound() : Ok(file);
    }

    [HttpPost("candidate/{candidateId:int}/upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(int candidateId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "No file was uploaded."
            });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "File size cannot exceed 10 MB."
            });
        }

        var candidateExists = await _context.Candidates
            .AnyAsync(x => x.CandidateId == candidateId && x.IsActive);

        if (!candidateExists)
        {
            return NotFound(new
            {
                statusCode = 404,
                statusMessage = "Candidate not found."
            });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "File type is not allowed."
            });
        }

        var uploadsRoot = Path.Combine(
            _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            "uploads",
            "candidates",
            candidateId.ToString()
        );

        Directory.CreateDirectory(uploadsRoot);

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, safeFileName);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/candidates/{candidateId}/{safeFileName}";

        var candidateFile = new CandidateFile
        {
            CandidateId = candidateId,
            FileName = safeFileName,
            OriginalFileName = Path.GetFileName(file.FileName),
            FilePath = relativePath,
            FileContentType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedUtc = DateTime.UtcNow
        };

        _context.CandidateFiles.Add(candidateFile);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "File uploaded successfully.",
            candidateFile.CandidateFileId,
            candidateFile.CandidateId,
            candidateFile.OriginalFileName,
            candidateFile.FileContentType,
            candidateFile.FileSizeBytes,
            candidateFile.UploadedUtc
        });
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var candidateFile = await _context.CandidateFiles
            .FirstOrDefaultAsync(x => x.CandidateFileId == id);

        if (candidateFile == null)
        {
            return NotFound();
        }

        var webRoot = _environment.WebRootPath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var relativePath = candidateFile.FilePath
            .TrimStart('/')
            .Replace("/", Path.DirectorySeparatorChar.ToString());

        var fullPath = Path.Combine(webRoot, relativePath);

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound(new
            {
                statusCode = 404,
                statusMessage = "File not found on server."
            });
        }

        var contentType = string.IsNullOrWhiteSpace(candidateFile.FileContentType)
            ? "application/octet-stream"
            : candidateFile.FileContentType;

        var downloadName = string.IsNullOrWhiteSpace(candidateFile.OriginalFileName)
            ? candidateFile.FileName
            : candidateFile.OriginalFileName;

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);

        return File(bytes, contentType, downloadName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var candidateFile = await _context.CandidateFiles
            .FirstOrDefaultAsync(x => x.CandidateFileId == id);

        if (candidateFile == null)
        {
            return NotFound();
        }

        var webRoot = _environment.WebRootPath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var relativePath = candidateFile.FilePath
            .TrimStart('/')
            .Replace("/", Path.DirectorySeparatorChar.ToString());

        var fullPath = Path.Combine(webRoot, relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }

        _context.CandidateFiles.Remove(candidateFile);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}