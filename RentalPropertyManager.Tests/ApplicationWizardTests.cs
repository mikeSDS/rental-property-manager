using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RentalPropertyManager.Controllers;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels.Applications;
using Xunit;

namespace RentalPropertyManager.Tests
{
    internal static class ApplicationTestHelper
    {
        public const string OwnerId = "owner-user-id";

        public static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();

            context.ApplicationStatuses.AddRange(ApplicationStatus.AllNames.Select(n => new ApplicationStatus { Name = n }));
            context.SaveChanges();
            return context;
        }

        public static ApplicationController CreateController(ApplicationDbContext context, string userId = OwnerId)
        {
            var controller = new ApplicationController(context, NullLogger<ApplicationController>.Instance);
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, "applicant@realestate.com")
            }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        public static async Task<Application> CreateApplicationAsync(ApplicationDbContext context, string statusName, string ownerId = OwnerId, bool withResidence = false)
        {
            var unitType = new UnitType { UnitTypeName = "Loft", ActiveBool = true };
            var property = new Property { Name = "Test Property", StreetAddress = "1 Main St", City = "City", State = "ST", ZipCode = "12345", CreatedAt = DateTime.UtcNow };
            context.UnitTypes.Add(unitType);
            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var unit = new Unit { PropertyID = property.Id, UnitNumber = "101", Bedrooms = 2, MonthlyRent = 1500, UnitTypeID = unitType.Id };
            context.Units.Add(unit);
            await context.SaveChangesAsync();

            var status = await context.ApplicationStatuses.FirstAsync(s => s.Name == statusName);
            var application = new Application
            {
                UnitID = unit.Id,
                ApplicantUserID = ownerId,
                ApplicationStatusID = status.Id,
                Date = DateTime.UtcNow
            };
            var applicant = new Applicant
            {
                UserID = ownerId,
                Name = "Original Name",
                Phone = "555-123-4567",
                Email = "original@example.com",
                CurrentAddress = "1 Original Way"
            };
            var link = new ApplicationApplicant { Applicant = applicant, IsPrimary = true };
            application.ApplicationApplicants.Add(link);

            if (withResidence)
            {
                link.ResidenceHistories.Add(new ResidenceHistory
                {
                    Street = "9 Old St",
                    City = "Town",
                    State = "ST",
                    Zip = "11111",
                    LandlordName = "Landlord",
                    LandlordPhone = "555-000-1111",
                    MoveInDate = DateTime.UtcNow.AddYears(-3),
                    MoveOutDate = DateTime.UtcNow.AddYears(-1)
                });
            }

            context.Applications.Add(application);
            await context.SaveChangesAsync();
            return application;
        }
    }

    public class ApplicationWizardTests
    {
        private static ApplicationWizardViewModel ModelFor(Application application, int step, string name, string email) => new()
        {
            ApplicationID = application.Id,
            CurrentStep = step,
            ApplicantInfo = new ApplicantFormViewModel
            {
                Name = name,
                Phone = "555-987-6543",
                Email = email,
                CurrentAddress = "42 New Address Ln"
            }
        };

        [Fact]
        public async Task Continue_Step1_ValidModel_AdvancesToStep2()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context);

            var result = await controller.Wizard(application.Id, ModelFor(application, 1, "New Name", "new@example.com"), "Continue");

            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.True(partial.StatusCode == null || partial.StatusCode == 200);
            var model = Assert.IsType<ApplicationWizardViewModel>(partial.Model);
            Assert.Equal(2, model.CurrentStep);

            var saved = await context.Applicants.SingleAsync();
            Assert.Equal("New Name", saved.Name);
            Assert.Equal("new@example.com", saved.Email);
        }

        [Fact]
        public async Task Continue_Step1_InvalidModel_ReturnsHttp400_StaysOnStep1()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context);

            var result = await controller.Wizard(application.Id, ModelFor(application, 1, "", "not-an-email"), "Continue");

            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal(400, partial.StatusCode);
            var model = Assert.IsType<ApplicationWizardViewModel>(partial.Model);
            Assert.Equal(1, model.CurrentStep);
            Assert.True(controller.ModelState.ContainsKey("ApplicantInfo.Name"));
            Assert.True(controller.ModelState.ContainsKey("ApplicantInfo.Email"));

            var saved = await context.Applicants.SingleAsync();
            Assert.Equal("Original Name", saved.Name);
        }

        [Fact]
        public async Task Back_Step2_NavigatesToStep1_NoDatabaseSave()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context);

            // Invalid posted data must be ignored by Back
            var result = await controller.Wizard(application.Id, ModelFor(application, 2, "", "bad"), "Back");

            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.True(partial.StatusCode == null || partial.StatusCode == 200);
            var model = Assert.IsType<ApplicationWizardViewModel>(partial.Model);
            Assert.Equal(1, model.CurrentStep);
            Assert.True(controller.ModelState.IsValid);

            var saved = await context.Applicants.SingleAsync();
            Assert.Equal("Original Name", saved.Name);
        }

        [Fact]
        public async Task SaveResidence_MoveOutBeforeMoveIn_ReturnsValidationError()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context);

            var model = new ResidenceModalViewModel
            {
                ApplicationID = application.Id,
                Street = "1 St",
                City = "City",
                State = "ST",
                Zip = "12345",
                LandlordName = "Landlord",
                LandlordPhone = "555-111-2222",
                MoveInDate = new DateTime(2023, 6, 1),
                MoveOutDate = new DateTime(2023, 1, 1)
            };

            var result = await controller.SaveResidence(model);

            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal(400, partial.StatusCode);
            Assert.True(controller.ModelState.ContainsKey(nameof(ResidenceModalViewModel.MoveOutDate)));
            Assert.Empty(context.ResidenceHistories);
        }

        [Fact]
        public async Task SaveResidence_ValidModel_SavesAndReturnsSuccess()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context);

            var model = new ResidenceModalViewModel
            {
                ApplicationID = application.Id,
                Street = "1 St",
                City = "City",
                State = "ST",
                Zip = "12345",
                LandlordName = "Landlord",
                LandlordPhone = "555-111-2222",
                MoveInDate = new DateTime(2023, 1, 1),
                MoveOutDate = null
            };

            var result = await controller.SaveResidence(model);

            Assert.IsType<OkObjectResult>(result);
            var saved = await context.ResidenceHistories.SingleAsync();
            Assert.Equal(application.ApplicationApplicants.Single().Id, saved.ApplicationApplicantID);
        }

        [Theory]
        [InlineData(ApplicationStatus.Submitted)]
        [InlineData(ApplicationStatus.Approved)]
        [InlineData(ApplicationStatus.Denied)]
        [InlineData(ApplicationStatus.Withdrawn)]
        public async Task Wizard_NonDraftStatus_SetsIsReadOnlyTrue(string statusName)
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, statusName);
            var controller = ApplicationTestHelper.CreateController(context);

            var result = await controller.Wizard(application.Id, (int?)null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ApplicationWizardViewModel>(view.Model);
            Assert.True(model.IsReadOnly);
            Assert.Equal(statusName, model.StatusName);
        }

        [Theory]
        [InlineData(ApplicationStatus.Draft)]
        [InlineData(ApplicationStatus.Returned)]
        public async Task Wizard_EditableStatus_SetsIsReadOnlyFalse(string statusName)
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, statusName);
            var controller = ApplicationTestHelper.CreateController(context);

            var result = await controller.Wizard(application.Id, (int?)null);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(Assert.IsType<ApplicationWizardViewModel>(view.Model).IsReadOnly);
        }

        [Fact]
        public async Task Submit_FromStep3_WithCompleteData_TransitionsToSubmitted()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft, withResidence: true);
            var controller = ApplicationTestHelper.CreateController(context);

            var result = await controller.Wizard(application.Id, new ApplicationWizardViewModel { CurrentStep = 3 }, "Submit");

            var partial = Assert.IsType<PartialViewResult>(result);
            var model = Assert.IsType<ApplicationWizardViewModel>(partial.Model);
            Assert.True(model.IsReadOnly);
            Assert.Equal(ApplicationStatus.Submitted, model.StatusName);

            var saved = await context.Applications.Include(a => a.Status).SingleAsync();
            Assert.Equal(ApplicationStatus.Submitted, saved.Status.Name);
        }

        [Fact]
        public async Task Submit_NotOnStep3_ReturnsBadRequest()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft, withResidence: true);
            var controller = ApplicationTestHelper.CreateController(context);

            var result = await controller.Wizard(application.Id, new ApplicationWizardViewModel { CurrentStep = 2 }, "Submit");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Submit_WithoutResidence_ReturnsHttp400()
        {
            using var context = ApplicationTestHelper.CreateContext();
            var application = await ApplicationTestHelper.CreateApplicationAsync(context, ApplicationStatus.Draft);
            var controller = ApplicationTestHelper.CreateController(context);

            var result = await controller.Wizard(application.Id, new ApplicationWizardViewModel { CurrentStep = 3 }, "Submit");

            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal(400, partial.StatusCode);
            var saved = await context.Applications.Include(a => a.Status).SingleAsync();
            Assert.Equal(ApplicationStatus.Draft, saved.Status.Name);
        }
    }
}
