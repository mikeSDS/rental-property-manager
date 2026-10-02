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
        }
    }
}
