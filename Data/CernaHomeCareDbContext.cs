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
        public DbSet<Jobs> Jobs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Role>(entity =>
            {
                entity.ToTable("RoleMaster");
                entity.HasKey(e => e.RoleId);
            });

            builder.Entity<Franchisee>(entity =>
            {
                entity.ToTable("Franchisees");
                entity.HasKey(e => e.FranchiseeId);
            });

            builder.Entity<AdminUser>(entity =>
            {
                entity.ToTable("AdminUser");
                entity.HasKey(e => e.AdminUserId);

                entity.Property(e => e.AdminUserId)
                    .HasColumnName("AdminId");

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId);

                entity.HasOne(e => e.Franchisee)
                    .WithMany()
                    .HasForeignKey(e => e.FranchiseeId);
            });

            builder.Entity<Candidate>(entity =>
            {
                entity.ToTable("Candidates");
                entity.HasKey(e => e.CandidateId);

                entity.HasOne(e => e.Franchisee)
                    .WithMany()
                    .HasForeignKey(e => e.FranchiseeId);

                entity.HasOne(e => e.AssignedAdminUser)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedAdminUserId);
            });

            builder.Entity<CandidateFile>(entity =>
            {
                entity.ToTable("CandidateFiles");
                entity.HasKey(e => e.CandidateFileId);

                entity.HasOne(e => e.Candidate)
                    .WithMany(e => e.CandidateFiles)
                    .HasForeignKey(e => e.CandidateId);
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.AuditLogId);

                entity.HasOne(e => e.AdminUser)
                    .WithMany()
                    .HasForeignKey(e => e.AdminUserId);
            });
        }
    }
}