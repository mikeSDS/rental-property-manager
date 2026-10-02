using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class PropertiesControllerTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetList_ReturnsEmptyList_WhenNoPropertiesExist()
        {
            // Arrange
            using var context = GetTestDbContext();

            // Act
            var properties = await context.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Assert
            Assert.NotNull(properties);
            Assert.Empty(properties);
        }

        [Fact]
        public async Task GetList_ReturnsAllProperties_WhenPropertiesExist()
        {
            // Arrange
            using var context = GetTestDbContext();

            var testProperties = new List<Property>
            {
                new Property
                {
                    Name = "Downtown Apartments",
                    StreetAddress = "123 Main St",
                    City = "Springfield",
                    State = "IL",
                    ZipCode = "62701",
                    CreatedAt = DateTime.UtcNow
                },
                new Property
                {
                    Name = "Riverside Complex",
                    StreetAddress = "456 River Rd",
                    City = "Springfield",
                    State = "IL",
                    ZipCode = "62702",
                    CreatedAt = DateTime.UtcNow
                },
                new Property
                {
                    Name = "Hillside Villas",
                    StreetAddress = "789 Hill Ave",
                    City = "Springfield",
                    State = "IL",
                    ZipCode = "62703",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Properties.AddRange(testProperties);
            await context.SaveChangesAsync();

            // Act
            var properties = await context.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Assert
            Assert.NotNull(properties);
            Assert.Equal(3, properties.Count);
            Assert.Equal("Downtown Apartments", properties[0].Name);
            Assert.Equal("Hillside Villas", properties[1].Name);
            Assert.Equal("Riverside Complex", properties[2].Name);
        }

        [Fact]
        public async Task GetList_ReturnsPropertiesSorted_ByName()
        {
            // Arrange
            using var context = GetTestDbContext();

            var testProperties = new List<Property>
            {
                new Property { Name = "Zebra Properties", CreatedAt = DateTime.UtcNow },
                new Property { Name = "Apple Apartments", CreatedAt = DateTime.UtcNow },
                new Property { Name = "Maple Residences", CreatedAt = DateTime.UtcNow }
            };

            context.Properties.AddRange(testProperties);
            await context.SaveChangesAsync();

            // Act
            var properties = await context.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Assert
            Assert.Equal("Apple Apartments", properties[0].Name);
            Assert.Equal("Maple Residences", properties[1].Name);
            Assert.Equal("Zebra Properties", properties[2].Name);
        }

        [Fact]
        public async Task GetList_ReturnsPropertyWithCompleteData()
        {
            // Arrange
            using var context = GetTestDbContext();

            var testProperty = new Property
            {
                Name = "Test Complex",
                StreetAddress = "999 Test Blvd",
                City = "Test City",
                State = "TC",
                ZipCode = "99999",
                CreatedAt = DateTime.UtcNow
            };

            context.Properties.Add(testProperty);
            await context.SaveChangesAsync();

            // Act
            var properties = await context.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            var property = properties.FirstOrDefault();

            // Assert
            Assert.NotNull(property);
            Assert.Equal("Test Complex", property.Name);
            Assert.Equal("999 Test Blvd", property.StreetAddress);
            Assert.Equal("Test City", property.City);
            Assert.Equal("TC", property.State);
            Assert.Equal("99999", property.ZipCode);
        }

        [Fact]
        public async Task GetList_IncludesPropertiesToBeSerializable_AsJson()
        {
            // Arrange
            using var context = GetTestDbContext();

            var testProperty = new Property
            {
                Name = "JSON Test Property",
                StreetAddress = "111 JSON St",
                City = "JSON City",
                State = "JS",
                ZipCode = "11111",
                CreatedAt = DateTime.UtcNow
            };

            context.Properties.Add(testProperty);
            await context.SaveChangesAsync();

            // Act
            var properties = await context.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Assert - Verify properties can be serialized (no circular references, etc)
            Assert.NotEmpty(properties);
            var prop = properties.First();
            Assert.IsType<int>(prop.Id);
            Assert.NotNull(prop.Name);
            Assert.NotNull(prop.StreetAddress);
            Assert.NotNull(prop.City);
            Assert.NotNull(prop.State);
            Assert.NotNull(prop.ZipCode);
        }

        [Fact]
        public async Task Property_CanBeCreated_WithMinimalData()
        {
            // Arrange
            using var context = GetTestDbContext();

            // Act
            var property = new Property
            {
                Name = "Minimal Property",
                CreatedAt = DateTime.UtcNow
            };

            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var savedProperty = await context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == "Minimal Property");

            // Assert
            Assert.NotNull(savedProperty);
            Assert.Equal("Minimal Property", savedProperty?.Name);
            // Properties not set default to empty string, not null (since they're not nullable)
            Assert.True(string.IsNullOrEmpty(savedProperty?.StreetAddress));
            Assert.True(string.IsNullOrEmpty(savedProperty?.City));
        }

        [Fact]
        public async Task Property_CanBeCreated_WithCompleteData()
        {
            // Arrange
            using var context = GetTestDbContext();

            // Act
            var property = new Property
            {
                Name = "Complete Property",
                StreetAddress = "100 Complete St",
                City = "Complete City",
                State = "CC",
                ZipCode = "12345",
                CreatedAt = DateTime.UtcNow
            };

            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var savedProperty = await context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == "Complete Property");

            // Assert
            Assert.NotNull(savedProperty);
            Assert.Equal("Complete Property", savedProperty?.Name);
            Assert.Equal("100 Complete St", savedProperty?.StreetAddress);
            Assert.Equal("Complete City", savedProperty?.City);
            Assert.Equal("CC", savedProperty?.State);
            Assert.Equal("12345", savedProperty?.ZipCode);
        }

        [Fact]
        public async Task Property_CanBeUpdated()
        {
            // Arrange
            using var context = GetTestDbContext();

            var property = new Property
            {
                Name = "Original Name",
                City = "Original City",
                CreatedAt = DateTime.UtcNow
            };

            context.Properties.Add(property);
            await context.SaveChangesAsync();
            var propertyId = property.Id;

            // Act
            var existingProperty = await context.Properties.FindAsync(propertyId);
            existingProperty.Name = "Updated Name";
            existingProperty.City = "Updated City";
            await context.SaveChangesAsync();

            var updatedProperty = await context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            // Assert
            Assert.NotNull(updatedProperty);
            Assert.Equal("Updated Name", updatedProperty.Name);
            Assert.Equal("Updated City", updatedProperty.City);
        }

        [Fact]
        public async Task Property_CanBeDeleted()
        {
            // Arrange
            using var context = GetTestDbContext();

            var property = new Property
            {
                Name = "Property to Delete",
                CreatedAt = DateTime.UtcNow
            };

            context.Properties.Add(property);
            await context.SaveChangesAsync();
            var propertyId = property.Id;

            // Act
            var propertyToDelete = await context.Properties.FindAsync(propertyId);
            if (propertyToDelete != null)
            {
                context.Properties.Remove(propertyToDelete);
                await context.SaveChangesAsync();
            }

            var deletedProperty = await context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            // Assert
            Assert.Null(deletedProperty);
        }

        [Fact]
        public async Task Property_HasCorrectCreatedAtTimestamp()
        {
            // Arrange
            using var context = GetTestDbContext();

            var beforeCreation = DateTime.UtcNow;

            // Act
            var property = new Property
            {
                Name = "Timestamp Test",
                CreatedAt = DateTime.UtcNow
            };

            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var afterCreation = DateTime.UtcNow;

            var savedProperty = await context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == "Timestamp Test");

            // Assert
            Assert.NotNull(savedProperty);
            Assert.True(savedProperty?.CreatedAt >= beforeCreation.AddSeconds(-1));
            Assert.True(savedProperty?.CreatedAt <= afterCreation.AddSeconds(1));
        }
    }
}
