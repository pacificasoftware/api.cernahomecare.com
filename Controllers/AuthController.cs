using api.cernahomecare.com.Models;
using CernaHomeCare.AdminApi.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CernaHomeCare.AdminApi.Controllers;

[Authorize(Roles = "Super Admin")]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    [HttpPost("GetAdminLogin")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> GetAdminLogin([FromForm] AdminLoginRequest login)
    {
        if (login == null ||
            string.IsNullOrWhiteSpace(login.Email) ||
            string.IsNullOrWhiteSpace(login.Password))
        {
            return BadRequest(new
            {
                statusCode = 400,
                statusMessage = "Email and password are required."
            });
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var parameters = new DynamicParameters();
        parameters.Add("@Email", login.Email.Trim(), DbType.String);

        var user = await conn.QueryFirstOrDefaultAsync<AdminLoginResult>(
            "SP_GetAdminLoginDetails",
            parameters,
            commandType: CommandType.StoredProcedure
        );


        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return Unauthorized(new
            {
                statusCode = 401,
                statusMessage = "Invalid credentials."
            });
        }

        var hasher = new PasswordHasher<AdminLoginResult>();

        var passwordResult = hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            login.Password
        );

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                statusCode = 401,
                statusMessage = "Invalid credentials."
            });
        }

        var secret = _configuration["JWT:ServerSecret"];

        if (string.IsNullOrWhiteSpace(secret) ||
            Encoding.UTF8.GetBytes(secret).Length < 32)
        {
            return StatusCode(500, new
            {
                statusCode = 500,
                statusMessage = "JWT:ServerSecret is missing or too short."
            });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.AdminUserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.RoleName ?? "Admin"),
            new Claim("AdminUserId", user.AdminUserId.ToString()),
            new Claim("FranchiseeId", user.FranchiseeId?.ToString() ?? "")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret.Trim()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            )
        );

        await conn.ExecuteAsync(
            "UPDATE dbo.AdminUser SET LastLoginUtc = SYSUTCDATETIME() WHERE AdminId = @AdminId",
            new { AdminId = user.AdminUserId }
        );

        return Ok(new
        {
            statusCode = 200,
            statusMessage = "Success",
            user.AdminUserId,
            user.FullName,
            user.Email,
            user.RoleId,
            user.RoleName,
            user.FranchiseeId,
            user.FranchiseeName,
            token
        });
    }
 

    [Authorize(Roles = "Super Admin")] 
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"
            SELECT
                au.AdminId AS AdminUserId,
                au.AdminId AS AdminId,
                au.Email,
                au.UserName,
                au.FullName,
                au.AvatarUrl,
                au.RoleId,
                rm.RoleName,
                au.FranchiseeId,
                f.FranchiseeName,
                au.IsActive,
                au.IsDeleted,
                au.CreatedUtc,
                au.UpdatedUtc,
                au.LastLoginUtc
            FROM dbo.AdminUser au
            LEFT JOIN dbo.RoleMaster rm
                ON au.RoleId = rm.RoleId
            LEFT JOIN dbo.Franchisees f
                ON au.FranchiseeId = f.FranchiseeId
            WHERE ISNULL(au.IsDeleted, 0) = 0
            ORDER BY au.FullName, au.Email;
        ";

        var users = await conn.QueryAsync(sql);

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"
            SELECT
                au.AdminId AS AdminUserId,
                au.AdminId AS AdminId,
                au.Email,
                au.UserName,
                au.FullName,
                au.AvatarUrl,
                au.RoleId,
                rm.RoleName,
                au.FranchiseeId,
                f.FranchiseeName,
                au.IsActive,
                au.IsDeleted,
                au.CreatedUtc,
                au.UpdatedUtc,
                au.LastLoginUtc
            FROM dbo.AdminUser au
            LEFT JOIN dbo.RoleMaster rm
                ON au.RoleId = rm.RoleId
            LEFT JOIN dbo.Franchisees f
                ON au.FranchiseeId = f.FranchiseeId
            WHERE au.AdminId = @Id
              AND ISNULL(au.IsDeleted, 0) = 0;
        ";

        var user = await conn.QueryFirstOrDefaultAsync(sql, new { Id = id });

        return user == null ? NotFound() : Ok(user);
    }

    [Authorize(Roles = "Super Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Email and password are required."
            });
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var existingUser = await conn.QueryFirstOrDefaultAsync<int?>(
            @"
        SELECT AdminId
        FROM dbo.AdminUser
        WHERE Email = @Email
          AND ISNULL(IsDeleted, 0) = 0;
        ",
            new { Email = request.Email.Trim() }
        );

        if (existingUser.HasValue)
        {
            return BadRequest(new
            {
                message = "A user with this email already exists."
            });
        }

        var roleId = await conn.QueryFirstOrDefaultAsync<int?>(
            @"
        SELECT RoleId
        FROM dbo.RoleMaster
        WHERE RoleName = @RoleName
          AND IsActive = 1;
        ",
            new { RoleName = request.RoleName?.Trim() ?? "Admin" }
        );

        if (!roleId.HasValue)
        {
            return BadRequest(new
            {
                message = "Invalid role selected."
            });
        }

        var hasher = new PasswordHasher<CreateAdminUserRequest>();

        var passwordHash = hasher.HashPassword(
            request,
            request.Password
        );

        var sql = @"
        INSERT INTO dbo.AdminUser
        (
            Email,
            PasswordHash,
            RoleId,
            UserName,
            FullName,
            FranchiseeId,
            IsActive,
            IsDeleted,
            CreatedUtc,
            UpdatedUtc
        )
        VALUES
        (
            @Email,
            @PasswordHash,
            @RoleId,
            @UserName,
            @FullName,
            @FranchiseeId,
            @IsActive,
            0,
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        );

        SELECT CAST(SCOPE_IDENTITY() AS int);
    ";

        var newAdminId = await conn.ExecuteScalarAsync<int>(
            sql,
            new
            {
                Email = request.Email.Trim(),
                PasswordHash = passwordHash,
                RoleId = roleId.Value,
                UserName = string.IsNullOrWhiteSpace(request.UserName)
                    ? request.Email.Trim()
                    : request.UserName.Trim(),
                FullName = request.FullName?.Trim(),
                FranchiseeId = request.FranchiseeId,
                IsActive = request.IsActive
            }
        );

        return Ok(new
        {
            message = "Admin user created successfully.",
            adminId = newAdminId
        });
    }

    [Authorize(Roles = "Super Admin")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] DeleteAdminUserRequest request)
    {
        var adminId = request.AdminId ?? request.AdminUserId ?? 0;

        if (adminId <= 0)
        {
            return BadRequest(new
            {
                message = "Missing admin user ID."
            });
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var affectedRows = await conn.ExecuteAsync(
            @"
        UPDATE dbo.AdminUser
        SET
            IsDeleted = 1,
            IsActive = 0,
            UpdatedUtc = SYSUTCDATETIME()
        WHERE AdminId = @AdminId
          AND ISNULL(IsDeleted, 0) = 0;
        ",
            new { AdminId = adminId }
        );

        if (affectedRows == 0)
        {
            return NotFound(new
            {
                message = "Admin user not found."
            });
        }

        return NoContent();
    }
} 