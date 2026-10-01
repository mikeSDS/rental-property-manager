using Microsoft.AspNetCore.Identity;
using RentalPropertyManager.Models;

namespace RentalPropertyManager.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Create roles if they don't exist
                var applicantRoleExists = await roleManager.RoleExistsAsync("Applicant");
                if (!applicantRoleExists)
                {
                    await roleManager.CreateAsync(new IdentityRole("Applicant"));
                }

                var propertyManagerRoleExists = await roleManager.RoleExistsAsync("PropertyManager");
                if (!propertyManagerRoleExists)
                {
                    await roleManager.CreateAsync(new IdentityRole("PropertyManager"));
                }

                // Seed UnitTypes if table is empty
                if (!dbContext.UnitTypes.Any())
                {
                    var unitTypes = new List<UnitType>
                    {
                        new UnitType { UnitTypeName = "Active", ActiveBool = true },
                        new UnitType { UnitTypeName = "Inactive", ActiveBool = false }
                    };

                    dbContext.UnitTypes.AddRange(unitTypes);
                    await dbContext.SaveChangesAsync();
                }

                // Optional: Seed a test admin user for local development
                var adminEmail = "admin@rentalmanager.local";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    var newAdmin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        UserType = "PropertyManager",
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(newAdmin, "AdminPassword123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "PropertyManager");
                    }
                }
            }
        }
    }
}
