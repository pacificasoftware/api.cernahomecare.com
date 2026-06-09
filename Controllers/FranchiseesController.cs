using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Authorize(Roles = "Super Admin")]
public class FranchiseesController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public FranchiseesController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Franchisees.ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.Franchisees.FindAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Franchisee franchisee)
    {
        franchisee.CreatedUtc = DateTime.UtcNow;

        _context.Franchisees.Add(franchisee);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = franchisee.FranchiseeId }, franchisee);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Franchisee franchisee)
    {
        if (id != franchisee.FranchiseeId) return BadRequest();

        franchisee.UpdatedUtc = DateTime.UtcNow;

        _context.Entry(franchisee).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Franchisees.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        item.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}