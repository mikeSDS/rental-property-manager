using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class ApplicationDbContextTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void ApplicationDbContext_CanAddUnitType()
        {
            using var context = CreateDbContext();
            var unitType = new UnitType { UnitTypeName = "Studio", ActiveBool = true };

            context.UnitTypes.Add(unitType);
            context.SaveChanges();

            Assert.NotEmpty(context.UnitTypes);
            Assert.Single(context.UnitTypes);
        }

        [Fact]
        public void ApplicationDbContext_CanRetrieveUnitType()
        {
            using var context = CreateDbContext();
            var unitType = new UnitType { UnitTypeName = "1-Bedroom", ActiveBool = true };

            context.UnitTypes.Add(unitType);
            context.SaveChanges();

            var retrieved = context.UnitTypes.FirstOrDefault(u => u.UnitTypeName == "1-Bedroom");
            Assert.NotNull(retrieved);
            Assert.Equal("1-Bedroom", retrieved.UnitTypeName);
        }

        [Fact]
        public void ApplicationDbContext_CanUpdateUnitType()
        {
            using var context = CreateDbContext();
            var unitType = new UnitType { UnitTypeName = "Studio", ActiveBool = true };

            context.UnitTypes.Add(unitType);
            context.SaveChanges();

            unitType.UnitTypeName = "Updated Studio";
            context.UnitTypes.Update(unitType);
            context.SaveChanges();

            var updated = context.UnitTypes.First();
            Assert.Equal("Updated Studio", updated.UnitTypeName);
        }

        [Fact]
        public void ApplicationDbContext_CanDeleteUnitType()
        {
            using var context = CreateDbContext();
            var unitType = new UnitType { UnitTypeName = "Studio", ActiveBool = true };

            context.UnitTypes.Add(unitType);
            context.SaveChanges();

            context.UnitTypes.Remove(unitType);
            context.SaveChanges();

            Assert.Empty(context.UnitTypes);
        }

        [Fact]
        public void ApplicationDbContext_UnitTypeDefaultActiveIsTrue()
        {
            using var context = CreateDbContext();
            var unitType = new UnitType { UnitTypeName = "Penthouse" };

            context.UnitTypes.Add(unitType);
            context.SaveChanges();

            var retrieved = context.UnitTypes.First();
            Assert.True(retrieved.ActiveBool);
        }
    }
}
