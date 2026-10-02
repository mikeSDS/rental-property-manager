using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentalPropertyManager.Controllers;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels.Applications;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class ApplicationV04Tests
    {
        private static ApplicationListController CreateListController(ApplicationDbContext context, string userId, string role)
        {
            var controller = new ApplicationListController(context);
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        [Fact]
        public async Task Withdraw_Submitted_SetsWithdrawn()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted);
            var controller = ApplicationTestHelper.CreateController(context);

            var result = await controller.Withdraw(app.Id);

            Assert.IsType<RedirectToActionResult>(result);
            var reloaded = context.Applications.Single(a => a.Id == app.Id);
            Assert.Equal(context.ApplicationStatuses.Single(s => s.Name == ApplicationStatus.Withdrawn).Id, reloaded.ApplicationStatusID);
        }

        [Fact]
        public async Task Withdraw_Approved_ReturnsBadRequest()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Approved);
            var controller = ApplicationTestHelper.CreateController(context);

            Assert.IsType<BadRequestObjectResult>(await controller.Withdraw(app.Id));
        }

        [Fact]
        public async Task Withdraw_OtherUser_Forbidden()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context, "someone-else");

            var result = await controller.Withdraw(app.Id);

            Assert.Equal(403, Assert.IsType<StatusCodeResult>(result).StatusCode);
        }

        [Fact]
        public async Task Submit_UnitWithActiveLease_IsBlocked()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft, withResidence: true);
            context.Leases.Add(new Lease
            {
                UnitID = app.UnitID,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddMonths(11),
                MonthlyRent = 1500
            });
            await context.SaveChangesAsync();
            var controller = ApplicationTestHelper.CreateController(context);

            await controller.Wizard(app.Id, new ApplicationWizardViewModel { CurrentStep = 3 }, "Submit");

            var status = context.Applications.Single(a => a.Id == app.Id).ApplicationStatusID;
            Assert.Equal(context.ApplicationStatuses.Single(s => s.Name == ApplicationStatus.Draft).Id, status);
        }

        [Fact]
        public async Task List_Applicant_SeesOnlyOwnApplications()
        {
            using var context = ApplicationTestHelper.CreateContext();
            await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted);
            await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted, ownerId: "other-user");
            var controller = CreateListController(context, ApplicationTestHelper.OwnerId, "Applicant");

            var view = Assert.IsType<ViewResult>(await controller.Index(null, null));
            var model = Assert.IsType<ApplicationListViewModel>(view.Model);

            Assert.Single(model.Items);
        }

        [Fact]
        public async Task List_Manager_SeesAllAndFiltersByStatus()
        {
            using var context = ApplicationTestHelper.CreateContext();
            await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted);
            await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft, ownerId: "other-user");
            var controller = CreateListController(context, "mgr", "PropertyManager");

            var all = Assert.IsType<ApplicationListViewModel>(Assert.IsType<ViewResult>(await controller.Index(null, null)).Model);
            var filtered = Assert.IsType<ApplicationListViewModel>(Assert.IsType<ViewResult>(await controller.Index(ApplicationStatus.Draft, null)).Model);

            Assert.Equal(2, all.Items.Count);
            Assert.Single(filtered.Items);
        }
    }
}
