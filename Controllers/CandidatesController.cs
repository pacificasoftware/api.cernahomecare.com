using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CandidatesController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public CandidatesController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var candidates = await _context.Candidates
            .Include(x => x.Franchisee)
            .Include(x => x.AssignedAdminUser)
            .Include(x => x.CandidateFiles)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync();

        return Ok(candidates);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var candidate = await _context.Candidates
            .Include(x => x.Franchisee)
            .Include(x => x.AssignedAdminUser)
            .Include(x => x.CandidateFiles)
            .FirstOrDefaultAsync(x => x.CandidateId == id);

        return candidate == null ? NotFound() : Ok(candidate);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Candidate candidate)
    {
        candidate.CreatedUtc = DateTime.UtcNow;
        candidate.Status ??= "New";
        candidate.Source ??= "Website";
        candidate.IsActive = true;

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = candidate.CandidateId }, candidate);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Candidate candidate)
    {
        if (id != candidate.CandidateId) return BadRequest();

        candidate.UpdatedUtc = DateTime.UtcNow;

        _context.Entry(candidate).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var candidate = await _context.Candidates.FindAsync(id);
        if (candidate == null) return NotFound();

        candidate.IsActive = false;
        candidate.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}