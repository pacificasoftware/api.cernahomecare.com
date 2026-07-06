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
public class JobsController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public JobsController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    private bool IsSuperAdmin()
    {
        return User.IsInRole("Super Admin");
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }

    private bool IsFranchisee()
    {
        return User.IsInRole("Franchisee");
    }

    private bool CanAccessAllFranchisees()
    {
        return IsSuperAdmin() || IsAdmin();
    }

    private int? GetUserFranchiseeId()
    {
        var value =
            User.FindFirst("FranchiseeId")?.Value ??
            User.FindFirst("franchiseeId")?.Value ??
            User.FindFirst("FranchiseeID")?.Value;

        return int.TryParse(value, out var franchiseeId)
            ? franchiseeId
            : null;
    }

    private async Task<bool> UserCanAccessFranchiseeAsync(int franchiseeId)
    {
        if (CanAccessAllFranchisees())
        {
            return true;
        }

        var userFranchiseeId = GetUserFranchiseeId();

        return userFranchiseeId.HasValue &&
               userFranchiseeId.Value == franchiseeId;
    } 

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _context.Jobs
            .Include(j => j.Franchisee)
            .AsQueryable();

        if (!CanAccessAllFranchisees())
        {
            var franchiseeId = GetUserFranchiseeId();

            if (!franchiseeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(j => j.FranchiseeId == franchiseeId.Value);
        }

        var jobs = await query
            .OrderBy(j => j.FranchiseeId)
            .ThenBy(j => j.SortOrder)
            .ThenBy(j => j.JobTitle)
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.Jobs
            .Include(j => j.Franchisee)
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (item == null)
        {
            return NotFound();
        }

        if (!await UserCanAccessFranchiseeAsync(item.FranchiseeId))
        {
            return Forbid();
        }

        return Ok(item);
    }

    [HttpGet("franchisee/{franchiseeId:int}")]
    public async Task<IActionResult> GetByFranchiseeId(int franchiseeId)
    {
        if (!await UserCanAccessFranchiseeAsync(franchiseeId))
        {
            return Forbid();
        }

        var jobs = await _context.Jobs
            .Where(j => j.FranchiseeId == franchiseeId)
            .OrderBy(j => j.SortOrder)
            .ThenBy(j => j.JobTitle)
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpGet("franchisee/{franchiseeId:int}/active")]
    public async Task<IActionResult> GetActiveByFranchiseeId(int franchiseeId)
    {
        if (!await UserCanAccessFranchiseeAsync(franchiseeId))
        {
            return Forbid();
        }

        var jobs = await _context.Jobs
            .Where(j => j.FranchiseeId == franchiseeId && j.IsActive)
            .OrderBy(j => j.SortOrder)
            .ThenBy(j => j.JobTitle)
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Jobs job)
    {
        if (!CanAccessAllFranchisees())
        {
            var franchiseeId = GetUserFranchiseeId();

            if (!franchiseeId.HasValue)
            {
                return Forbid();
            }

            job.FranchiseeId = franchiseeId.Value;
        }

        if (job.FranchiseeId <= 0)
        {
            return BadRequest("FranchiseeId is required.");
        }

        if (string.IsNullOrWhiteSpace(job.JobTitle))
        {
            return BadRequest("JobTitle is required.");
        }

        var franchiseeExists = await _context.Franchisees
            .AnyAsync(f => f.FranchiseeId == job.FranchiseeId);

        if (!franchiseeExists)
        {
            return BadRequest("Invalid FranchiseeId.");
        }

        job.JobId = 0;
        job.CreatedUtc = DateTime.UtcNow;
        job.UpdatedUtc = null;

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = job.JobId }, job);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Jobs job)
    {
        if (id != job.JobId)
        {
            return BadRequest("Route id does not match JobId.");
        }

        if (job.FranchiseeId <= 0)
        {
            return BadRequest("FranchiseeId is required.");
        }

        if (string.IsNullOrWhiteSpace(job.JobTitle))
        {
            return BadRequest("JobTitle is required.");
        }

        var existingJob = await _context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (existingJob == null)
        {
            return NotFound();
        }

        if (!CanAccessAllFranchisees())
        {
            var franchiseeId = GetUserFranchiseeId();

            if (!franchiseeId.HasValue || existingJob.FranchiseeId != franchiseeId.Value)
            {
                return Forbid();
            }

            // Prevent franchisee users from moving a job to another franchisee.
            job.FranchiseeId = franchiseeId.Value;
        }

        var franchiseeExists = await _context.Franchisees
            .AnyAsync(f => f.FranchiseeId == job.FranchiseeId);

        if (!franchiseeExists)
        {
            return BadRequest("Invalid FranchiseeId.");
        }

        job.CreatedUtc = existingJob.CreatedUtc;
        job.UpdatedUtc = DateTime.UtcNow;

        _context.Entry(job).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:int}/active")]
    public async Task<IActionResult> SetActiveStatus(int id, JobActiveRequest request)
    {
        var item = await _context.Jobs.FindAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        if (!await UserCanAccessFranchiseeAsync(item.FranchiseeId))
        {
            return Forbid();
        }

        item.IsActive = request.IsActive;
        item.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Jobs.FindAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        if (!await UserCanAccessFranchiseeAsync(item.FranchiseeId))
        {
            return Forbid();
        }

        item.IsActive = false;
        item.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class JobActiveRequest
{
    public bool IsActive { get; set; }
}