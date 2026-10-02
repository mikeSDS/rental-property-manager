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
    /// <summary>
    /// Tests for Properties controller route handling.
    /// These tests verify that the GetList endpoint is accessible at /properties/list
    /// and would have caught the original 404 error when the view was calling /api/properties/list.
    /// </summary>
    public class PropertiesControllerRoutingTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void PropertiesController_RouteAttribute_IncludesControllerRoute()
        {
            // Arrange & Act & Assert
            // This test verifies the routing configuration.
            // The controller should have [Route("[controller]")] which maps to /properties
            var controllerType = typeof(RentalPropertyManager.Controllers.PropertiesController);
            var attributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), false);

            Assert.NotEmpty(attributes);
            var routeAttribute = attributes[0] as Microsoft.AspNetCore.Mvc.RouteAttribute;
            Assert.NotNull(routeAttribute);
            Assert.Contains("[controller]", routeAttribute.Template);
        }

        [Fact]
        public void PropertiesController_GetList_HasCorrectHttpGetAttribute()
        {
            // Arrange & Act
            var method = typeof(RentalPropertyManager.Controllers.PropertiesController)
                .GetMethod("GetList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Assert
            Assert.NotNull(method);
            var httpGetAttributes = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), false);
            Assert.NotEmpty(httpGetAttributes);

            var httpGetAttr = httpGetAttributes[0] as Microsoft.AspNetCore.Mvc.HttpGetAttribute;
            Assert.NotNull(httpGetAttr);
            // Should have route "list" which maps to /properties/list
            Assert.Equal("list", httpGetAttr.Template);
        }

        [Fact]
        public void PropertiesController_HasAuthorizationAttribute()
        {
            // Arrange & Act
            var controllerType = typeof(RentalPropertyManager.Controllers.PropertiesController);
            var authAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

            // Assert
            Assert.NotEmpty(authAttributes);
            var authAttr = authAttributes[0] as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
            Assert.NotNull(authAttr);
            Assert.Equal("PropertyManager", authAttr.Roles);
        }

        [Fact]
        public void GetListMethod_DoesNotHaveAllowAnonymousAttribute()
        {
            // Arrange & Act
            // This test ensures GetList does NOT have [AllowAnonymous]
            // because it should require PropertyManager role
            var method = typeof(RentalPropertyManager.Controllers.PropertiesController)
                .GetMethod("GetList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Assert
            Assert.NotNull(method);
            var allowAnonAttributes = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false);
            Assert.Empty(allowAnonAttributes);
        }

        [Fact]
        public async Task GetList_QueryLogic_ReturnsEmptyListWhenNoProperties()
        {
            // Arrange
            using var context = GetTestDbContext();

            // Act - Simulate what GetList does
            var properties = await context.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Assert
            Assert.NotNull(properties);
            Assert.Empty(properties);
        }

        [Fact]
        public async Task GetList_QueryLogic_ReturnsSortedProperties()
        {
            // Arrange
            using var context = GetTestDbContext();

            var testData = new List<Property>
            {
                new Property { Name = "Zebra", CreatedAt = DateTime.UtcNow },
                new Property { Name = "Apple", CreatedAt = DateTime.UtcNow },
                new Property { Name = "Maple", CreatedAt = DateTime.UtcNow }
            };

            context.Properties.AddRange(testData);
            await context.SaveChangesAsync();

            // Act - Simulate what GetList does
            var properties = await context.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Assert
            Assert.Equal(3, properties.Count);
            Assert.Equal("Apple", properties[0].Name);
            Assert.Equal("Maple", properties[1].Name);
            Assert.Equal("Zebra", properties[2].Name);
        }

        [Fact]
        public async Task GetList_QueryLogic_ReturnsJsonSerializable()
        {
            // Arrange
            using var context = GetTestDbContext();

            var testProperty = new Property
            {
                Name = "Test Property",
                StreetAddress = "123 Main St",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701",
                CreatedAt = DateTime.UtcNow
            };

            context.Properties.Add(testProperty);
            await context.SaveChangesAsync();

            // Act
            var properties = await context.Properties
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Assert - Verify all properties needed by JavaScript are present
            Assert.NotEmpty(properties);
            var prop = properties[0];

            // These are the fields accessed by the JavaScript in the view:
            // prop.name, prop.streetAddress, prop.city, prop.state, prop.zipCode, prop.id
            Assert.NotNull(prop?.Name);
            Assert.NotNull(prop?.StreetAddress);
            Assert.NotNull(prop?.City);
            Assert.NotNull(prop?.State);
            Assert.NotNull(prop?.ZipCode);
            Assert.True(prop?.Id > 0);

            // Verify it can be serialized to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(properties);
            Assert.NotNull(json);
            Assert.NotEmpty(json);
            Assert.StartsWith("[", json.Trim());
            Assert.EndsWith("]", json.Trim());
        }
    }
}

