using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var items = await _context.Franchisees
            .Where(x => x.IsActive)
            .OrderBy(x => x.FranchiseeName)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.Franchisees
            .FirstOrDefaultAsync(x => x.FranchiseeId == id && x.IsActive);

        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Franchisee franchisee)
    {
        franchisee.FranchiseeId = 0;
        franchisee.IsActive = true;
        franchisee.CreatedUtc = DateTime.UtcNow;
        franchisee.UpdatedUtc = DateTime.UtcNow;

        _context.Franchisees.Add(franchisee);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = franchisee.FranchiseeId },
            franchisee
        );
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Franchisee franchisee)
    {
        var item = await _context.Franchisees.FindAsync(id);

        if (item == null)
        {
            return NotFound(new { message = "Franchisee not found." });
        }

        item.FranchiseeName = franchisee.FranchiseeName;
        item.ContactName = franchisee.ContactName;
        item.Email = franchisee.Email;
        item.Phone = franchisee.Phone;
        item.Address1 = franchisee.Address1;
        item.Address2 = franchisee.Address2;
        item.City = franchisee.City;
        item.State = franchisee.State;
        item.ZipCode = franchisee.ZipCode;
        item.IsActive = franchisee.IsActive;
        item.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Franchisees.FindAsync(id);

        if (item == null)
        {
            return NotFound(new { message = "Franchisee not found." });
        }

        item.IsActive = false;
        item.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}