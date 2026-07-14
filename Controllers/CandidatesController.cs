using api.cernahomecare.com.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Security.Claims;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Super Admin,Admin,Franchisee")]
public class CandidatesController : ControllerBase
{
	private readonly CernaHomeCareDbContext _context;

	private static readonly string[] AllowedStatuses =
	{
		"New",
		"Reviewing",
		"Contacted",
		"Interview Scheduled",
		"Hired",
		"Rejected",
		"Archived"
	};

	public CandidatesController(CernaHomeCareDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll(
		int page = 1,
		int pageSize = 25,
		string? search = null,
		string? status = null,
		int? franchiseeId = null)
	{
		page = Math.Max(page, 1);
		pageSize = Math.Clamp(pageSize, 1, 100);

		var role = GetRole();
		var userFranchiseeId = GetUserFranchiseeId();

		var query = _context.CandidateApplications
			.AsNoTracking()
			.Where(x =>
				x.IsActive &&
				x.Candidate != null &&
				x.Candidate.IsActive);

		if (!IsSuperAdmin(role))
		{
			if (!userFranchiseeId.HasValue)
			{
				return Forbid();
			}

			query = query.Where(x =>
				x.FranchiseeId == userFranchiseeId.Value);
		}
		else if (franchiseeId.HasValue)
		{
			query = query.Where(x =>
				x.FranchiseeId == franchiseeId.Value);
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			var s = search.Trim();

			query = query.Where(x =>
				(x.Candidate != null &&
				 (x.Candidate.FullName.Contains(s) ||
				  x.Candidate.Email.Contains(s) ||
				  x.Candidate.Phone.Contains(s))) ||
				(x.Job != null &&
				 x.Job.JobTitle.Contains(s)) ||
				(x.Franchisee != null &&
				 x.Franchisee.FranchiseeName.Contains(s)) ||
				(x.AssignedStaff != null &&
				 (x.AssignedStaff.FirstName.Contains(s) ||
				  x.AssignedStaff.LastName.Contains(s))));
		}

		if (!string.IsNullOrWhiteSpace(status))
		{
			var normalizedStatus = status.Trim();

			if (!AllowedStatuses.Contains(
					normalizedStatus,
					StringComparer.OrdinalIgnoreCase))
			{
				return BadRequest(new
				{
					statusCode = 400,
					statusMessage = "Invalid candidate status."
				});
			}

			query = query.Where(x =>
				x.Status == normalizedStatus);
		}

		var total = await query.CountAsync();

		var rows = await query
			.OrderByDescending(x => x.AppliedUtc)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(x => new
			{
				x.CandidateApplicationId,
				x.CandidateId,
				x.JobId,
				x.FranchiseeId,
				x.AssignedStaffId,

				FullName = x.Candidate!.FullName,
				Phone = x.Candidate.Phone,
				Email = x.Candidate.Email,
				Address = x.Candidate.Address,
				HasHcaPerId = x.Candidate.HasHcaPerId,
				HowHeardAboutUs = x.Candidate.HowHeardAboutUs,

				x.Status,
				x.Source,
				x.Notes,
				x.AppliedUtc,
				x.UpdatedUtc,

				Job = x.Job == null
					? null
					: new
					{
						x.Job.JobId,
						x.Job.JobTitle,
						x.Job.JobType,
						x.Job.ShiftType,
						x.Job.City,
						x.Job.State,
						x.Job.ZipCode,
						x.Job.PayRange
					},

				Franchisee = x.Franchisee == null
					? null
					: new
					{
						x.Franchisee.FranchiseeId,
						x.Franchisee.FranchiseeName
					},

				AssignedStaff = x.AssignedStaff == null
					? null
					: new
					{
						x.AssignedStaff.StaffId,
						x.AssignedStaff.FirstName,
						x.AssignedStaff.LastName,
						FullName =
							x.AssignedStaff.FirstName + " " +
							x.AssignedStaff.LastName,
						x.AssignedStaff.Email,
						x.AssignedStaff.JobTitle
					},

				FileCount = x.Candidate!.CandidateFiles.Count
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

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(int id)
	{
		var role = GetRole();
		var userFranchiseeId = GetUserFranchiseeId();

		var query = _context.CandidateApplications
			.AsNoTracking()
			.Where(x =>
				x.CandidateApplicationId == id &&
				x.IsActive &&
				x.Candidate != null &&
				x.Candidate.IsActive);

		if (!IsSuperAdmin(role))
		{
			if (!userFranchiseeId.HasValue)
			{
				return Forbid();
			}

			query = query.Where(x =>
				x.FranchiseeId == userFranchiseeId.Value);
		}

		var application = await query
			.Select(x => new
			{
				x.CandidateApplicationId,
				x.CandidateId,
				x.JobId,
				x.FranchiseeId,
				x.AssignedStaffId,
				x.Status,
				x.Notes,
				x.Source,
				x.AppliedUtc,
				x.UpdatedUtc,
				x.IsActive,

				Candidate = new
				{
					x.Candidate!.CandidateId,
					x.Candidate.FullName,
					x.Candidate.Phone,
					x.Candidate.Email,
					x.Candidate.Address,
					x.Candidate.HasHcaPerId,
					x.Candidate.HowHeardAboutUs,
					x.Candidate.Source,
					x.Candidate.CreatedUtc,
					x.Candidate.UpdatedUtc
				},

				Job = x.Job == null
					? null
					: new
					{
						x.Job.JobId,
						x.Job.JobTitle,
						x.Job.JobType,
						x.Job.ShiftType,
						x.Job.JobDescription,
						x.Job.City,
						x.Job.State,
						x.Job.ZipCode,
						x.Job.PayRange
					},

				Franchisee = x.Franchisee == null
					? null
					: new
					{
						x.Franchisee.FranchiseeId,
						x.Franchisee.FranchiseeName
					},

				AssignedStaff = x.AssignedStaff == null
					? null
					: new
					{
						x.AssignedStaff.StaffId,
						x.AssignedStaff.FirstName,
						x.AssignedStaff.LastName,
						FullName =
							x.AssignedStaff.FirstName + " " +
							x.AssignedStaff.LastName,
						x.AssignedStaff.Email,
						x.AssignedStaff.Phone,
						x.AssignedStaff.JobTitle
					},

				CandidateFiles = x.Candidate!.CandidateFiles
					.OrderByDescending(f => f.UploadedUtc)
					.Select(f => new
					{
						f.CandidateFileId,
						f.CandidateId,
						f.FileName,
						f.OriginalFileName,
						f.FilePath,
						f.FileContentType,
						f.FileSizeBytes,
						f.UploadedUtc
					})
					.ToList()
			})
			.FirstOrDefaultAsync();

		return application == null
			? NotFound(new
			{
				statusCode = 404,
				statusMessage = "Application not found."
			})
			: Ok(application);
	}

	[HttpPost]
	public async Task<IActionResult> Create(
		[FromBody] CreateCandidateRequest request)
	{
		if (request.JobId <= 0)
		{
			return BadRequest(new
			{
				statusCode = 400,
				statusMessage = "A valid job is required."
			});
		}

		if (string.IsNullOrWhiteSpace(request.FullName) ||
			string.IsNullOrWhiteSpace(request.Email) ||
			string.IsNullOrWhiteSpace(request.Phone))
		{
			return BadRequest(new
			{
				statusCode = 400,
				statusMessage =
					"Full name, email, and phone are required."
			});
		}

		var role = GetRole();
		var userFranchiseeId = GetUserFranchiseeId();

		var job = await _context.Jobs
			.FirstOrDefaultAsync(x =>
				x.JobId == request.JobId &&
				x.IsActive);

		if (job == null)
		{
			return BadRequest(new
			{
				statusCode = 400,
				statusMessage =
					"The selected job was not found or is inactive."
			});
		}

		if (!IsSuperAdmin(role))
		{
			if (!userFranchiseeId.HasValue)
			{
				return Forbid();
			}

			if (job.FranchiseeId != userFranchiseeId.Value)
			{
				return Forbid();
			}
		}

		if (request.AssignedStaffId.HasValue)
		{
			var validStaff = await _context.Staff.AnyAsync(x =>
				x.StaffId == request.AssignedStaffId.Value &&
				x.FranchiseeId == job.FranchiseeId &&
				x.IsActive);

			if (!validStaff)
			{
				return BadRequest(new
				{
					statusCode = 400,
					statusMessage =
						"The selected staff member is inactive or belongs to another franchisee."
				});
			}
		}

		var normalizedEmail =
			request.Email.Trim().ToLowerInvariant();

		var candidate = await _context.Candidates
			.FirstOrDefaultAsync(x =>
				x.Email == normalizedEmail &&
				x.IsActive);

		if (candidate == null)
		{
			candidate = new Candidate
			{
				FullName = request.FullName.Trim(),
				Phone = request.Phone.Trim(),
				Email = normalizedEmail,
				Address = NormalizeOptional(request.Address),
				HasHcaPerId =
					NormalizeOptional(request.HasHcaPerId),
				HowHeardAboutUs =
					NormalizeOptional(request.HowHeardAboutUs),
				Source = NormalizeOptional(request.Source) ?? "Admin",
				IsActive = true,
				CreatedUtc = DateTime.UtcNow
			};

			_context.Candidates.Add(candidate);
			await _context.SaveChangesAsync();
		}
		else
		{
			candidate.FullName = request.FullName.Trim();
			candidate.Phone = request.Phone.Trim();
			candidate.Address =
				NormalizeOptional(request.Address) ??
				candidate.Address;
			candidate.HasHcaPerId =
				NormalizeOptional(request.HasHcaPerId) ??
				candidate.HasHcaPerId;
			candidate.HowHeardAboutUs =
				NormalizeOptional(request.HowHeardAboutUs) ??
				candidate.HowHeardAboutUs;
			candidate.UpdatedUtc = DateTime.UtcNow;

			await _context.SaveChangesAsync();
		}

		var alreadyApplied =
			await _context.CandidateApplications.AnyAsync(x =>
				x.CandidateId == candidate.CandidateId &&
				x.JobId == job.JobId &&
				x.IsActive);

		if (alreadyApplied)
		{
			return Conflict(new
			{
				statusCode = 409,
				statusMessage =
					"This candidate already has an active application for this job."
			});
		}

		var application = new CandidateApplication
		{
			CandidateId = candidate.CandidateId,
			JobId = job.JobId,
			FranchiseeId = job.FranchiseeId,
			AssignedStaffId = request.AssignedStaffId,
			Status = "New",
			Notes = NormalizeOptional(request.Notes),
			Source = NormalizeOptional(request.Source) ?? "Admin",
			AppliedUtc = DateTime.UtcNow,
			IsActive = true
		};

		_context.CandidateApplications.Add(application);
		await _context.SaveChangesAsync();

		return CreatedAtAction(
			nameof(GetById),
			new { id = application.CandidateApplicationId },
			new
			{
				statusCode = 201,
				statusMessage =
					"Candidate application created successfully.",
				candidateId = candidate.CandidateId,
				candidateApplicationId =
					application.CandidateApplicationId,
				jobId = job.JobId,
				franchiseeId = job.FranchiseeId
			});
	}

	[HttpPut("{id:int}/status")]
	public async Task<IActionResult> UpdateStatus(
		int id,
		[FromBody] UpdateCandidateStatusRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Status))
		{
			return BadRequest(new
			{
				statusCode = 400,
				statusMessage = "Invalid candidate status."
			});
		}

		var normalizedStatus = AllowedStatuses
			.FirstOrDefault(x =>
				string.Equals(
					x,
					request.Status.Trim(),
					StringComparison.OrdinalIgnoreCase));

		if (normalizedStatus == null)
		{
			return BadRequest(new
			{
				statusCode = 400,
				statusMessage = "Invalid candidate status."
			});
		}

		var application =
			await GetAuthorizedApplicationAsync(id);

		if (application == null)
		{
			return NotFound(new
			{
				statusCode = 404,
				statusMessage = "Application not found."
			});
		}

		application.Status = normalizedStatus;
		application.UpdatedUtc = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			statusCode = 200,
			statusMessage = "Application status updated.",
			candidateApplicationId =
				application.CandidateApplicationId,
			application.Status
		});
	}

	[HttpPut("{id:int}/assign")]
	public async Task<IActionResult> AssignCandidate(
		int id,
		[FromBody] AssignCandidateRequest request)
	{
		var application =
			await GetAuthorizedApplicationAsync(id);

		if (application == null)
		{
			return NotFound(new
			{
				statusCode = 404,
				statusMessage = "Application not found."
			});
		}

		if (!request.AssignedStaffId.HasValue)
		{
			application.AssignedStaffId = null;
			application.UpdatedUtc = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			return Ok(new
			{
				statusCode = 200,
				statusMessage = "Staff assignment removed.",
				candidateApplicationId =
					application.CandidateApplicationId,
				assignedStaffId = (int?)null
			});
		}

		var staffExists = await _context.Staff.AnyAsync(x =>
			x.StaffId == request.AssignedStaffId.Value &&
			x.FranchiseeId == application.FranchiseeId &&
			x.IsActive);

		if (!staffExists)
		{
			return BadRequest(new
			{
				statusCode = 400,
				statusMessage =
					"The selected staff member was not found, is inactive, or belongs to another franchisee."
			});
		}

		application.AssignedStaffId =
			request.AssignedStaffId.Value;
		application.UpdatedUtc = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			statusCode = 200,
			statusMessage =
				"Application assigned successfully.",
			candidateApplicationId =
				application.CandidateApplicationId,
			assignedStaffId =
				application.AssignedStaffId
		});
	}

	[HttpPut("{id:int}/notes")]
	public async Task<IActionResult> UpdateNotes(
		int id,
		[FromBody] UpdateCandidateNotesRequest request)
	{
		var application =
			await GetAuthorizedApplicationAsync(id);

		if (application == null)
		{
			return NotFound(new
			{
				statusCode = 404,
				statusMessage = "Application not found."
			});
		}

		application.Notes =
			NormalizeOptional(request.Notes);
		application.UpdatedUtc = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			statusCode = 200,
			statusMessage = "Application notes updated.",
			candidateApplicationId =
				application.CandidateApplicationId
		});
	}

	[HttpPut("profile/{candidateId:int}")]
	public async Task<IActionResult> UpdateCandidateProfile(
		int candidateId,
		[FromBody] UpdateCandidateRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.FullName) ||
			string.IsNullOrWhiteSpace(request.Email) ||
			string.IsNullOrWhiteSpace(request.Phone))
		{
			return BadRequest(new
			{
				statusCode = 400,
				statusMessage =
					"Full name, email, and phone are required."
			});
		}

		var role = GetRole();
		var userFranchiseeId = GetUserFranchiseeId();

		if (!IsSuperAdmin(role))
		{
			if (!userFranchiseeId.HasValue)
			{
				return Forbid();
			}

			var belongsToFranchisee =
				await _context.CandidateApplications.AnyAsync(x =>
					x.CandidateId == candidateId &&
					x.FranchiseeId == userFranchiseeId.Value &&
					x.IsActive);

			if (!belongsToFranchisee)
			{
				return Forbid();
			}
		}

		var candidate = await _context.Candidates
			.FirstOrDefaultAsync(x =>
				x.CandidateId == candidateId &&
				x.IsActive);

		if (candidate == null)
		{
			return NotFound(new
			{
				statusCode = 404,
				statusMessage = "Candidate not found."
			});
		}

		candidate.FullName = request.FullName.Trim();
		candidate.Phone = request.Phone.Trim();
		candidate.Email =
			request.Email.Trim().ToLowerInvariant();
		candidate.Address =
			NormalizeOptional(request.Address);
		candidate.HasHcaPerId =
			NormalizeOptional(request.HasHcaPerId);
		candidate.HowHeardAboutUs =
			NormalizeOptional(request.HowHeardAboutUs);

		candidate.Source =
			NormalizeOptional(request.Source) ??
			candidate.Source;

		candidate.UpdatedUtc = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			statusCode = 200,
			statusMessage =
				"Candidate profile updated successfully.",
			candidate.CandidateId
		});
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(int id)
	{
		var application =
			await GetAuthorizedApplicationAsync(id);

		if (application == null)
		{
			return NotFound(new
			{
				statusCode = 404,
				statusMessage = "Application not found."
			});
		}

		application.IsActive = false;
		application.Status = "Archived";
		application.UpdatedUtc = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return NoContent();
	}

	private async Task<CandidateApplication?>
		GetAuthorizedApplicationAsync(int candidateApplicationId)
	{
		var role = GetRole();
		var userFranchiseeId = GetUserFranchiseeId();

		var query = _context.CandidateApplications
			.Where(x =>
				x.CandidateApplicationId ==
					candidateApplicationId &&
				x.IsActive);

		if (!IsSuperAdmin(role))
		{
			if (!userFranchiseeId.HasValue)
			{
				return null;
			}

			query = query.Where(x =>
				x.FranchiseeId ==
					userFranchiseeId.Value);
		}

		return await query.FirstOrDefaultAsync();
	}

	private string? GetRole()
	{
		return User.FindFirstValue(ClaimTypes.Role);
	}

	private int? GetUserFranchiseeId()
	{
		var value =
			User.FindFirstValue("FranchiseeId");

		return int.TryParse(value, out var franchiseeId)
			? franchiseeId
			: null;
	}

	private static bool IsSuperAdmin(string? role)
	{
		return string.Equals(
			role,
			"Super Admin",
			StringComparison.OrdinalIgnoreCase);
	}

	private static string? NormalizeOptional(
		string? value)
	{
		return string.IsNullOrWhiteSpace(value)
			? null
			: value.Trim();
	}
}
