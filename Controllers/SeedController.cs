using api.cernahomecare.com.Data;
using Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public SeedController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

  // [HttpPost("CreateSuperAdmin")]
    //public async Task<IActionResult> CreateSuperAdmin()
    //{
    //    var role = await _context.Roles
    //        .FirstOrDefaultAsync(x => x.RoleName == "Super Admin");

    //    if (role == null)
    //    {
    //        role = new Role
    //        {
    //            RoleName = "Super Admin",
    //            IsActive = true,
    //            CreatedUtc = DateTime.UtcNow
    //        };

    //        _context.Roles.Add(role);
    //        await _context.SaveChangesAsync();
    //    }

    //    var email = "paul.amand@gmail.com";

    //    var existingUser = await _context.AdminUsers
    //        .FirstOrDefaultAsync(x => x.Email == email);

    //    if (existingUser != null)
    //    {
    //        return Ok(new
    //        {
    //            statusCode = 200,
    //            statusMessage = "Super admin already exists.",
    //            email
    //        });
    //    }

    //    var admin = new AdminUser
    //    {
    //        Email = email,
    //        UserName = "paul",
    //        FullName = "Paul Amand",
    //        RoleId = role.RoleId,
    //        IsActive = true,
    //        IsDeleted = false,
    //        CreatedUtc = DateTime.UtcNow
    //    };

    //    var hasher = new PasswordHasher<AdminUser>();
    //    admin.PasswordHash = hasher.HashPassword(admin, "Welcome@123!");

    //    _context.AdminUsers.Add(admin);
    //    await _context.SaveChangesAsync();

    //    return Ok(new
    //    {
    //        statusCode = 200,
    //        statusMessage = "Super admin created successfully.",
    //        email,
    //        password = "Welcome@123!"
    //    });
    //}
}