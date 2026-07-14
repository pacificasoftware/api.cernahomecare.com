using Microsoft.EntityFrameworkCore;
using Models;

namespace api.cernahomecare.com.Data;

public class CernaHomeCareDbContext : DbContext
{
	public CernaHomeCareDbContext(
		DbContextOptions<CernaHomeCareDbContext> options)
		: base(options)
	{
	}

	public DbSet<Role> Roles { get; set; } = null!;

	public DbSet<Franchisee> Franchisees { get; set; } = null!;

	public DbSet<AdminUser> AdminUsers { get; set; } = null!;

	public DbSet<Candidate> Candidates { get; set; } = null!;

	public DbSet<CandidateApplication> CandidateApplications { get; set; } =
		null!;

	public DbSet<CandidateFile> CandidateFiles { get; set; } = null!;

	public DbSet<Staff> Staff { get; set; } = null!;

	public DbSet<AuditLog> AuditLogs { get; set; } = null!;

	public DbSet<Job> Jobs { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.Entity<Role>(entity =>
		{
			entity.ToTable("RoleMaster");

			entity.HasKey(e => e.RoleId);
		});

		builder.Entity<Franchisee>(entity =>
		{
			entity.ToTable("Franchisees");

			entity.HasKey(e => e.FranchiseeId);

			entity.Property(e => e.FranchiseeName)
				.HasMaxLength(255)
				.IsRequired();
		});

		builder.Entity<AdminUser>(entity =>
		{
			entity.ToTable("AdminUser");

			entity.HasKey(e => e.AdminUserId);

			entity.Property(e => e.AdminUserId)
				.HasColumnName("AdminId");

			entity.HasOne(e => e.Role)
				.WithMany()
				.HasForeignKey(e => e.RoleId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(e => e.Franchisee)
				.WithMany()
				.HasForeignKey(e => e.FranchiseeId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<Candidate>(entity =>
		{
			entity.ToTable("Candidates");

			entity.HasKey(e => e.CandidateId);

			entity.Property(e => e.FullName)
				.HasMaxLength(200)
				.IsRequired();

			entity.Property(e => e.Phone)
				.HasMaxLength(50)
				.IsRequired();

			entity.Property(e => e.Email)
				.HasMaxLength(255)
				.IsRequired();

			entity.Property(e => e.Address)
				.HasMaxLength(500);

			entity.Property(e => e.HasHcaPerId)
				.HasMaxLength(50);

			entity.Property(e => e.HowHeardAboutUs)
				.HasMaxLength(250);

			entity.Property(e => e.Source)
				.HasMaxLength(100);

			entity.Property(e => e.IsActive)
				.HasDefaultValue(true);

			entity.Property(e => e.CreatedUtc)
				.HasDefaultValueSql("SYSUTCDATETIME()");

			entity.HasIndex(e => e.Email)
				.HasDatabaseName("IX_Candidates_Email");
		});

		builder.Entity<CandidateApplication>(entity =>
		{
			entity.ToTable("CandidateApplications");

			entity.HasKey(e => e.CandidateApplicationId);

			entity.Property(e => e.Status)
				.HasMaxLength(50)
				.IsRequired()
				.HasDefaultValue("New");

			entity.Property(e => e.Source)
				.HasMaxLength(100);

			entity.Property(e => e.AppliedUtc)
				.HasDefaultValueSql("SYSUTCDATETIME()");

			entity.Property(e => e.IsActive)
				.HasDefaultValue(true);

			entity.HasOne(e => e.Candidate)
				.WithMany(e => e.CandidateApplications)
				.HasForeignKey(e => e.CandidateId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(e => e.Job)
				.WithMany(e => e.CandidateApplications)
				.HasForeignKey(e => e.JobId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(e => e.Franchisee)
				.WithMany(e => e.CandidateApplications)
				.HasForeignKey(e => e.FranchiseeId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(e => e.AssignedStaff)
				.WithMany(e => e.CandidateApplications)
				.HasForeignKey(e => e.AssignedStaffId)
				.OnDelete(DeleteBehavior.SetNull);

			entity.HasIndex(e => e.CandidateId)
				.HasDatabaseName(
					"IX_CandidateApplications_CandidateId");

			entity.HasIndex(e => e.JobId)
				.HasDatabaseName(
					"IX_CandidateApplications_JobId");

			entity.HasIndex(e => e.FranchiseeId)
				.HasDatabaseName(
					"IX_CandidateApplications_FranchiseeId");

			entity.HasIndex(e => e.AssignedStaffId)
				.HasDatabaseName(
					"IX_CandidateApplications_AssignedStaffId");

			entity.HasIndex(e => new
			{
				e.CandidateId,
				e.JobId
			})
			.IsUnique()
			.HasFilter("[IsActive] = 1")
			.HasDatabaseName(
				"UX_CandidateApplications_CandidateId_JobId_Active");
		});

		builder.Entity<CandidateFile>(entity =>
		{
			entity.ToTable("CandidateFiles");

			entity.HasKey(e => e.CandidateFileId);

			entity.Property(e => e.FileName)
				.HasMaxLength(255)
				.IsRequired();

			entity.Property(e => e.OriginalFileName)
				.HasMaxLength(255);

			entity.Property(e => e.FilePath)
				.HasMaxLength(1000)
				.IsRequired();

			entity.Property(e => e.FileContentType)
				.HasMaxLength(150);

			entity.Property(e => e.UploadedUtc)
				.HasDefaultValueSql("SYSUTCDATETIME()");

			entity.HasOne(e => e.Candidate)
				.WithMany(e => e.CandidateFiles)
				.HasForeignKey(e => e.CandidateId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<Job>(entity =>
		{
			entity.ToTable("Jobs");

			entity.HasKey(e => e.JobId);

			entity.Property(e => e.JobTitle)
				.HasMaxLength(255)
				.IsRequired();

			entity.Property(e => e.JobType)
				.HasMaxLength(100);

			entity.Property(e => e.ShiftType)
				.HasMaxLength(100);

			entity.Property(e => e.City)
				.HasMaxLength(150);

			entity.Property(e => e.State)
				.HasMaxLength(50);

			entity.Property(e => e.ZipCode)
				.HasMaxLength(20);

			entity.Property(e => e.PayRange)
				.HasMaxLength(100);

			entity.Property(e => e.IsActive)
				.HasDefaultValue(true);

			entity.Property(e => e.CreatedUtc)
				.HasDefaultValueSql("SYSUTCDATETIME()");

			entity.HasOne(e => e.Franchisee)
				.WithMany(e => e.Jobs)
				.HasForeignKey(e => e.FranchiseeId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<Staff>(entity =>
		{
			entity.ToTable("Staff");

			entity.HasKey(e => e.StaffId);

			entity.Property(e => e.FirstName)
				.HasMaxLength(100)
				.IsRequired();

			entity.Property(e => e.LastName)
				.HasMaxLength(100)
				.IsRequired();

			entity.Property(e => e.Email)
				.HasMaxLength(255);

			entity.Property(e => e.Phone)
				.HasMaxLength(50);

			entity.Property(e => e.JobTitle)
				.HasMaxLength(150);

			entity.Property(e => e.IsActive)
				.HasDefaultValue(true);

			entity.Property(e => e.CreatedUtc)
				.HasDefaultValueSql("SYSUTCDATETIME()");

			entity.HasIndex(e => e.FranchiseeId)
				.HasDatabaseName("IX_Staff_FranchiseeId");

			entity.HasOne(e => e.Franchisee)
				.WithMany(e => e.StaffMembers)
				.HasForeignKey(e => e.FranchiseeId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<AuditLog>(entity =>
		{
			entity.ToTable("AuditLogs");

			entity.HasKey(e => e.AuditLogId);

			entity.HasOne(e => e.AdminUser)
				.WithMany()
				.HasForeignKey(e => e.AdminUserId)
				.OnDelete(DeleteBehavior.SetNull);
		});
	}
}