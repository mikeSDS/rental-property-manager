using RentalPropertyManager.Models;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class UnitTypeTests
    {
        [Fact]
        public void UnitType_CanBeCreated()
        {
            var unitType = new UnitType { Id = 1, UnitTypeName = "Studio", ActiveBool = true };

            Assert.NotNull(unitType);
            Assert.Equal(1, unitType.Id);
            Assert.Equal("Studio", unitType.UnitTypeName);
            Assert.True(unitType.ActiveBool);
        }

        [Fact]
        public void UnitType_DefaultActiveIsTrue()
        {
            var unitType = new UnitType { UnitTypeName = "1-Bedroom" };

            Assert.True(unitType.ActiveBool);
        }

        [Fact]
        public void UnitType_EmptyNameInitialized()
        {
            var unitType = new UnitType();

            Assert.NotNull(unitType.UnitTypeName);
            Assert.Equal(string.Empty, unitType.UnitTypeName);
        }

        [Fact]
        public void UnitType_CanUpdateName()
        {
            var unitType = new UnitType { UnitTypeName = "Studio" };
            unitType.UnitTypeName = "1-Bedroom";

            Assert.Equal("1-Bedroom", unitType.UnitTypeName);
        }

        [Fact]
        public void UnitType_CanDeactivate()
        {
            var unitType = new UnitType { UnitTypeName = "Studio", ActiveBool = true };
            unitType.ActiveBool = false;

            Assert.False(unitType.ActiveBool);
        }
    }
}
