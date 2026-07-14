using api.cernahomecare.com.Data;
using api.cernahomecare.com.Services;
using Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Super Admin,Admin,Franchisee")]
public class JobsController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;
    private readonly GoogleGeocodingService _googleGeocodingService;

    public JobsController(CernaHomeCareDbContext context, GoogleGeocodingService googleGeocodingService)
    {
        _context = context;
        _googleGeocodingService = googleGeocodingService;
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
            .AsNoTracking()
            .AsQueryable();

        if (!CanAccessAllFranchisees())
        {
            var franchiseeId = GetUserFranchiseeId();

            if (!franchiseeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(j =>
                j.FranchiseeId == franchiseeId.Value
            );
        }

        var jobs = await query
            .OrderBy(j => j.FranchiseeId)
            .ThenBy(j => j.SortOrder)
            .ThenBy(j => j.JobTitle)
            .Select(j => new
            {
                j.JobId,
                j.FranchiseeId,

                FranchiseeName = j.Franchisee != null
                    ? j.Franchisee.FranchiseeName
                    : null,

                j.JobTitle,
                j.JobType,
                j.ShiftType,
                j.JobDescription,
                j.City,
                j.State,
                j.ZipCode,
                j.PayRange,
                j.IsActive,
                j.SortOrder,
                j.Latitude,
                j.Longitude,
                j.CreatedUtc,
                j.UpdatedUtc
            })
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
    public async Task<IActionResult> Create(Job job)
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

        // Calculate and store latitude/longitude for radius-based public job search
        await PopulateJobCoordinatesAsync(job);

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = job.JobId }, job);
    }

    private async Task PopulateJobCoordinatesAsync(Job job)
    {
        job.ZipCode = job.ZipCode?.Trim();
        job.City = job.City?.Trim();
        job.State = job.State?.Trim();

        if (string.IsNullOrWhiteSpace(job.ZipCode))
        {
            return;
        }

        if (job.ZipCode.Length != 5 || !job.ZipCode.All(char.IsDigit))
        {
            return;
        }

        var coordinates = await _googleGeocodingService.GetLatLongFromZipAsync(job.ZipCode);

        if (coordinates == null)
        {
            throw new Exception($"Google geocoding returned NULL for ZIP {job.ZipCode}.");
        }

        job.Latitude = coordinates.Latitude;
        job.Longitude = coordinates.Longitude;
    }


    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Job job)
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

        var zipChanged =
            !string.Equals(
                existingJob.ZipCode?.Trim(),
                job.ZipCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );

        if (zipChanged || job.Latitude == null || job.Longitude == null)
        {
            await PopulateJobCoordinatesAsync(job);
        }
        else
        {
            job.Latitude = existingJob.Latitude;
            job.Longitude = existingJob.Longitude;
        }

        _context.Entry(job).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
        //return Ok(new
        //{
        //    job.JobId,
        //    job.ZipCode,
        //    job.Latitude,
        //    job.Longitude
        //});
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