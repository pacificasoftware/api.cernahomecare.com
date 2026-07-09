using Microsoft.AspNetCore.Mvc;

namespace api.cernahomecare.com.Controllers
{
    using api.cernahomecare.com.Data;
    using api.cernahomecare.com.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [ApiController]
    [Route("api/public/franchisees")]
    public class PublicFranchiseesController : ControllerBase
    {
        private readonly CernaHomeCareDbContext _db;

        public PublicFranchiseesController(CernaHomeCareDbContext db)
        {
            _db = db;
        }

        [HttpGet("active/franchisee/{franchiseeId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveByFranchiseeId(int franchiseeId)
        {
            var jobs = await _db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.FranchiseeId == franchiseeId &&
                    x.IsActive
                )
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.JobTitle)
                .ToListAsync();

            return Ok(new
            {
                jobs
            });
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var requestedSlug = (slug ?? "")
                .Trim()
                .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(requestedSlug))
            {
                return BadRequest(new { message = "Missing slug." });
            }

            var item = await _db.Franchisees
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.IsActive &&
                    x.IsPublished &&
                    x.Slug != null &&
                    x.Slug.Trim().ToLower() == requestedSlug
                );

            if (item == null)
            {
                var possibleMatches = await _db.Franchisees
                    .AsNoTracking()
                    .Where(x =>
                        x.FranchiseeName.Contains("Orange") ||
                        (x.Slug != null && x.Slug.Contains("orange"))
                    )
                    .Select(x => new
                    {
                        x.FranchiseeId,
                        x.FranchiseeName,
                        x.Slug,
                        SlugLength = x.Slug == null ? 0 : x.Slug.Length,
                        x.City,
                        x.State,
                        x.IsActive,
                        x.IsPublished
                    })
                    .ToListAsync();

                var databaseName = _db.Database
                    .GetDbConnection()
                    .Database;

                return NotFound(new
                {
                    message = "Franchisee not found.",
                    requestedSlug,
                    databaseName,
                    possibleMatches
                });
            }

            return Ok(new
            {
                franchiseeId = item.FranchiseeId,
                slug = item.Slug?.Trim(),
                name = item.FranchiseeName,
                city = item.City,
                state = item.State,
                phone = FormatPhone(item.Phone),
                phoneHref = ToPhoneHref(item.Phone),
                jobsZip = item.ZipCode
            });
        }

        private static string CreateSlug(string? franchiseeName, string? city)
        {
            var value = franchiseeName ?? city ?? "";

            value = value
                .Replace("Cerna Home Care", "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToLowerInvariant();

            value = value
                .Replace("&", "and")
                .Replace(",", "")
                .Replace(".", "")
                .Replace("'", "")
                .Replace("/", "-")
                .Replace("_", "-")
                .Replace(" ", "-");

            while (value.Contains("--"))
            {
                value = value.Replace("--", "-");
            }

            return value.Trim('-');
        }
        private static string FormatPhone(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && digits.StartsWith("1"))
            {
                digits = digits.Substring(1);
            }

            if (digits.Length != 10)
            {
                return phone;
            }

            return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        }

        private static string ToPhoneHref(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 10)
            {
                digits = "1" + digits;
            }

            return $"tel:{digits}";
        }
    }
}
