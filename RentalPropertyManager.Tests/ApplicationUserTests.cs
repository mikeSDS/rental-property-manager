using System;
using RentalPropertyManager.Models;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class ApplicationUserTests
    {
        [Fact]
        public void ApplicationUser_CanBeCreated()
        {
            var user = new ApplicationUser { UserName = "testuser" };

            Assert.NotNull(user);
            Assert.Equal("testuser", user.UserName);
        }

        [Fact]
        public void ApplicationUser_CreatedAtDefaultsToNow()
        {
            var now = DateTime.UtcNow;
            var user = new ApplicationUser { UserName = "testuser" };

            Assert.True(user.CreatedAt >= now.AddSeconds(-1) && user.CreatedAt <= now.AddSeconds(1));
        }
    }
}
