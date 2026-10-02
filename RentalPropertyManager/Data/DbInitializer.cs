using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
                        // Active unit types - available for selection
                        new UnitType { UnitTypeName = "Studio / Efficiency", ActiveBool = true },
                        new UnitType { UnitTypeName = "Traditional Multi-Bedroom", ActiveBool = true },
                        new UnitType { UnitTypeName = "Loft", ActiveBool = true },
                        new UnitType { UnitTypeName = "Townhome", ActiveBool = true },
                        new UnitType { UnitTypeName = "Multiplex Unit (Duplex / Triplex / Fourplex)", ActiveBool = true },
                        new UnitType { UnitTypeName = "Mixed-Use Residential", ActiveBool = true },
                        // Inactive unit types - retained for historical data integrity, not selectable
                        new UnitType { UnitTypeName = "Penthouse", ActiveBool = false },
                        new UnitType { UnitTypeName = "Specialized Housing Unit", ActiveBool = false }
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
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(newAdmin, "AdminPassword123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "PropertyManager");
                    }
                }

                // Seed default test accounts: manager@realestate.com and applicant@realestate.com
                var managerEmail = "manager@realestate.com";
                var managerUser = await userManager.FindByEmailAsync(managerEmail);
                if (managerUser == null)
                {
                    var newManager = new ApplicationUser
                    {
                        UserName = managerEmail,
                        Email = managerEmail,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(newManager, "ManagerPassword123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newManager, "PropertyManager");
                    }
                }

                var applicantEmail = "applicant@realestate.com";
                var applicantUser = await userManager.FindByEmailAsync(applicantEmail);
                if (applicantUser == null)
                {
                    var newApplicant = new ApplicationUser
                    {
                        UserName = applicantEmail,
                        Email = applicantEmail,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(newApplicant, "ApplicantPassword123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newApplicant, "Applicant");
                    }
                }

                // Seed Properties and Units using Bogus if Properties table is empty
                if (!await dbContext.Properties.AnyAsync())
                {
                    var propertyFaker = new Faker<Property>()
                        .RuleFor(p => p.Name, f => f.Company.CompanyName() + " Apartments")
                        .RuleFor(p => p.StreetAddress, f => f.Address.StreetAddress())
                        .RuleFor(p => p.City, f => f.Address.City())
                        .RuleFor(p => p.State, f => f.Address.StateAbbr())
                        .RuleFor(p => p.ZipCode, f => f.Address.ZipCode())
                        .RuleFor(p => p.CreatedAt, f => DateTime.UtcNow.AddMonths(-f.Random.Number(1, 12)));

                    var properties = propertyFaker.Generate(5);
                    dbContext.Properties.AddRange(properties);
                    await dbContext.SaveChangesAsync();

                         // Get active and inactive UnitTypes
                         var activeUnitTypeList = dbContext.UnitTypes.Where(ut => ut.ActiveBool).ToList();
                         var inactiveUnitTypeList = dbContext.UnitTypes.Where(ut => !ut.ActiveBool).ToList();

                         if (!activeUnitTypeList.Any())
                         {
                             throw new InvalidOperationException("No active UnitTypes found. Ensure UnitTypes are seeded before Properties.");
                         }

                         // Generate units for each property
                         var random = new Random();
                         var unitsToCreate = new List<Unit>();

                         foreach (var property in properties)
                         {
                             var numUnits = random.Next(3, 7); // Generate 3-6 units per property

                             for (int i = 0; i < numUnits; i++)
                             {
                                 UnitType unitType;

                                 // First units of each property: use INACTIVE types if available
                                 // This ensures we always have some inactive units for testing the dropdown
                                 if (i == 0 && inactiveUnitTypeList.Any())
                                 {
                                     // Use first inactive type (Penthouse)
                                     unitType = inactiveUnitTypeList[0];
                                 }
                                 else if (i == 1 && inactiveUnitTypeList.Count > 1)
                                 {
                                     // Use second inactive type (Specialized Housing Unit)
                                     unitType = inactiveUnitTypeList[1];
                                 }
                                 else
                                 {
                                     // Use active types for remaining units
                                     unitType = activeUnitTypeList[random.Next(activeUnitTypeList.Count)];
                                 }

                                 var unit = new Unit
                                 {
                                     PropertyID = property.Id,
                                     UnitNumber = (i + 1).ToString(),
                                     Bedrooms = random.Next(1, 4),
                                     MonthlyRent = (decimal)random.Next(9, 32) * 100,
                                     UnitTypeID = unitType.Id
                                 };

                                 unitsToCreate.Add(unit);
                             }
                         }

                         dbContext.Units.AddRange(unitsToCreate);
                         await dbContext.SaveChangesAsync();
                     }
            }
        }
    }
}
