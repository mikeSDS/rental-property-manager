using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Models;

namespace RentalPropertyManager.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<UnitType> UnitTypes { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<ApplicationApplicant> ApplicationApplicants { get; set; }
        public DbSet<ResidenceHistory> ResidenceHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure UnitType
            builder.Entity<UnitType>()
                .HasKey(u => u.Id);

            builder.Entity<UnitType>()
                .Property(u => u.UnitTypeName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<UnitType>()
                .Property(u => u.ActiveBool)
                .HasDefaultValue(true);

            // Configure Unit
            builder.Entity<Unit>()
                .Property(u => u.MonthlyRent)
                .HasPrecision(18, 2);

            // Configure relationships
            builder.Entity<Unit>()
                .HasOne(u => u.Property)
                .WithMany(p => p.Units)
                .HasForeignKey(u => u.PropertyID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Unit>()
                .HasOne(u => u.UnitType)
                .WithMany()
                .HasForeignKey(u => u.UnitTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ApplicationStatus
            builder.Entity<ApplicationStatus>()
                .Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(50);

            // Configure Application
            builder.Entity<Application>()
                .Property(a => a.Date)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<Application>()
                .HasOne(a => a.PropertyUnit)
                .WithMany()
                .HasForeignKey(a => a.UnitID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Application>()
                .HasOne(a => a.Status)
                .WithMany()
                .HasForeignKey(a => a.ApplicationStatusID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Application>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.ApplicantUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Applicant
            builder.Entity<Applicant>(entity =>
            {
                entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Phone).IsRequired().HasMaxLength(20);
                entity.Property(a => a.Email).IsRequired().HasMaxLength(100);
                entity.Property(a => a.CurrentAddress).IsRequired().HasMaxLength(200);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(a => a.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ApplicationApplicant (many-to-many cross-reference)
            builder.Entity<ApplicationApplicant>(entity =>
            {
                entity.HasOne(x => x.Application)
                    .WithMany(a => a.ApplicationApplicants)
                    .HasForeignKey(x => x.ApplicationID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Applicant)
                    .WithMany(a => a.ApplicationApplicants)
                    .HasForeignKey(x => x.ApplicantID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.ApplicationID, x.ApplicantID }).IsUnique();
            });

            // Configure ResidenceHistory
            builder.Entity<ResidenceHistory>(entity =>
            {
                entity.Property(r => r.Street).IsRequired().HasMaxLength(200);
                entity.Property(r => r.City).IsRequired().HasMaxLength(100);
                entity.Property(r => r.State).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Zip).IsRequired().HasMaxLength(20);
                entity.Property(r => r.LandlordName).IsRequired().HasMaxLength(100);
                entity.Property(r => r.LandlordPhone).IsRequired().HasMaxLength(20);

                entity.HasOne(r => r.ApplicationApplicant)
                    .WithMany(x => x.ResidenceHistories)
                    .HasForeignKey(r => r.ApplicationApplicantID)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
