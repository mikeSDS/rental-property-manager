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

                // Seed ApplicationStatuses, Applications, Applicants and ResidenceHistories
                await SeedApplicationDataAsync(dbContext, userManager);
            }
        }

        private static async Task SeedApplicationDataAsync(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            var existingStatusNames = await dbContext.ApplicationStatuses.Select(s => s.Name).ToListAsync();
            var missingStatuses = ApplicationStatus.AllNames.Except(existingStatusNames).ToList();
            if (missingStatuses.Count > 0)
            {
                dbContext.ApplicationStatuses.AddRange(missingStatuses.Select(name => new ApplicationStatus { Name = name }));
                await dbContext.SaveChangesAsync();
            }

            // Seed one active lease so the submission collision lock can be exercised
            if (!await dbContext.Leases.AnyAsync())
            {
                var leaseUnit = await dbContext.Units.OrderByDescending(u => u.Id).FirstOrDefaultAsync();
                if (leaseUnit != null)
                {
                    var today = DateTime.UtcNow.Date;
                    dbContext.Leases.Add(new Lease
                    {
                        UnitID = leaseUnit.Id,
                        StartDate = today.AddMonths(-2),
                        EndDate = today.AddMonths(10),
                        MonthlyRent = leaseUnit.MonthlyRent
                    });
                    await dbContext.SaveChangesAsync();
                }
            }

            // Seed any missing ActionType lookups (idempotent per name)
            var existingActionNames = await dbContext.ActionTypes.Select(a => a.Name).ToListAsync();
            var missingActions = ActionType.AllNames.Except(existingActionNames).ToList();
            if (missingActions.Count > 0)
            {
                dbContext.ActionTypes.AddRange(missingActions.Select(n => new ActionType { Name = n }));
                await dbContext.SaveChangesAsync();
            }

            // Seed one application in every status if Applications table is empty
            if (await dbContext.Applications.AnyAsync())
            {
                await SeedReviewAndHistoryAsync(dbContext, userManager);
                return;
            }

            var applicantUser = await userManager.FindByEmailAsync("applicant@realestate.com");
            var units = await dbContext.Units.OrderBy(u => u.Id).ToListAsync();
            if (applicantUser == null || units.Count == 0)
            {
                return;
            }

            var statuses = await dbContext.ApplicationStatuses.ToDictionaryAsync(s => s.Name);

            var applicantFaker = new Faker<Applicant>()
                .RuleFor(a => a.Name, f => f.Name.FullName())
                .RuleFor(a => a.Phone, f => f.Phone.PhoneNumber("###-###-####"))
                .RuleFor(a => a.Email, f => f.Internet.Email())
                .RuleFor(a => a.CurrentAddress, f => $"{f.Address.StreetAddress()}, {f.Address.City()}, {f.Address.StateAbbr()} {f.Address.ZipCode("#####")}");

            var residenceFaker = new Faker<ResidenceHistory>()
                .RuleFor(r => r.Street, f => f.Address.StreetAddress())
                .RuleFor(r => r.City, f => f.Address.City())
                .RuleFor(r => r.State, f => f.Address.StateAbbr())
                .RuleFor(r => r.Zip, f => f.Address.ZipCode("#####"))
                .RuleFor(r => r.LandlordName, f => f.Name.FullName())
                .RuleFor(r => r.LandlordPhone, f => f.Phone.PhoneNumber("###-###-####"));

            var random = new Random();
            var index = 0;

            foreach (var statusName in ApplicationStatus.AllNames)
            {
                var application = new Application
                {
                    UnitID = units[index % units.Count].Id,
                    ApplicantUserID = applicantUser.Id,
                    Date = DateTime.UtcNow.AddDays(-random.Next(1, 60)),
                    ApplicationStatusID = statuses[statusName].Id
                };
                index++;

                var applicant = applicantFaker.Generate();
                applicant.UserID = applicantUser.Id;
                var link = new ApplicationApplicant { Applicant = applicant, IsPrimary = true };
                application.ApplicationApplicants.Add(link);

                // Older, completed residence followed by the current residence (no move-out date)
                var previousMoveIn = DateTime.UtcNow.Date.AddYears(-random.Next(3, 6));
                var previous = residenceFaker.Generate();
                previous.MoveInDate = previousMoveIn;
                previous.MoveOutDate = previousMoveIn.AddMonths(random.Next(12, 24));
                link.ResidenceHistories.Add(previous);

                var current = residenceFaker.Generate();
                current.MoveInDate = previous.MoveOutDate.Value.AddDays(random.Next(0, 30));
                current.MoveOutDate = null;
                link.ResidenceHistories.Add(current);

                dbContext.Applications.Add(application);
            }

            await dbContext.SaveChangesAsync();
            await SeedReviewAndHistoryAsync(dbContext, userManager);
        }

        private static async Task SeedReviewAndHistoryAsync(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            if (await dbContext.ActionHistories.AnyAsync() || await dbContext.Reviews.AnyAsync())
            {
                return;
            }

            var manager = await userManager.FindByEmailAsync("manager@realestate.com");
            if (manager == null)
            {
                return;
            }

            var actionTypes = await dbContext.ActionTypes.ToDictionaryAsync(a => a.Name);
            var applications = await dbContext.Applications
                .Include(a => a.Status)
                .Include(a => a.PropertyUnit)
                .ToListAsync();
            var statuses = await dbContext.ApplicationStatuses.ToDictionaryAsync(s => s.Name);
            var faker = new Faker();

            foreach (var app in applications)
            {
                var name = app.Status.Name;
                var submittedAt = app.Date;

                if (name != ApplicationStatus.Draft)
                {
                    dbContext.ActionHistories.Add(new ActionHistory
                    {
                        ActionTypeID = actionTypes[ActionType.Submission].Id,
                        UserID = app.ApplicantUserID,
                        Date = submittedAt,
                        ApplicationID = app.Id,
                        FromStatus = ApplicationStatus.Draft,
                        ToStatus = ApplicationStatus.Submitted
                    });
                }

                if (name == ApplicationStatus.Withdrawn)
                {
                    dbContext.ActionHistories.Add(new ActionHistory
                    {
                        ActionTypeID = actionTypes[ActionType.Withdraw].Id,
                        UserID = app.ApplicantUserID,
                        Date = submittedAt.AddDays(1),
                        ApplicationID = app.Id,
                        FromStatus = ApplicationStatus.Submitted,
                        ToStatus = ApplicationStatus.Withdrawn
                    });
                }
                else if (name == ApplicationStatus.Approved || name == ApplicationStatus.Returned || name == ApplicationStatus.Denied)
                {
                    var actionName = name == ApplicationStatus.Approved ? ActionType.Approve
                        : name == ApplicationStatus.Returned ? ActionType.Return
                        : ActionType.Deny;
                    var reviewDate = submittedAt.AddDays(2);
                    string? comment = name == ApplicationStatus.Approved ? null : faker.Lorem.Sentence();

                    dbContext.Reviews.Add(new Review
                    {
                        ApplicationID = app.Id,
                        UserID = manager.Id,
                        ReviewDate = reviewDate,
                        OutcomeApplicationStatusID = statuses[name].Id,
                        Comment = comment
                    });
                    dbContext.ActionHistories.Add(new ActionHistory
                    {
                        ActionTypeID = actionTypes[actionName].Id,
                        UserID = manager.Id,
                        Date = reviewDate,
                        ApplicationID = app.Id,
                        FromStatus = ApplicationStatus.Submitted,
                        ToStatus = name
                    });

                    var unitHasLease = await dbContext.Leases.AnyAsync(l => l.UnitID == app.UnitID);
                    if (name == ApplicationStatus.Approved && !unitHasLease)
                    {
                        var start = reviewDate.Date;
                        dbContext.Leases.Add(new Lease
                        {
                            UnitID = app.UnitID,
                            ApplicationID = app.Id,
                            StartDate = start,
                            EndDate = start.AddMonths(12),
                            MonthlyRent = app.PropertyUnit.MonthlyRent,
                            CreatedAt = reviewDate
                        });
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
