using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels.Applications;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class ApplicationSecurityAndSeedingTests
    {
        [Fact]
        public async Task Wizard_AccessOtherUserApplication_ReturnsHttp403Forbidden()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context, "some-other-user");

            var result = await controller.Wizard(application.Id, (int?)null);

            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public async Task SaveResidence_OtherUserApplication_ReturnsHttp403Forbidden()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context, "some-other-user");

            var result = await controller.SaveResidence(new ResidenceModalViewModel { ApplicationID = application.Id });

            Assert.Equal(403, Assert.IsType<StatusCodeResult>(result).StatusCode);
        }

        [Theory]
        [InlineData(ApplicationStatus.Submitted)]
        [InlineData(ApplicationStatus.Approved)]
        [InlineData(ApplicationStatus.Denied)]
        [InlineData(ApplicationStatus.Withdrawn)]
        public async Task PostUpdate_NonEditableApplication_ReturnsHttp400Or403(string statusName)
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, statusName);
            var controller = ApplicationTestHelper.CreateController(context);

            var model = new ApplicationWizardViewModel
            {
                CurrentStep = 1,
                ApplicantInfo = new ApplicantFormViewModel
                {
                    Name = "Hacker",
                    Phone = "555-111-2222",
                    Email = "hacker@example.com",
                    CurrentAddress = "Somewhere"
                }
            };

            var result = await controller.Wizard(application.Id, model, "Continue");

            var code = result switch
            {
                BadRequestObjectResult => 400,
                StatusCodeResult s => s.StatusCode,
                _ => 0
            };
            Assert.True(code == 400 || code == 403);
            Assert.Equal("Original Name", (await context.Applicants.SingleAsync()).Name);
        }

        [Fact]
        public async Task PostUpdate_SubmittedApplication_DeleteResidenceRejected()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted, withResidence: true);
            var controller = ApplicationTestHelper.CreateController(context);
            var residence = await context.ResidenceHistories.SingleAsync();

            var result = await controller.DeleteResidence(residence.Id);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Single(context.ResidenceHistories);
        }

        [Fact]
        public async Task DbInitializer_SeedingIdempotency()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            using var provider = services.BuildServiceProvider();

            await DbInitializer.InitializeAsync(provider);
            var first = await CountsAsync(provider);

            await DbInitializer.InitializeAsync(provider);
            var second = await CountsAsync(provider);

            Assert.Equal(first, second);
            Assert.Equal(7, first.Statuses);
            Assert.True(first.Applications >= 6);
            Assert.True(first.Applicants >= 6);
            Assert.True(first.Residences > 0);
        }

        [Fact]
        public async Task DbInitializer_SeedsApplicationInEveryStatus()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            using var provider = services.BuildServiceProvider();
            await DbInitializer.InitializeAsync(provider);

            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreach (var name in ApplicationStatus.AllNames)
            {
                Assert.True(await context.Applications.AnyAsync(a => a.Status.Name == name), $"No seeded application with status {name}");
            }
        }

        private static async Task<(int Statuses, int Applications, int Applicants, int Residences)> CountsAsync(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return (
                await context.ApplicationStatuses.CountAsync(),
                await context.Applications.CountAsync(),
                await context.Applicants.CountAsync(),
                await context.ResidenceHistories.CountAsync());
        }
    }
}
