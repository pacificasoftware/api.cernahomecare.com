using api.cernahomecare.com.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models; 
using System.Security.Claims;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public StaffController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    // GET: /api/Staff
    // GET: /api/Staff?page=1&pageSize=25
    // GET: /api/Staff?search=paul
    // GET: /api/Staff?franchiseeId=1
    // GET: /api/Staff?activeOnly=true
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] int? franchiseeId = null,
        [FromQuery] bool activeOnly = false)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _context.Staff
            .AsNoTracking()
            .AsQueryable();

        if (!CanAccessAllFranchisees())
        {
            var userFranchiseeId = GetUserFranchiseeId();

            if (!userFranchiseeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(x =>
                x.FranchiseeId == userFranchiseeId.Value
            );
        }
        else if (franchiseeId.HasValue && franchiseeId.Value > 0)
        {
            query = query.Where(x =>
                x.FranchiseeId == franchiseeId.Value
            );
        }

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchText = search.Trim();

            query = query.Where(x =>
                x.FirstName.Contains(searchText) ||
                x.LastName.Contains(searchText) ||
                (x.Email != null && x.Email.Contains(searchText)) ||
                (x.Phone != null && x.Phone.Contains(searchText)) ||
                (x.JobTitle != null && x.JobTitle.Contains(searchText))
            );
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.StaffId,
                x.FranchiseeId,

                FranchiseeName = x.Franchisee != null
                    ? x.Franchisee.FranchiseeName
                    : null,

                x.FirstName,
                x.LastName,

                FullName =
                    (x.FirstName + " " + x.LastName).Trim(),

                x.Email,
                x.Phone,
                x.JobTitle,
                x.IsActive,
                x.CreatedUtc,
                x.UpdatedUtc
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            rows
        });
    }

    // GET: /api/Staff/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var staff = await _context.Staff
            .AsNoTracking()
            .Where(x => x.StaffId == id)
            .Select(x => new
            {
                x.StaffId,
                x.FranchiseeId,

                FranchiseeName = x.Franchisee != null
                    ? x.Franchisee.FranchiseeName
                    : null,

                x.FirstName,
                x.LastName,

                FullName =
                    (x.FirstName + " " + x.LastName).Trim(),

                x.Email,
                x.Phone,
                x.JobTitle,
                x.IsActive,
                x.CreatedUtc,
                x.UpdatedUtc
            })
            .FirstOrDefaultAsync();

        if (staff == null)
        {
            return NotFound(new
            {
                message = "Staff member not found."
            });
        }

        if (!CanAccessFranchisee(staff.FranchiseeId))
        {
            return Forbid();
        }

        return Ok(staff);
    }

    // POST: /api/Staff
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStaffRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var email = NormalizeOptional(request.Email);
        var phone = NormalizeOptional(request.Phone);
        var jobTitle = NormalizeOptional(request.JobTitle);

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return BadRequest(new
            {
                message = "First name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return BadRequest(new
            {
                message = "Last name is required."
            });
        }

        var franchiseeId = request.FranchiseeId;

        if (!CanAccessAllFranchisees())
        {
            var userFranchiseeId = GetUserFranchiseeId();

            if (!userFranchiseeId.HasValue)
            {
                return Forbid();
            }

            // Prevent franchisee users from creating staff
            // under another franchise.
            franchiseeId = userFranchiseeId.Value;
        }

        var franchiseeExists = await _context.Franchisees
            .AsNoTracking()
            .AnyAsync(x =>
                x.FranchiseeId == franchiseeId &&
                x.IsActive
            );

        if (!franchiseeExists)
        {
            return BadRequest(new
            {
                message = "The selected franchisee was not found or is inactive."
            });
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var duplicateEmail = await _context.Staff
                .AsNoTracking()
                .AnyAsync(x =>
                    x.FranchiseeId == franchiseeId &&
                    x.Email != null &&
                    x.Email == email
                );

            if (duplicateEmail)
            {
                return Conflict(new
                {
                    message = "A staff member with this email already exists for this franchisee."
                });
            }
        }

        var staff = new Staff
        {
            FranchiseeId = franchiseeId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            JobTitle = jobTitle,
            IsActive = request.IsActive,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = null
        };

        _context.Staff.Add(staff);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = staff.StaffId },
            new
            {
                staff.StaffId,
                staff.FranchiseeId,
                staff.FirstName,
                staff.LastName,
                FullName =
                    $"{staff.FirstName} {staff.LastName}".Trim(),
                staff.Email,
                staff.Phone,
                staff.JobTitle,
                staff.IsActive,
                staff.CreatedUtc,
                staff.UpdatedUtc
            }
        );
    }

    // PUT: /api/Staff/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateStaffRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var staff = await _context.Staff
            .FirstOrDefaultAsync(x => x.StaffId == id);

        if (staff == null)
        {
            return NotFound(new
            {
                message = "Staff member not found."
            });
        }

        if (!CanAccessFranchisee(staff.FranchiseeId))
        {
            return Forbid();
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var email = NormalizeOptional(request.Email);
        var phone = NormalizeOptional(request.Phone);
        var jobTitle = NormalizeOptional(request.JobTitle);

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return BadRequest(new
            {
                message = "First name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return BadRequest(new
            {
                message = "Last name is required."
            });
        }

        var targetFranchiseeId = request.FranchiseeId;

        if (!CanAccessAllFranchisees())
        {
            var userFranchiseeId = GetUserFranchiseeId();

            if (!userFranchiseeId.HasValue)
            {
                return Forbid();
            }

            targetFranchiseeId = userFranchiseeId.Value;
        }

        var franchiseeExists = await _context.Franchisees
            .AsNoTracking()
            .AnyAsync(x =>
                x.FranchiseeId == targetFranchiseeId &&
                x.IsActive
            );

        if (!franchiseeExists)
        {
            return BadRequest(new
            {
                message = "The selected franchisee was not found or is inactive."
            });
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var duplicateEmail = await _context.Staff
                .AsNoTracking()
                .AnyAsync(x =>
                    x.StaffId != id &&
                    x.FranchiseeId == targetFranchiseeId &&
                    x.Email != null &&
                    x.Email == email
                );

            if (duplicateEmail)
            {
                return Conflict(new
                {
                    message = "Another staff member with this email already exists for this franchisee."
                });
            }
        }

        staff.FranchiseeId = targetFranchiseeId;
        staff.FirstName = firstName;
        staff.LastName = lastName;
        staff.Email = email;
        staff.Phone = phone;
        staff.JobTitle = jobTitle;
        staff.IsActive = request.IsActive;
        staff.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            staff.StaffId,
            staff.FranchiseeId,
            staff.FirstName,
            staff.LastName,
            FullName =
                $"{staff.FirstName} {staff.LastName}".Trim(),
            staff.Email,
            staff.Phone,
            staff.JobTitle,
            staff.IsActive,
            staff.CreatedUtc,
            staff.UpdatedUtc
        });
    }

    // PUT: /api/Staff/5/status
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateStaffStatusRequest request)
    {
        var staff = await _context.Staff
            .FirstOrDefaultAsync(x => x.StaffId == id);

        if (staff == null)
        {
            return NotFound(new
            {
                message = "Staff member not found."
            });
        }

        if (!CanAccessFranchisee(staff.FranchiseeId))
        {
            return Forbid();
        }

        staff.IsActive = request.IsActive;
        staff.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            staff.StaffId,
            staff.IsActive,
            staff.UpdatedUtc
        });
    }

    // DELETE: /api/Staff/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var staff = await _context.Staff
            .FirstOrDefaultAsync(x => x.StaffId == id);

        if (staff == null)
        {
            return NotFound(new
            {
                message = "Staff member not found."
            });
        }

        if (!CanAccessFranchisee(staff.FranchiseeId))
        {
            return Forbid();
        }

        // Check whether the staff member is assigned to applications.
        var hasAssignedApplications =
            await _context.CandidateApplications
                .AsNoTracking()
                .AnyAsync(x => x.AssignedStaffId == id);

        if (hasAssignedApplications)
        {
            return Conflict(new
            {
                message =
                    "This staff member is assigned to one or more candidate applications. Deactivate the staff member instead of deleting them."
            });
        }

        _context.Staff.Remove(staff);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CanAccessAllFranchisees()
    {
        var role = GetUserRole();

        return role.Equals(
                   "Super Admin",
                   StringComparison.OrdinalIgnoreCase
               ) ||
               role.Equals(
                   "Admin",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    private bool CanAccessFranchisee(int franchiseeId)
    {
        if (CanAccessAllFranchisees())
        {
            return true;
        }

        var userFranchiseeId = GetUserFranchiseeId();

        return userFranchiseeId.HasValue &&
               userFranchiseeId.Value == franchiseeId;
    }

    private int? GetUserFranchiseeId()
    {
        var value =
            User.FindFirst("franchiseeId")?.Value ??
            User.FindFirst("FranchiseeId")?.Value ??
            User.FindFirst("franchisee_id")?.Value;

        return int.TryParse(value, out var franchiseeId)
            ? franchiseeId
            : null;
    }

    private string GetUserRole()
    {
        return
            User.FindFirst(ClaimTypes.Role)?.Value ??
            User.FindFirst("role")?.Value ??
            User.FindFirst("Role")?.Value ??
            string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}