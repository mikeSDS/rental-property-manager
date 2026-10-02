using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class UnitModalValidationTests
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
        public void UnitFormViewModel_WithValidData_PassesValidation()
        {
            // Arrange
            var viewModel = new UnitFormViewModel
            {
                Id = null,
                PropertyID = 1,
                UnitNumber = "101",
                Bedrooms = 2,
                MonthlyRent = 1500.00m,
                UnitTypeID = 1,
                AvailableUnitTypes = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
            };

            // Act
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public void PropertyFormViewModel_WithInvalidData_FailsValidation()
        {
            // Arrange - Name is the only required field
            var viewModel = new PropertyFormViewModel
            {
                Name = "", // Invalid: required
                StreetAddress = "123 Main St", // Optional
                City = "Test City", // Optional
                State = "TS", // Optional
                ZipCode = "12345" // Optional
            };

            // Act
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void PropertyFormViewModel_WithValidData_PassesValidation()
        {
            // Arrange - Name is required; address fields are optional
            var viewModel = new PropertyFormViewModel
            {
                Id = null,
                Name = "Downtown Apartments",
                StreetAddress = null, // Optional
                City = null, // Optional
                State = null, // Optional
                ZipCode = null // Optional
            };

            // Act
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(0, false)] // Bedrooms: 0 - invalid
        [InlineData(1, true)] // Bedrooms: 1 - valid (minimum)
        [InlineData(5, true)] // Bedrooms: 5 - valid
        [InlineData(10, true)] // Bedrooms: 10 - valid (maximum)
        [InlineData(11, false)] // Bedrooms: 11 - invalid (exceeds maximum)
        public void UnitForm_BedroomValidation_EnforcesRange(int bedrooms, bool shouldBeValid)
        {
            // Arrange
            var viewModel = new UnitFormViewModel
            {
                PropertyID = 1,
                UnitNumber = "101",
                Bedrooms = bedrooms,
                MonthlyRent = 1500.00m,
                UnitTypeID = 1
            };

            // Act
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            if (shouldBeValid)
            {
                Assert.True(isValid,  $"Bedrooms value {bedrooms} should be valid");
            }
            else
            {
                Assert.False(isValid, $"Bedrooms value {bedrooms} should be invalid");
            }
        }

        [Theory]
        [InlineData(0.00, false)] // MonthlyRent: 0.00 - invalid
        [InlineData(0.01, true)] // MonthlyRent: 0.01 - valid (minimum)
        [InlineData(1500.00, true)] // MonthlyRent: 1500.00 - valid
        [InlineData(100000.00, true)] // MonthlyRent: 100000.00 - valid (maximum)
        [InlineData(100000.01, false)] // MonthlyRent: 100000.01 - invalid (exceeds maximum)
        public void UnitForm_MonthlyRentValidation_EnforcesRange(decimal monthlyRent, bool shouldBeValid)
        {
            // Arrange
            var viewModel = new UnitFormViewModel
            {
                PropertyID = 1,
                UnitNumber = "101",
                Bedrooms = 2,
                MonthlyRent = monthlyRent,
                UnitTypeID = 1
            };

            // Act
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            if (shouldBeValid)
            {
                Assert.True(isValid, $"MonthlyRent value {monthlyRent} should be valid");
            }
            else
            {
                Assert.False(isValid, $"MonthlyRent value {monthlyRent} should be invalid");
            }
        }

        [Fact]
        public async Task UnitFormViewModel_AvailableUnitTypes_PopulatesCorrectly()
        {
            // Arrange
            using var context = GetTestDbContext();

            var activeType1 = new UnitType { UnitTypeName = "Studio", ActiveBool = true };
            var activeType2 = new UnitType { UnitTypeName = "1-Bedroom", ActiveBool = true };
            var inactiveType = new UnitType { UnitTypeName = "Inactive-Type", ActiveBool = false };

            context.UnitTypes.AddRange(activeType1, activeType2, inactiveType);
            await context.SaveChangesAsync();

            // Act - Simulate dropdown population for new unit (active only)
            var activeTypes = await context.UnitTypes
                .Where(ut => ut.ActiveBool)
                .OrderBy(ut => ut.UnitTypeName)
                .ToListAsync();

            var selectItems = activeTypes
                .Select(ut => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = ut.Id.ToString(),
                    Text = ut.UnitTypeName
                })
                .ToList();

            // Assert
            Assert.Equal(2, selectItems.Count);
            Assert.DoesNotContain(selectItems, si => si.Text == "Inactive-Type");
            Assert.Contains(selectItems, si => si.Text == "Studio");
            Assert.Contains(selectItems, si => si.Text == "1-Bedroom");
        }
    }
}
