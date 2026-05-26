using CernaHomeCare.AdminApi.Models;
using Microsoft.EntityFrameworkCore;

namespace api.cernahomecare.com.Data
{
    public class CernaHomeCareDbContext : DbContext
    {
        public CernaHomeCareDbContext(DbContextOptions<CernaHomeCareDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<Franchisee> Franchisees { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<CandidateFile> CandidateFiles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Role>().ToTable("Roles").HasKey(e => e.RoleId);
            builder.Entity<Franchisee>().ToTable("Franchisees").HasKey(e => e.FranchiseeId);
            builder.Entity<AdminUser>().ToTable("AdminUsers").HasKey(e => e.AdminUserId);
            builder.Entity<Candidate>().ToTable("Candidates").HasKey(e => e.CandidateId);
            builder.Entity<CandidateFile>().ToTable("CandidateFiles").HasKey(e => e.CandidateFileId);
            builder.Entity<AuditLog>().ToTable("AuditLogs").HasKey(e => e.AuditLogId);

            builder.Entity<AdminUser>()
                .HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId);

            builder.Entity<AdminUser>()
                .HasOne(e => e.Franchisee)
                .WithMany()
                .HasForeignKey(e => e.FranchiseeId);

            builder.Entity<Candidate>()
                .HasOne(e => e.Franchisee)
                .WithMany()
                .HasForeignKey(e => e.FranchiseeId);

            builder.Entity<Candidate>()
                .HasOne(e => e.AssignedAdminUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedAdminUserId);

            builder.Entity<CandidateFile>()
                .HasOne(e => e.Candidate)
                .WithMany(e => e.CandidateFiles)
                .HasForeignKey(e => e.CandidateId);

            builder.Entity<AuditLog>()
                .HasOne(e => e.AdminUser)
                .WithMany()
                .HasForeignKey(e => e.AdminUserId);
        }
    }
}