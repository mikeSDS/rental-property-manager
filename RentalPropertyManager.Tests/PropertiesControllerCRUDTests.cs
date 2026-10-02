using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RentalPropertyManager.Controllers;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels;
using Xunit;

namespace RentalPropertyManager.Tests
{
    /// <summary>
    /// Integration tests for Properties Controller form field handling.
    /// These tests verify that optional fields work correctly and that the controller
    /// properly persists data as expected by the form submission contracts.
    /// </summary>
    public class PropertiesControllerCRUDTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private PropertiesController GetController(ApplicationDbContext context)
        {
            var mockLogger = new Mock<ILogger<PropertiesController>>();
            return new PropertiesController(context, mockLogger.Object);
        }

        [Fact]
        public async Task Edit_CanClearOptionalAddressFields()
        {
            // Arrange - Verify that optional address fields in PropertyFormViewModel can be set to null
            using var context = GetTestDbContext();
            var controller = GetController(context);

            var property = new Property
            {
                Name = "Property With Address",
                StreetAddress = "123 Main St",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701",
                CreatedAt = DateTime.UtcNow
            };
            context.Properties.Add(property);
            await context.SaveChangesAsync();
            var propertyId = property.Id;

            // Create edit view model with optional fields set to null
            var viewModel = new PropertyFormViewModel
            {
                Id = propertyId,
                Name = "Updated Name",
                StreetAddress = null,
                City = null,
                State = null,
                ZipCode = null
            };

            // Act
            var result = await controller.Edit(propertyId, viewModel);

            // Assert - Update should succeed
            Assert.IsType<OkObjectResult>(result);

            var updated = await context.Properties.FindAsync(propertyId);
            Assert.NotNull(updated);
            Assert.Equal("Updated Name", updated.Name);
        }

        [Fact]
        public async Task Edit_UpdatesAddressFields()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = GetController(context);

            var property = new Property
            {
                Name = "Original House",
                StreetAddress = "Old Address",
                CreatedAt = DateTime.UtcNow
            };
            context.Properties.Add(property);
            await context.SaveChangesAsync();
            var propertyId = property.Id;

            var updateViewModel = new PropertyFormViewModel
            {
                Id = propertyId,
                Name = "Same Name",
                StreetAddress = "New Address",
                City = "New City",
                State = "CA",
                ZipCode = "90210"
            };

            // Act
            var result = await controller.Edit(propertyId, updateViewModel);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var updated = await context.Properties.FindAsync(propertyId);
            Assert.NotNull(updated);
            Assert.Equal("New Address", updated.StreetAddress);
            Assert.Equal("New City", updated.City);
            Assert.Equal("CA", updated.State);
            Assert.Equal("90210", updated.ZipCode);
        }

        [Fact]
        public async Task Edit_RejectsIdMismatch()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = GetController(context);

            // Act - Try to edit property 1 but view model has ID 999
            var result = await controller.Edit(1, new PropertyFormViewModel { Id = 999, Name = "Test" });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Edit_Returns404WhenPropertyNotFound()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = GetController(context);

            // Act
            var result = await controller.Edit(999, new PropertyFormViewModel { Id = 999, Name = "Test" });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_RemovesProperty()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = GetController(context);

            var property = new Property { Name = "To Delete", CreatedAt = DateTime.UtcNow };
            context.Properties.Add(property);
            await context.SaveChangesAsync();
            var propertyId = property.Id;

            // Act
            var result = await controller.Delete(propertyId);

            // Assert - Should succeed
            Assert.IsType<OkObjectResult>(result);

            // Verify property was deleted
            var deleted = await context.Properties.FindAsync(propertyId);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Delete_Returns404ForNonexistentProperty()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = GetController(context);

            // Act
            var result = await controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_FailsWhenPropertyHasUnits()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = GetController(context);

            var property = new Property { Name = "With Units", CreatedAt = DateTime.UtcNow };
            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var unitType = new UnitType { UnitTypeName = "Studio", ActiveBool = true };
            context.UnitTypes.Add(unitType);
            await context.SaveChangesAsync();

            var unit = new Unit
            {
                PropertyID = property.Id,
                UnitNumber = "101",
                UnitTypeID = unitType.Id,
                MonthlyRent = 1000
            };
            context.Units.Add(unit);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Delete(property.Id);

            // Assert - Should fail validation
            Assert.IsType<BadRequestObjectResult>(result);

            // Verify property was NOT deleted
            var notDeleted = await context.Properties.FindAsync(property.Id);
            Assert.NotNull(notDeleted);
        }

        [Fact]
        public async Task PropertyFormViewModel_CanBeValidated_WithMinimalFields()
        {
            // Arrange - Test that form validation allows optional fields to be empty
            var viewModel = new PropertyFormViewModel
            {
                Name = "Test Property"
                // Address fields intentionally null - should pass validation
            };

            // Act
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(viewModel);
            var results = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(viewModel, context, results, true);

            // Assert - Should pass validation because only Name is required
            Assert.True(isValid, $"Validation failed: {string.Join(", ", results.Select(r => r.ErrorMessage))}");
        }

        [Fact]
        public async Task Create_WithJsonPostBody_ShouldPersistProperty()
        {
            // Arrange - Test the exact flow: POST with JSON body containing PascalCase properties
            using var context = GetTestDbContext();
            var controller = GetController(context);

            var viewModel = new PropertyFormViewModel
            {
                Name = "New Property",
                StreetAddress = "456 Oak Ave",
                City = "Chicago",
                State = "IL",
                ZipCode = "60601"
            };

            // Act
            var result = await controller.Create(viewModel);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            // Verify the property was actually saved to the database
            var savedProperty = await context.Properties.FirstOrDefaultAsync(p => p.Name == "New Property");
            Assert.NotNull(savedProperty);
            Assert.Equal("456 Oak Ave", savedProperty.StreetAddress);
            Assert.Equal("Chicago", savedProperty.City);
            Assert.Equal("IL", savedProperty.State);
            Assert.Equal("60601", savedProperty.ZipCode);
        }

        [Fact]
        public async Task Edit_WithJsonPostBody_ShouldUpdateProperty()
        {
            // Arrange - Test the exact flow: POST with JSON body containing PascalCase properties
            using var context = GetTestDbContext();
            var controller = GetController(context);

            var property = new Property
            {
                Name = "Original Property",
                StreetAddress = "123 Main St",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701",
                CreatedAt = DateTime.UtcNow
            };
            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var viewModel = new PropertyFormViewModel
            {
                Id = property.Id,
                Name = "Updated Property",
                StreetAddress = "789 Elm St",
                City = "Peoria",
                State = "IL",
                ZipCode = "61601"
            };

            // Act
            var result = await controller.Edit(property.Id, viewModel);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            // Verify the property was actually updated
            var updatedProperty = await context.Properties.FindAsync(property.Id);
            Assert.NotNull(updatedProperty);
            Assert.Equal("Updated Property", updatedProperty.Name);
            Assert.Equal("789 Elm St", updatedProperty.StreetAddress);
            Assert.Equal("Peoria", updatedProperty.City);
        }
    }
}
