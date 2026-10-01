using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Models;

namespace RentalPropertyManager.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<UnitType> UnitTypes { get; set; }

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
        }
    }
}
