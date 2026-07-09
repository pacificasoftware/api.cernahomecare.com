using api.cernahomecare.com.Data;
using api.cernahomecare.com.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 

[ApiController]
[Route("api/public/jobs")]
[AllowAnonymous]
public class PublicJobsController : ControllerBase
{
   
    private readonly CernaHomeCareDbContext _context;
    private readonly GoogleGeocodingService _googleGeocodingService;
    public PublicJobsController(
     CernaHomeCareDbContext context,
     GoogleGeocodingService googleGeocodingService)
    {
        _context = context;
        _googleGeocodingService = googleGeocodingService;
    }

    [HttpGet("active/franchisee/{franchiseeId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveByFranchiseeId(int franchiseeId)
    {
        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(x =>
                x.FranchiseeId == franchiseeId &&
                x.IsActive
            )
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.JobTitle)
            .Select(x => new
            {
                x.JobId,
                x.FranchiseeId,
                FranchiseeName = x.Franchisee.FranchiseeName,
                FranchiseeCity = x.Franchisee.City,
                FranchiseeState = x.Franchisee.State,
                FranchiseeZipCode = x.Franchisee.ZipCode,

                x.JobTitle,
                x.JobType,
                x.ShiftType,
                x.JobDescription,
                x.City,
                x.State,
                x.ZipCode,
                x.PayRange,
                x.SortOrder,
                x.Latitude,
                x.Longitude
            })
            .ToListAsync();

        return Ok(new
        {
            franchiseeId,
            jobs
        });
    }

    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<IActionResult> GetPublicActiveJobs(
      [FromQuery] string? zipCode,
      [FromQuery] double? latitude,
      [FromQuery] double? longitude,
      [FromQuery] string? searchedLocation,
      [FromQuery] double radiusMiles = 50)
    {
        const double earthRadiusMiles = 3958.8;

        radiusMiles = Math.Clamp(radiusMiles, 1, 100);

        string searchedCity = searchedLocation ?? "";

        if (!string.IsNullOrWhiteSpace(zipCode))
        {
            zipCode = zipCode.Trim();

            if (zipCode.Length != 5 || !zipCode.All(char.IsDigit))
            {
                return BadRequest(new
                {
                    message = "Invalid ZIP code."
                });
            }
        }

        if ((!latitude.HasValue || !longitude.HasValue) && !string.IsNullOrWhiteSpace(zipCode))
        {
            var coordinates = await _googleGeocodingService.GetLatLongFromZipAsync(zipCode);

            if (coordinates == null)
            {
                return BadRequest(new
                {
                    message = "Invalid ZIP code."
                });
            }

            latitude = coordinates.Latitude;
            longitude = coordinates.Longitude;

            if (string.IsNullOrWhiteSpace(searchedCity))
            {
                searchedCity = coordinates.City;
            }
        }

        if (!latitude.HasValue || !longitude.HasValue)
        {
            return BadRequest(new
            {
                message = "ZIP code or coordinates are required."
            });
        }

        var searchLat = latitude.Value;
        var searchLng = longitude.Value;

        var activeJobs = await _context.Jobs
            .Include(j => j.Franchisee)
            .Where(j =>
                j.IsActive &&
                j.Franchisee != null &&
                j.Franchisee.IsActive &&
                j.Latitude != null &&
                j.Longitude != null)
            .ToListAsync();

        var jobs = activeJobs
            .Select(j =>
            {
                var distanceMiles = CalculateDistanceMiles(
                    searchLat,
                    searchLng,
                    j.Latitude!.Value,
                    j.Longitude!.Value
                );

                return new
                {
                    Job = j,
                    DistanceMiles = distanceMiles
                };
            })
            .Where(x => x.DistanceMiles <= radiusMiles)
            .OrderBy(x => x.DistanceMiles)
            .ThenBy(x => x.Job.SortOrder)
            .ThenBy(x => x.Job.JobTitle)
            .Select(x => new
            {
                x.Job.JobId,
                x.Job.FranchiseeId,

                FranchiseeName = x.Job.Franchisee!.FranchiseeName,
                FranchiseeCity = x.Job.Franchisee.City,
                FranchiseeState = x.Job.Franchisee.State,
                FranchiseeZipCode = x.Job.Franchisee.ZipCode,

                x.Job.JobTitle,
                x.Job.JobType,
                x.Job.ShiftType,
                x.Job.JobDescription,

                x.Job.City,
                x.Job.State,
                x.Job.ZipCode,
                x.Job.Latitude,
                x.Job.Longitude,

                x.Job.PayRange,
                x.Job.SortOrder,

                DistanceMiles = Math.Round(x.DistanceMiles, 1)
            })
            .ToList();

        return Ok(new
        {
            searchedZipCode = zipCode,
            searchedCity,
            radiusMiles,
            jobs
        });
    }
    private static double CalculateDistanceMiles(
                            double lat1,
                            double lon1,
                            double lat2,
                            double lon2)
    {
        const double earthRadiusMiles = 3958.8;

        var lat1Rad = lat1 * Math.PI / 180;
        var lat2Rad = lat2 * Math.PI / 180;
        var deltaLatRad = (lat2 - lat1) * Math.PI / 180;
        var deltaLonRad = (lon2 - lon1) * Math.PI / 180;

        var a =
            Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
            Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
            Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMiles * c;
    }

    [HttpGet("geocode/{zipCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GeocodeZip(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode) ||
            zipCode.Length != 5 ||
            !zipCode.All(char.IsDigit))
        {
            return BadRequest(new
            {
                message = "Invalid ZIP code."
            });
        }

        var coordinates = await _googleGeocodingService.GetLatLongFromZipAsync(zipCode);

        if (coordinates == null)
        {
            return NotFound(new
            {
                message = $"No coordinates found for ZIP {zipCode}."
            });
        }

        return Ok(new
        {
            zipCode,
            coordinates
        });
    }
}