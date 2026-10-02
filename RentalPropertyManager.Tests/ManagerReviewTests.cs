using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Controllers;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.Services;
using RentalPropertyManager.ViewModels.Review;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class ManagerReviewTests
    {
        private const string ManagerId = "manager-user-id";

        private static async Task<ApplicationDbContext> CreateSeededContextAsync()
        {
            var context = ApplicationTestHelper.CreateContext();
            context.ActionTypes.AddRange(ActionType.AllNames.Select(n => new ActionType { Name = n }));
            context.Users.Add(new ApplicationUser { Id = ManagerId, UserName = "mgr@test.com", Email = "mgr@test.com" });
            context.Users.Add(new ApplicationUser { Id = ApplicationTestHelper.OwnerId, UserName = "owner@test.com", Email = "owner@test.com" });
            await context.SaveChangesAsync();
            return context;
        }

        private static ReviewController CreateController(ApplicationDbContext context)
        {
            var controller = new ReviewController(context, new AuditService(context));
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ManagerId),
                new Claim(ClaimTypes.Role, "PropertyManager")
            }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        private static int StatusId(ApplicationDbContext context, string name) =>
            context.ApplicationStatuses.Single(s => s.Name == name).Id;

        [Fact]
        public void LeaseEnd_IsExactlyTwelveMonths_IncludingLeapYear()
        {
            Assert.Equal(new DateTime(2025, 1, 15), ReviewController.CalculateLeaseEnd(new DateTime(2024, 1, 15)));
            Assert.Equal(new DateTime(2025, 2, 28), ReviewController.CalculateLeaseEnd(new DateTime(2024, 2, 29)));
        }

        [Fact]
        public void IsReviewable_OnlySubmittedAndUnderReview()
        {
            Assert.True(ReviewController.IsReviewable(ApplicationStatus.Submitted));
            Assert.True(ReviewController.IsReviewable(ApplicationStatus.UnderReview));
            Assert.False(ReviewController.IsReviewable(ApplicationStatus.Approved));
            Assert.False(ReviewController.IsReviewable(ApplicationStatus.Draft));
        }

        [Fact]
        public void ReviewController_RequiresPropertyManagerRole()
        {
            var attr = (AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(ReviewController), typeof(AuthorizeAttribute))!;
            Assert.Equal("PropertyManager", attr.Roles);
        }

        [Fact]
        public async Task Approve_CreatesLeaseReviewAndHistory()
        {
            using var context = await CreateSeededContextAsync();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted);
            var controller = CreateController(context);

            var result = await controller.CompleteReview(new ReviewModalViewModel
            {
                ApplicationID = app.Id,
                OutcomeStatusID = StatusId(context, ApplicationStatus.Approved)
            });

            Assert.IsType<JsonResult>(result);
            var lease = context.Leases.Single(l => l.ApplicationID == app.Id);
            Assert.Equal(DateTime.UtcNow.Date, lease.StartDate);
            Assert.Equal(lease.StartDate.AddMonths(12), lease.EndDate);
            Assert.Equal(1500m, lease.MonthlyRent);
            Assert.Single(context.Reviews);
            var history = context.ActionHistories.Include(h => h.ActionType).Single();
            Assert.Equal(ActionType.Approve, history.ActionType.Name);
            Assert.Equal(ApplicationStatus.Submitted, history.FromStatus);
            Assert.Equal(ApplicationStatus.Approved, history.ToStatus);
        }

        [Theory]
        [InlineData(ApplicationStatus.Denied)]
        [InlineData(ApplicationStatus.Returned)]
        public async Task ReturnOrDeny_WithoutComment_Returns400(string outcome)
        {
            using var context = await CreateSeededContextAsync();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted);
            var controller = CreateController(context);

            var result = await controller.CompleteReview(new ReviewModalViewModel
            {
                ApplicationID = app.Id,
                OutcomeStatusID = StatusId(context, outcome),
                Comment = "  "
            });

            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal(400, controller.Response.StatusCode);
            Assert.NotEmpty(((ReviewModalViewModel)partial.Model!).AvailableOutcomes);
            Assert.True(controller.ModelState.ContainsKey("Comment"));
            Assert.Empty(context.Reviews);
        }

        [Fact]
        public async Task Approve_UnitWithActiveLease_IsRejected()
        {
            using var context = await CreateSeededContextAsync();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted);
            context.Leases.Add(new Lease
            {
                UnitID = app.UnitID,
                StartDate = DateTime.UtcNow.Date.AddMonths(-1),
                EndDate = DateTime.UtcNow.Date.AddMonths(11),
                MonthlyRent = 1500
            });
            await context.SaveChangesAsync();
            var controller = CreateController(context);

            var result = await controller.CompleteReview(new ReviewModalViewModel
            {
                ApplicationID = app.Id,
                OutcomeStatusID = StatusId(context, ApplicationStatus.Approved)
            });

            Assert.IsType<PartialViewResult>(result);
            Assert.Equal(400, controller.Response.StatusCode);
            Assert.Equal(StatusId(context, ApplicationStatus.Submitted), context.Applications.Single().ApplicationStatusID);
            Assert.Single(context.Leases);
        }

        [Fact]
        public async Task Review_TerminalStatus_IsRejected()
        {
            using var context = await CreateSeededContextAsync();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Approved);
            var controller = CreateController(context);

            Assert.IsType<BadRequestObjectResult>(await controller.ReviewModal(app.Id));
        }

        [Fact]
        public async Task ApplicantSubmit_WritesSubmissionHistory()
        {
            using var context = await CreateSeededContextAsync();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft, withResidence: true);
            var controller = ApplicationTestHelper.CreateController(context);

            await controller.Wizard(app.Id, new RentalPropertyManager.ViewModels.Applications.ApplicationWizardViewModel { CurrentStep = 3 }, "Submit");

            var history = context.ActionHistories.Include(h => h.ActionType).Single();
            Assert.Equal(ActionType.Submission, history.ActionType.Name);
            Assert.Equal(ApplicationStatus.Draft, history.FromStatus);
        }

        [Fact]
        public async Task ApplicantWithdraw_WritesWithdrawHistory()
        {
            using var context = await CreateSeededContextAsync();
            var app = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Submitted);
            var controller = ApplicationTestHelper.CreateController(context);

            await controller.Withdraw(app.Id);

            var history = context.ActionHistories.Include(h => h.ActionType).Single();
            Assert.Equal(ActionType.Withdraw, history.ActionType.Name);
            Assert.Equal(ApplicationStatus.Withdrawn, history.ToStatus);
        }
    }
}
