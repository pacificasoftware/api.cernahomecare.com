using api.cernahomecare.com.Data;
using Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Super Admin,Admin")]
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
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.FranchiseeName)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.Franchisees
            .FirstOrDefaultAsync(x => x.FranchiseeId == id);

        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Franchisee franchisee)
    {
        franchisee.FranchiseeId = 0;

        if (string.IsNullOrWhiteSpace(franchisee.Slug))
        {
            franchisee.Slug = GenerateSlug(franchisee.FranchiseeName);
        }

        franchisee.IsActive = franchisee.IsActive;
        franchisee.IsPublished = franchisee.IsPublished;
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

    [HttpPut]
    public async Task<IActionResult> UpdateFromBody([FromBody] Franchisee franchisee)
    {
        if (franchisee.FranchiseeId <= 0)
        {
            return BadRequest(new { message = "Missing franchiseeId." });
        }

        return await UpdateInternal(franchisee.FranchiseeId, franchisee);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Franchisee franchisee)
    {
        return await UpdateInternal(id, franchisee);
    }

    private async Task<IActionResult> UpdateInternal(int id, Franchisee franchisee)
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
        item.TollFreePhone = franchisee.TollFreePhone;

        item.Address1 = franchisee.Address1;
        item.Address2 = franchisee.Address2;
        item.City = franchisee.City;
        item.State = franchisee.State;
        item.ZipCode = franchisee.ZipCode;

        item.Latitude = franchisee.Latitude;
        item.Longitude = franchisee.Longitude;

        item.Slug = string.IsNullOrWhiteSpace(franchisee.Slug)
            ? GenerateSlug(franchisee.FranchiseeName)
            : franchisee.Slug.Trim().ToLower();

        item.HeroImageUrl = franchisee.HeroImageUrl;
        item.CoverageTitle = franchisee.CoverageTitle;
        item.CoverageAreas = franchisee.CoverageAreas;

        item.PageTitle = franchisee.PageTitle;
        item.MetaDescription = franchisee.MetaDescription;
        item.ShortDescription = franchisee.ShortDescription;

        item.IsPublished = franchisee.IsPublished;
        item.SortOrder = franchisee.SortOrder;
        item.IsActive = franchisee.IsActive;

        item.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteFromBody([FromBody] DeleteFranchiseeRequest request)
    {
        if (request.FranchiseeId <= 0)
        {
            return BadRequest(new { message = "Missing franchiseeId." });
        }

        return await DeleteInternal(request.FranchiseeId);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await DeleteInternal(id);
    }

    private async Task<IActionResult> DeleteInternal(int id)
    {
        var item = await _context.Franchisees.FindAsync(id);

        if (item == null)
        {
            return NotFound(new { message = "Franchisee not found." });
        }

        item.IsActive = false;
        item.IsPublished = false;
        item.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string GenerateSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var slug = value
            .Trim()
            .ToLowerInvariant()
            .Replace("&", "and");

        var chars = slug
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        slug = new string(chars);

        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }
}

public class DeleteFranchiseeRequest
{
    public int FranchiseeId { get; set; }
}