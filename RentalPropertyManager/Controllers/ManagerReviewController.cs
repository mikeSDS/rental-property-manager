using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.Services;
using RentalPropertyManager.ViewModels.Review;

namespace RentalPropertyManager.Controllers
{
    [Authorize(Roles = "PropertyManager")]
    public class ReviewController : Controller
    {
        private static readonly string[] OutcomeNames =
            [ApplicationStatus.Approved, ApplicationStatus.Returned, ApplicationStatus.Denied];

        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ReviewController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        public static bool IsReviewable(string? statusName) =>
            statusName == ApplicationStatus.Submitted || statusName == ApplicationStatus.UnderReview;

        public static DateTime CalculateLeaseEnd(DateTime start) => start.AddMonths(12);

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var application = await LoadApplicationAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            return View(BuildModel(application));
        }

        [HttpGet]
        public async Task<IActionResult> ReviewModal(int id)
        {
            var application = await LoadApplicationAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            if (!IsReviewable(application.Status.Name))
            {
                return BadRequest(new { success = false, error = "This application can no longer be reviewed." });
            }

            var model = BuildModel(application);
            await PopulateOutcomesAsync(model);
            return PartialView("_ReviewModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteReview(ReviewModalViewModel model)
        {
            var application = await LoadApplicationAsync(model.ApplicationID);
            if (application == null)
            {
                return NotFound();
            }

            if (!IsReviewable(application.Status.Name))
            {
                return BadRequest(new { success = false, error = "This application can no longer be reviewed." });
            }

            var outcome = await _context.ApplicationStatuses.FirstOrDefaultAsync(s => s.Id == model.OutcomeStatusID);
            if (outcome == null || !OutcomeNames.Contains(outcome.Name))
            {
                ModelState.AddModelError("OutcomeStatusID", "Please select a review outcome.");
            }
            else
            {
                if (outcome.Name != ApplicationStatus.Approved && string.IsNullOrWhiteSpace(model.Comment))
                {
                    ModelState.AddModelError("Comment", "A comment is required when returning or denying an application.");
                }

                if (outcome.Name == ApplicationStatus.Approved)
                {
                    var today = DateTime.UtcNow.Date;
                    var activeLease = await _context.Leases
                        .Where(l => l.UnitID == application.UnitID && l.StartDate <= today && l.EndDate >= today)
                        .OrderByDescending(l => l.EndDate)
                        .FirstOrDefaultAsync();
                    if (activeLease != null)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Cannot approve application: Unit {application.PropertyUnit.UnitNumber} currently has an active lease running until {activeLease.EndDate:d}.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                FillSummary(model, application);
                await PopulateOutcomesAsync(model);
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_ReviewModal", model);
            }

            var fromStatus = application.Status.Name;
            string? comment = null;
            if (!string.IsNullOrWhiteSpace(model.Comment))
            {
                comment = model.Comment.Trim();
            }

            application.ApplicationStatusID = outcome!.Id;
            application.Status = outcome;

            _context.Reviews.Add(new Models.Review
            {
                ApplicationID = application.Id,
                UserID = CurrentUserId,
                ReviewDate = DateTime.UtcNow,
                OutcomeApplicationStatusID = outcome.Id,
                Comment = comment
            });

            var actionName = outcome.Name == ApplicationStatus.Approved ? ActionType.Approve
                : outcome.Name == ApplicationStatus.Returned ? ActionType.Return
                : ActionType.Deny;
            await _audit.LogStatusChangeAsync(actionName, CurrentUserId, application.Id, fromStatus, outcome.Name);

            if (outcome.Name == ApplicationStatus.Approved)
            {
                var start = DateTime.UtcNow.Date;
                _context.Leases.Add(new Lease
                {
                    UnitID = application.UnitID,
                    ApplicationID = application.Id,
                    StartDate = start,
                    EndDate = CalculateLeaseEnd(start),
                    MonthlyRent = application.PropertyUnit.MonthlyRent,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Review completed successfully." });
        }

        private async Task<Application?> LoadApplicationAsync(int id) =>
            await _context.Applications
                .Include(a => a.Status)
                .Include(a => a.PropertyUnit).ThenInclude(u => u.Property)
                .Include(a => a.ApplicationApplicants).ThenInclude(x => x.Applicant)
                .FirstOrDefaultAsync(a => a.Id == id);

        private static void FillSummary(ReviewModalViewModel model, Application application)
        {
            model.ApplicationID = application.Id;
            model.UnitID = application.UnitID;
            model.PropertyName = application.PropertyUnit.Property.Name;
            model.UnitNumber = application.PropertyUnit.UnitNumber;
            model.MonthlyRent = application.PropertyUnit.MonthlyRent;
            model.CurrentStatusName = application.Status.Name;
            model.ApplicantName = application.ApplicationApplicants
                .Where(x => x.IsPrimary).Select(x => x.Applicant.Name).FirstOrDefault() ?? string.Empty;
        }

        private static ReviewModalViewModel BuildModel(Application application)
        {
            var model = new ReviewModalViewModel();
            FillSummary(model, application);
            return model;
        }

        private async Task PopulateOutcomesAsync(ReviewModalViewModel model)
        {
            var statuses = await _context.ApplicationStatuses
                .Where(s => OutcomeNames.Contains(s.Name))
                .ToListAsync();
            model.AvailableOutcomes = OutcomeNames
                .Select(n => statuses.FirstOrDefault(s => s.Name == n))
                .Where(s => s != null)
                .Select(s => new SelectListItem(s!.Name, s.Id.ToString(), s.Id == model.OutcomeStatusID))
                .ToList();
        }
    }
}
