using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public RolesController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Roles.ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.Roles.FindAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Role role)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = role.RoleId }, role);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Role role)
    {
        if (id != role.RoleId) return BadRequest();

        _context.Entry(role).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Roles.FindAsync(id);
        if (item == null) return NotFound();

        _context.Roles.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}