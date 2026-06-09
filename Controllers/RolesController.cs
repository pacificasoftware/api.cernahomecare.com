using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Super Admin")]
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
        var roles = await _context.Roles
            .Where(x => x.IsActive)
            .OrderBy(x => x.RoleName)
            .Select(x => new
            {
                x.RoleId,
                x.RoleName,
                x.IsActive,
                x.CreatedUtc
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _context.Roles
            .Where(x => x.RoleId == id && x.IsActive)
            .Select(x => new
            {
                x.RoleId,
                x.RoleName,
                x.IsActive,
                x.CreatedUtc
            })
            .FirstOrDefaultAsync();

        return role == null ? NotFound() : Ok(role);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Role name is required."
            });
        }

        var roleName = request.RoleName.Trim();

        var exists = await _context.Roles
            .AnyAsync(x => x.RoleName == roleName);

        if (exists)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "A role with this name already exists."
            });
        }

        var role = new Role
        {
            RoleName = roleName,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = role.RoleId }, new
        {
            statusCode = 201,
            statusMessage = "Role created successfully.",
            role.RoleId,
            role.RoleName
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Role name is required."
            });
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(x => x.RoleId == id && x.IsActive);

        if (role == null)
        {
            return NotFound();
        }

        var roleName = request.RoleName.Trim();

        var duplicateExists = await _context.Roles
            .AnyAsync(x => x.RoleId != id && x.RoleName == roleName);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "A role with this name already exists."
            });
        }

        role.RoleName = roleName;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Role updated successfully."
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(x => x.RoleId == id && x.IsActive);

        if (role == null)
        {
            return NotFound();
        }

        var roleInUse = await _context.AdminUsers
            .AnyAsync(x => x.RoleId == id && !x.IsDeleted);

        if (roleInUse)
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "This role is assigned to one or more admin users and cannot be deleted."
            });
        }

        role.IsActive = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    public class CreateRoleRequest
    {
        public string RoleName { get; set; } = "";
    }

    public class UpdateRoleRequest
    {
        public string RoleName { get; set; } = "";
    }
}