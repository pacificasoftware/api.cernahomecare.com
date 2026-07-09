using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Super Admin,Admin,Franchisee")] 
public class CandidatesController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    private static readonly string[] AllowedStatuses =
    {
        "New",
        "Reviewing",
        "Contacted",
        "Interview Scheduled",
        "Hired",
        "Rejected",
        "Archived"
    };

    public CandidatesController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        string? status = null,
        int? franchiseeId = null)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var role = User.FindFirstValue(ClaimTypes.Role);
        var userFranchiseeIdValue = User.FindFirstValue("FranchiseeId");

        int? userFranchiseeId = int.TryParse(userFranchiseeIdValue, out var parsedFranchiseeId)
            ? parsedFranchiseeId
            : null;

        var query = _context.Candidates
            .Include(x => x.Franchisee)
            .Include(x => x.AssignedAdminUser)
            .Include(x => x.CandidateFiles)
            .Where(x => x.IsActive)
            .AsQueryable();

        // Normal admins only see their own franchisee applicants.
        if (role != "Super Admin")
        {
            if (!userFranchiseeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(x => x.FranchiseeId == userFranchiseeId.Value);
        }
        else if (franchiseeId.HasValue)
        {
            query = query.Where(x => x.FranchiseeId == franchiseeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();

            query = query.Where(x =>
                x.FullName.Contains(s) ||
                x.Email.Contains(s) ||
                x.Phone.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!AllowedStatuses.Contains(status))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    statusMessage = "Invalid candidate status."
                });
            }

            query = query.Where(x => x.Status == status);
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(x => x.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.CandidateId,
                x.FullName,
                x.Phone,
                x.Email,
                x.Address,
                x.HasHcaPerId,
                x.HowHeardAboutUs,
                x.Status,
                x.Source,
                x.CreatedUtc,
                x.UpdatedUtc,
                Franchisee = x.Franchisee == null ? null : new
                {
                    x.Franchisee.FranchiseeId,
                    x.Franchisee.FranchiseeName
                },
                AssignedAdminUser = x.AssignedAdminUser == null ? null : new
                {
                    x.AssignedAdminUser.AdminUserId,
                    x.AssignedAdminUser.FullName,
                    x.AssignedAdminUser.Email
                },
                FileCount = x.CandidateFiles.Count
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            rows
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userFranchiseeIdValue = User.FindFirstValue("FranchiseeId");

        int? userFranchiseeId = int.TryParse(userFranchiseeIdValue, out var parsedFranchiseeId)
            ? parsedFranchiseeId
            : null;

        var query = _context.Candidates
            .Where(x => x.CandidateId == id && x.IsActive)
            .AsQueryable();

        if (role != "Super Admin")
        {
            if (!userFranchiseeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(x => x.FranchiseeId == userFranchiseeId.Value);
        }

        var candidate = await query
            .Select(x => new
            {
                x.CandidateId,
                x.FranchiseeId,
                x.AssignedAdminUserId,
                x.FullName,
                x.Phone,
                x.Email,
                x.Address,
                x.HasHcaPerId,
                x.HowHeardAboutUs,
                x.Status,
                x.Notes,
                x.Source,
                x.IsActive,
                x.CreatedUtc,
                x.UpdatedUtc,
                Franchisee = x.Franchisee == null ? null : new
                {
                    x.Franchisee.FranchiseeId,
                    x.Franchisee.FranchiseeName
                },
                AssignedAdminUser = x.AssignedAdminUser == null ? null : new
                {
                    x.AssignedAdminUser.AdminUserId,
                    x.AssignedAdminUser.FullName,
                    x.AssignedAdminUser.Email
                },
                CandidateFiles = x.CandidateFiles.Select(f => new
                {
                    f.CandidateFileId,
                    f.CandidateId,
                    f.FileName,
                    f.OriginalFileName,
                    f.FilePath,
                    f.FileContentType,
                    f.FileSizeBytes,
                    f.UploadedUtc
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return candidate == null ? NotFound() : Ok(candidate);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCandidateRequest request)
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

        var role = User.FindFirstValue(ClaimTypes.Role);
        var userFranchiseeIdValue = User.FindFirstValue("FranchiseeId");

        int? userFranchiseeId = int.TryParse(userFranchiseeIdValue, out var parsedFranchiseeId)
            ? parsedFranchiseeId
            : null;

        var franchiseeId = request.FranchiseeId;

        if (franchiseeId <= 0)
        {
            if (role != "Super Admin" && userFranchiseeId.HasValue)
            {
                franchiseeId = userFranchiseeId.Value;
            }
            else
            {
                var firstFranchisee = await _context.Franchisees
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.FranchiseeId)
                    .FirstOrDefaultAsync();

                if (firstFranchisee == null)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        statusMessage = "No active franchisee exists."
                    });
                }

                franchiseeId = firstFranchisee.FranchiseeId;
            }
        }

        var candidate = new Candidate
        {
            FranchiseeId = franchiseeId,
            AssignedAdminUserId = request.AssignedAdminUserId,
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim(),
            Address = request.Address?.Trim(),
            HasHcaPerId = request.HasHcaPerId,
            HowHeardAboutUs = request.HowHeardAboutUs,
            Status = "New",
            Notes = request.Notes,
            Source = request.Source ?? "Admin",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = candidate.CandidateId }, new
        {
            statusCode = 201,
            statusMessage = "Candidate created successfully.",
            candidate.CandidateId
        });
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateCandidateStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status) ||
            !AllowedStatuses.Contains(request.Status))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Invalid candidate status."
            });
        }

        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(x => x.CandidateId == id && x.IsActive);

        if (candidate == null)
        {
            return NotFound();
        }

        candidate.Status = request.Status;
        candidate.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Candidate status updated."
        });
    }

    [HttpPut("{id:int}/assign")]
    public async Task<IActionResult> AssignCandidate(int id, AssignCandidateRequest request)
    {
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(x => x.CandidateId == id && x.IsActive);

        if (candidate == null)
        {
            return NotFound();
        }

        var adminExists = await _context.AdminUsers
            .AnyAsync(x => x.AdminUserId == request.AssignedAdminUserId &&
                           x.IsActive &&
                           !x.IsDeleted);

        if (!adminExists)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Assigned admin user not found."
            });
        }

        candidate.AssignedAdminUserId = request.AssignedAdminUserId;
        candidate.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Candidate assigned successfully."
        });
    }

    [HttpPut("{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes(int id, UpdateCandidateNotesRequest request)
    {
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(x => x.CandidateId == id && x.IsActive);

        if (candidate == null)
        {
            return NotFound();
        }

        candidate.Notes = request.Notes;
        candidate.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Candidate notes updated."
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCandidateRequest request)
    {
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(x => x.CandidateId == id && x.IsActive);

        if (candidate == null)
        {
            return NotFound(new
            {
                statusCode = 404,
                statusMessage = "Candidate not found."
            });
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Full name is required."
            });
        }

        candidate.FullName = request.FullName.Trim();
        candidate.Phone = request.Phone?.Trim() ?? "";
        candidate.Email = request.Email?.Trim() ?? "";
        candidate.Address = request.Address?.Trim();
        candidate.HasHcaPerId = request.HasHcaPerId?.Trim();
        candidate.HowHeardAboutUs = request.HowHeardAboutUs;
        candidate.Status = string.IsNullOrWhiteSpace(request.Status)
            ? candidate.Status
            : request.Status;
        candidate.Notes = request.Notes;
        candidate.Source = string.IsNullOrWhiteSpace(request.Source)
            ? candidate.Source
            : request.Source;
        candidate.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Candidate updated successfully.",
            candidate.CandidateId
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(x => x.CandidateId == id && x.IsActive);

        if (candidate == null)
        {
            return NotFound();
        }

        candidate.IsActive = false;
        candidate.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}