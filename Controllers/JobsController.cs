using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Super Admin")]
public class JobsController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public JobsController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var jobs = await _context.Jobs
            .Include(j => j.Franchisee)
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

        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("franchisee/{franchiseeId:int}")]
    public async Task<IActionResult> GetByFranchiseeId(int franchiseeId)
    {
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
            return BadRequest();
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

        item.IsActive = request.IsActive;
        item.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("public/active")]
    public async Task<IActionResult> GetPublicActiveJobs()
    {
        var jobs = await _context.Jobs
            .Include(j => j.Franchisee)
            .Where(j => j.IsActive && j.Franchisee != null && j.Franchisee.IsActive)
            .OrderBy(j => j.FranchiseeId)
            .ThenBy(j => j.SortOrder)
            .ThenBy(j => j.JobTitle)
            .Select(j => new
            {
                j.JobId,
                j.FranchiseeId,
                FranchiseeName = j.Franchisee!.FranchiseeName,
                FranchiseeCity = j.Franchisee.City,
                FranchiseeState = j.Franchisee.State,
                FranchiseeZipCode = j.Franchisee.ZipCode,

                j.JobTitle,
                j.JobType,
                j.ShiftType,
                j.JobDescription,
                j.City,
                j.State,
                j.ZipCode,
                j.PayRange,
                j.SortOrder
            })
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Jobs.FindAsync(id);

        if (item == null)
        {
            return NotFound();
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