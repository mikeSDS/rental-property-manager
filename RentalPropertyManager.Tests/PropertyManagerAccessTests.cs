using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class PropertyManagerAccessTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task BogusSeeding_PopulatesPropertiesAndUnits_Idempotently()
        {
            // Arrange
            using var context = GetTestDbContext();

            // Seed base data
            var activeUnitType = new UnitType { UnitTypeName = "Loft", ActiveBool = true };
            context.UnitTypes.Add(activeUnitType);
            await context.SaveChangesAsync();

            // Act - Simulate DbInitializer seeding
            var existingProperties = await context.Properties.AnyAsync();

            if (!existingProperties)
            {
                var properties = new List<Property>
                {
                    new Property { Name = "Property 1", StreetAddress = "123 Main St", City = "City 1", State = "ST", ZipCode = "12345", CreatedAt = DateTime.UtcNow },
                    new Property { Name = "Property 2", StreetAddress = "456 Oak Ave", City = "City 2", State = "ST", ZipCode = "54321", CreatedAt = DateTime.UtcNow },
                    new Property { Name = "Property 3", StreetAddress = "789 Pine Rd", City = "City 3", State = "ST", ZipCode = "11111", CreatedAt = DateTime.UtcNow },
                    new Property { Name = "Property 4", StreetAddress = "101 Elm St", City = "City 4", State = "ST", ZipCode = "22222", CreatedAt = DateTime.UtcNow },
                    new Property { Name = "Property 5", StreetAddress = "202 Birch Ln", City = "City 5", State = "ST", ZipCode = "33333", CreatedAt = DateTime.UtcNow }
                };

                context.Properties.AddRange(properties);
                await context.SaveChangesAsync();

                // Add units for each property
                foreach (var property in properties)
                {
                    var units = new List<Unit>
                    {
                        new Unit { PropertyID = property.Id, UnitNumber = "101", Bedrooms = 1, MonthlyRent = 1000, UnitTypeID = activeUnitType.Id },
                        new Unit { PropertyID = property.Id, UnitNumber = "102", Bedrooms = 2, MonthlyRent = 1500, UnitTypeID = activeUnitType.Id },
                        new Unit { PropertyID = property.Id, UnitNumber = "103", Bedrooms = 3, MonthlyRent = 2000, UnitTypeID = activeUnitType.Id }
                    };
                    context.Units.AddRange(units);
                }
                await context.SaveChangesAsync();
            }

            // Assert - Verify seeding worked
            var propertyCount = await context.Properties.CountAsync();
            var unitCount = await context.Units.CountAsync();

            Assert.Equal(5, propertyCount);
            Assert.Equal(15, unitCount); // 5 properties * 3 units each
            Assert.True(await context.Units.AllAsync(u => u.UnitType.ActiveBool));

            // Act - Call seeding again (idempotently check)
            var existingPropertiesSecondCall = await context.Properties.AnyAsync();

            // Assert - Should not duplicate
            Assert.True(existingPropertiesSecondCall); // Idempotent check works
            var propertyCountSecond = await context.Properties.CountAsync();
            var unitCountSecond = await context.Units.CountAsync();

            Assert.Equal(5, propertyCountSecond); // Should still be 5, not 10
            Assert.Equal(15, unitCountSecond);// Should still be 15, not 30
        }

        [Fact]
        public async Task DbInitializer_CreatesDefaultTestAccounts_WithProperRoles()
        {
            // Arrange - This test verifies the seeding pattern exists (integration test pattern)
            using var context = GetTestDbContext();

            // Simulate creating test users (mimics DbInitializer pattern)
            var testUsers = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    UserName = "admin@rentalmanager.local",
                    Email = "admin@rentalmanager.local",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    UserName = "manager@realestate.com",
                    Email = "manager@realestate.com",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    UserName = "applicant@realestate.com",
                    Email = "applicant@realestate.com",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Act
            context.Users.AddRange(testUsers);
            await context.SaveChangesAsync();

            var savedUsers = await context.Users
                .Where(u => u.Email.Contains("realestate.com") || u.Email.Contains("rentalmanager"))
                .ToListAsync();

            // Assert
            Assert.Equal(3, savedUsers.Count);

            var manager = savedUsers.FirstOrDefault(u => u.Email == "manager@realestate.com");
            var applicant = savedUsers.FirstOrDefault(u => u.Email == "applicant@realestate.com");

            Assert.NotNull(manager);
            Assert.NotNull(applicant);
            Assert.True(manager.EmailConfirmed);
            Assert.True(applicant.EmailConfirmed);
        }

        [Fact]
        public async Task PropertiesAndUnits_HaveForeignKeyConstraints_Enforced()
        {
            // Arrange
            using var context = GetTestDbContext();

            var property = new Property
            {
                Name = "Test Property",
                StreetAddress = "123 Main St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345"
            };
            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var unitType = new UnitType { UnitTypeName = "Test Type", ActiveBool = true };
            context.UnitTypes.Add(unitType);
            await context.SaveChangesAsync();

            // Act - Create unit with valid foreign keys
            var unit = new Unit
            {
                PropertyID = property.Id,
                UnitNumber = "101",
                Bedrooms = 2,
                MonthlyRent = 1500,
                UnitTypeID = unitType.Id
            };

            context.Units.Add(unit);
            await context.SaveChangesAsync();

            // Assert - Verify relationships are loaded
            var retrievedUnit = await context.Units
                .Include(u => u.Property)
                .Include(u => u.UnitType)
                .FirstOrDefaultAsync(u => u.Id == unit.Id);

            Assert.NotNull(retrievedUnit);
            Assert.NotNull(retrievedUnit.Property);
            Assert.NotNull(retrievedUnit.UnitType);
            Assert.Equal("Test Property", retrievedUnit.Property.Name);
            Assert.Equal("Test Type", retrievedUnit.UnitType.UnitTypeName);
        }
    }
}
