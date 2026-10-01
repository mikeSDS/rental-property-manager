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

        [Fact]
        public void ApplicationUser_UserTypeCanBeApplicant()
        {
            var user = new ApplicationUser { UserName = "testuser", UserType = "Applicant" };

            Assert.Equal("Applicant", user.UserType);
        }

        [Fact]
        public void ApplicationUser_UserTypeCanBePropertyManager()
        {
            var user = new ApplicationUser { UserName = "testuser", UserType = "PropertyManager" };

            Assert.Equal("PropertyManager", user.UserType);
        }

        [Fact]
        public void ApplicationUser_UserTypeCanBeNull()
        {
            var user = new ApplicationUser { UserName = "testuser", UserType = null };

            Assert.Null(user.UserType);
        }
    }
}
