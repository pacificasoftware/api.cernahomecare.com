using api.cernahomecare.com.Data;
using api.cernahomecare.com.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection.Emit;

[ApiController]
[Route("api/public/jobs")]
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

        var baseQuery = _context.Jobs
            .Include(j => j.Franchisee)
            .Where(j =>
                j.IsActive &&
                j.Franchisee != null &&
                j.Franchisee.IsActive);

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
            var allJobs = await baseQuery
                .OrderBy(j => j.FranchiseeId)
                .ThenBy(j => j.SortOrder)
                .ThenBy(j => j.JobTitle)
                .Select(j => new
                {
                    j.JobId,
                    j.FranchiseeId,
                    FranchiseeName = j.Franchisee!.FranchiseeName,
                    FranchiseeCity = j.Franchisee.City,
                    FranchiseeState = j.Franchisee.State,
                    FranchiseeZipCode = j.Franchisee.ZipCode,

                    j.JobTitle,
                    j.JobType,
                    j.ShiftType,
                    j.JobDescription,
                    j.City,
                    j.State,
                    j.ZipCode,
                    j.Latitude,
                    j.Longitude,
                    j.PayRange,
                    j.SortOrder,

                    DistanceMiles = (double?)null
                })
                .ToListAsync();

            return Ok(new
            {
                searchedZipCode = zipCode,
                searchedCity,
                jobs = allJobs
            });
        }

        var searchLat = latitude.Value;
        var searchLng = longitude.Value;

        var jobs = await baseQuery
            .Where(j => j.Latitude != null && j.Longitude != null)
            .Select(j => new
            {
                Job = j,
                DistanceMiles =
                    earthRadiusMiles *
                    Math.Acos(
                        Math.Cos(searchLat * Math.PI / 180) *
                        Math.Cos(j.Latitude!.Value * Math.PI / 180) *
                        Math.Cos((j.Longitude!.Value - searchLng) * Math.PI / 180) +
                        Math.Sin(searchLat * Math.PI / 180) *
                        Math.Sin(j.Latitude!.Value * Math.PI / 180)
                    )
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
            .ToListAsync();

        return Ok(new
        {
            searchedZipCode = zipCode,
            searchedCity,
            jobs
        });
    }
}