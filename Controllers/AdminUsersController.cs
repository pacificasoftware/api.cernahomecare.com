using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminUsersController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public AdminUsersController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.AdminUsers
            .Include(x => x.Role)
            .Include(x => x.Franchisee)
            .Select(x => new
            {
                x.AdminUserId,
                x.RoleId,
                RoleName = x.Role != null ? x.Role.RoleName : null,
                x.FranchiseeId,
                FranchiseName = x.Franchisee != null ? x.Franchisee.FranchiseName : null,
                x.FirstName,
                x.LastName,
                x.Email,
                x.Phone,
                x.IsActive,
                x.LastLoginUtc,
                x.CreatedUtc,
                x.UpdatedUtc
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.AdminUsers
            .Include(x => x.Role)
            .Include(x => x.Franchisee)
            .Where(x => x.AdminUserId == id)
            .Select(x => new
            {
                x.AdminUserId,
                x.RoleId,
                RoleName = x.Role != null ? x.Role.RoleName : null,
                x.FranchiseeId,
                FranchiseName = x.Franchisee != null ? x.Franchisee.FranchiseName : null,
                x.FirstName,
                x.LastName,
                x.Email,
                x.Phone,
                x.IsActive,
                x.LastLoginUtc,
                x.CreatedUtc,
                x.UpdatedUtc
            })
            .FirstOrDefaultAsync();

        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AdminUser user)
    {
        user.CreatedUtc = DateTime.UtcNow;
        user.UpdatedUtc = null;
        user.IsActive = true;

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return BadRequest("Password is required.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

        _context.AdminUsers.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.AdminUserId }, new
        {
            user.AdminUserId,
            user.Email
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AdminUser input)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user == null) return NotFound();

        user.RoleId = input.RoleId;
        user.FranchiseeId = input.FranchiseeId;
        user.FirstName = input.FirstName;
        user.LastName = input.LastName;
        user.Email = input.Email;
        user.Phone = input.Phone;
        user.IsActive = input.IsActive;
        user.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> UpdatePassword(int id, [FromBody] string password)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(password))
        {
            return BadRequest("Password is required.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        user.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}