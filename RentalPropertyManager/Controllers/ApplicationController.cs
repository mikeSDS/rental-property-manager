using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels.Applications;

namespace RentalPropertyManager.Controllers
{
    [Authorize(Roles = "Applicant")]
    public class ApplicationController : Controller
    {
        private const int FirstStep = 1;
        private const int LastStep = 3;
        private const string WizardBodyView = "Partials/_WizardContent";

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationController> _logger;

        public ApplicationController(ApplicationDbContext context, ILogger<ApplicationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// GET /Application/Create?unitId={unitId} - Start (or resume) a draft application for a unit.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(int unitId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var unitExists = await _context.Units.AnyAsync(u => u.Id == unitId);
            if (!unitExists)
            {
                return NotFound();
            }

            var draftStatus = await GetStatusAsync(ApplicationStatus.Draft);

            // Resume an existing draft for the same unit rather than creating duplicates
            var existing = await _context.Applications
                .Where(a => a.UnitID == unitId
                            && a.ApplicantUserID == userId
                            && a.ApplicationStatusID == draftStatus.Id)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return RedirectToAction(nameof(Wizard), new { id = existing.Id, step = FirstStep });
            }

            var application = new Application
            {
                UnitID = unitId,
                ApplicantUserID = userId,
                Date = DateTime.UtcNow,
                ApplicationStatusID = draftStatus.Id
            };

            // An Applicant (person) can be linked to many applications through the ApplicationApplicant cross-reference
            var applicant = await _context.Applicants.OrderBy(a => a.Id).FirstOrDefaultAsync(a => a.UserID == userId)
                ?? new Applicant
                {
                    UserID = userId,
                    Name = string.Empty,
                    Phone = string.Empty,
                    Email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? string.Empty,
                    CurrentAddress = string.Empty
                };

            application.ApplicationApplicants.Add(new ApplicationApplicant
            {
                Applicant = applicant,
                IsPrimary = true
            });

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Wizard), new { id = application.Id, step = FirstStep });
        }

        /// <summary>
        /// GET /Application/Wizard/{id}?step={step} - Display the wizard.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Wizard(int id, int? step)
        {
            var (application, error) = await GetOwnedApplicationAsync(id, requireEditable: false);
            if (error != null)
            {
                return error;
            }

            var isReadOnly = !ApplicationStatus.IsEditable(application!.Status.Name);
            var requestedStep = step ?? (isReadOnly ? LastStep : FirstStep);

            return View("Wizard", BuildViewModel(application, ClampStep(requestedStep)));
        }

        /// <summary>
        /// POST /Application/Wizard/{id} - Continue, Back or Submit.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Wizard(int id, ApplicationWizardViewModel model, string buttonAction)
        {
            var (application, error) = await GetOwnedApplicationAsync(id, requireEditable: true);
            if (error != null)
            {
                return error;
            }

            var currentStep = ClampStep(model.CurrentStep);

            switch (buttonAction)
            {
                case "Back":
                    ModelState.Clear();
                    return WizardBody(BuildViewModel(application!, ClampStep(currentStep - 1)));

                case "Continue":
                    return await ContinueAsync(application!, model, currentStep);

                case "Submit":
                    return await SubmitAsync(application!, currentStep);

                default:
                    return BadRequest(new { success = false, error = "Unknown action." });
            }
        }

        /// <summary>
        /// GET /Application/ResidenceModal?applicationId={appId}&amp;residenceId={resId?}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResidenceModal(int applicationId, int? residenceId)
        {
            var (application, error) = await GetOwnedApplicationAsync(applicationId, requireEditable: true);
            if (error != null)
            {
                return error;
            }

            var link = GetPrimaryLink(application!);
            var viewModel = new ResidenceModalViewModel
            {
                ApplicationID = application!.Id,
                ApplicantID = link.ApplicantID
            };

            if (residenceId.HasValue && residenceId.Value > 0)
            {
                var residence = GetAllResidences(application).FirstOrDefault(r => r.Id == residenceId.Value);
                if (residence == null)
                {
                    return NotFound();
                }

                viewModel.ResidenceID = residence.Id;
                viewModel.Street = residence.Street;
                viewModel.City = residence.City;
                viewModel.State = residence.State;
                viewModel.Zip = residence.Zip;
                viewModel.LandlordName = residence.LandlordName;
                viewModel.LandlordPhone = residence.LandlordPhone;
                viewModel.MoveInDate = residence.MoveInDate;
                viewModel.MoveOutDate = residence.MoveOutDate;
            }

            return PartialView("Modals/_ResidenceModal", viewModel);
        }

        /// <summary>
        /// POST /Application/SaveResidence - Create or update a residence history record.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResidence(ResidenceModalViewModel model)
        {
            var (application, error) = await GetOwnedApplicationAsync(model.ApplicationID, requireEditable: true);
            if (error != null)
            {
                return error;
            }

            var link = GetPrimaryLink(application!);

            // Re-run validation server-side so DataAnnotations and IValidatableObject rules always apply
            ModelState.Clear();
            AddValidationErrors(model, string.Empty);

            if (!ModelState.IsValid)
            {
                model.ApplicantID = link.ApplicantID;
                var invalid = PartialView("Modals/_ResidenceModal", model);
                invalid.StatusCode = StatusCodes.Status400BadRequest;
                return invalid;
            }

            ResidenceHistory residence;
            if (model.ResidenceID > 0)
            {
                var existing = GetAllResidences(application).FirstOrDefault(r => r.Id == model.ResidenceID);
                if (existing == null)
                {
                    return NotFound();
                }

                residence = existing;
            }
            else
            {
                residence = new ResidenceHistory
                {
                    ApplicationApplicantID = link.Id
                };
                _context.ResidenceHistories.Add(residence);
            }

            residence.Street = model.Street.Trim();
            residence.City = model.City.Trim();
            residence.State = model.State.Trim();
            residence.Zip = model.Zip.Trim();
            residence.LandlordName = model.LandlordName.Trim();
            residence.LandlordPhone = model.LandlordPhone.Trim();
            residence.MoveInDate = model.MoveInDate!.Value;
            residence.MoveOutDate = model.MoveOutDate;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, applicationId = model.ApplicationID });
        }

        /// <summary>
        /// POST /Application/DeleteResidence/{id}
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResidence(int id)
        {
            var residence = await _context.ResidenceHistories
                .Include(r => r.ApplicationApplicant)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (residence == null)
            {
                return NotFound();
            }

            var (_, error) = await GetOwnedApplicationAsync(residence.ApplicationApplicant.ApplicationID, requireEditable: true);
            if (error != null)
            {
                return error;
            }

            _context.ResidenceHistories.Remove(residence);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        /// <summary>
        /// GET /Application/ResidenceTablePartial?applicationId={id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResidenceTablePartial(int applicationId)
        {
            var (application, error) = await GetOwnedApplicationAsync(applicationId, requireEditable: false);
            if (error != null)
            {
                return error;
            }

            return PartialView("Partials/_ResidenceTable", BuildViewModel(application!, 2));
        }

        private async Task<IActionResult> ContinueAsync(Application application, ApplicationWizardViewModel model, int currentStep)
        {
            if (currentStep >= LastStep)
            {
                return BadRequest(new { success = false, error = "There is no next step." });
            }

            if (currentStep == 1)
            {
                var info = model.ApplicantInfo ?? new ApplicantFormViewModel();

                ModelState.Clear();
                AddValidationErrors(info, nameof(ApplicationWizardViewModel.ApplicantInfo));

                if (!ModelState.IsValid)
                {
                    var invalid = BuildViewModel(application, 1);
                    invalid.ApplicantInfo.Name = info.Name;
                    invalid.ApplicantInfo.Phone = info.Phone;
                    invalid.ApplicantInfo.Email = info.Email;
                    invalid.ApplicantInfo.CurrentAddress = info.CurrentAddress;
                    return WizardBody(invalid, StatusCodes.Status400BadRequest);
                }

                var applicant = GetPrimaryLink(application).Applicant;
                applicant.Name = info.Name.Trim();
                applicant.Phone = info.Phone.Trim();
                applicant.Email = info.Email.Trim();
                applicant.CurrentAddress = info.CurrentAddress.Trim();
                await _context.SaveChangesAsync();
            }

            ModelState.Clear();
            return WizardBody(BuildViewModel(application, currentStep + 1));
        }

        private async Task<IActionResult> SubmitAsync(Application application, int currentStep)
        {
            if (currentStep != LastStep)
            {
                return BadRequest(new { success = false, error = "Application can only be submitted from the summary step." });
            }

            ModelState.Clear();

            var viewModel = BuildViewModel(application, LastStep);
            var errorStep = 0;

            if (!TryValidate(viewModel.ApplicantInfo))
            {
                ModelState.AddModelError(string.Empty, "Applicant information is incomplete. Please complete Step 1.");
                errorStep = 1;
            }
            else if (viewModel.Residences.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one residence in Step 2 before submitting.");
                errorStep = 2;
            }

            if (errorStep != 0)
            {
                viewModel.CurrentStep = errorStep;
                return WizardBody(viewModel, StatusCodes.Status400BadRequest);
            }

            var today = DateTime.UtcNow.Date;
            var hasActiveLease = await _context.Leases.AnyAsync(l =>
                l.UnitID == application.UnitID && l.StartDate <= today && l.EndDate >= today);
            if (hasActiveLease)
            {
                ModelState.AddModelError(string.Empty, "This unit currently has an active lease and cannot accept applications.");
                viewModel.CurrentStep = LastStep;
                return WizardBody(viewModel, StatusCodes.Status400BadRequest);
            }

            var submittedStatus = await GetStatusAsync(ApplicationStatus.Submitted);
            application.ApplicationStatusID = submittedStatus.Id;
            application.Status = submittedStatus;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Application {ApplicationId} submitted by user {UserId}", application.Id, CurrentUserId);

            ModelState.Clear();
            return WizardBody(BuildViewModel(application, LastStep));
        }

        /// <summary>
        /// POST /Application/Withdraw/{id} - Withdraw an owned application.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var (application, error) = await GetOwnedApplicationAsync(id, requireEditable: false);
            if (error != null)
            {
                return error;
            }

            if (!ApplicationStatus.CanWithdraw(application!.Status.Name))
            {
                return BadRequest(new { success = false, error = "This application can no longer be withdrawn." });
            }

            var withdrawn = await GetStatusAsync(ApplicationStatus.Withdrawn);
            application.ApplicationStatusID = withdrawn.Id;
            application.Status = withdrawn;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Application {ApplicationId} withdrawn by user {UserId}", application.Id, CurrentUserId);
            return RedirectToAction("Index", "ApplicationList");
        }

        private async Task<ApplicationStatus> GetStatusAsync(string name) =>
            await _context.ApplicationStatuses.FirstAsync(s => s.Name == name);

        private async Task<(Application? Application, IActionResult? Error)> GetOwnedApplicationAsync(int id, bool requireEditable)
        {
            var application = await _context.Applications
                .Include(a => a.Status)
                .Include(a => a.PropertyUnit).ThenInclude(u => u.Property)
                .Include(a => a.ApplicationApplicants).ThenInclude(x => x.Applicant)
                .Include(a => a.ApplicationApplicants).ThenInclude(x => x.ResidenceHistories)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return (null, NotFound());
            }

            if (string.IsNullOrEmpty(CurrentUserId) || application.ApplicantUserID != CurrentUserId)
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden));
            }

            if (requireEditable && !ApplicationStatus.IsEditable(application.Status.Name))
            {
                return (null, BadRequest(new { success = false, error = "This application can no longer be edited." }));
            }

            if (!application.ApplicationApplicants.Any())
            {
                _logger.LogError("Application {ApplicationId} has no applicant record", application.Id);
                return (null, StatusCode(StatusCodes.Status500InternalServerError));
            }

            return (application, null);
        }

        private static ApplicationApplicant GetPrimaryLink(Application application) =>
            application.ApplicationApplicants.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Id).First();

        private static IEnumerable<ResidenceHistory> GetAllResidences(Application application) =>
            application.ApplicationApplicants.SelectMany(x => x.ResidenceHistories);

        private static ApplicationWizardViewModel BuildViewModel(Application application, int step)
        {
            var link = GetPrimaryLink(application);
            var applicant = link.Applicant;

            return new ApplicationWizardViewModel
            {
                ApplicationID = application.Id,
                UnitID = application.UnitID,
                UnitNumber = application.PropertyUnit?.UnitNumber ?? string.Empty,
                PropertyName = application.PropertyUnit?.Property?.Name ?? string.Empty,
                MonthlyRent = application.PropertyUnit?.MonthlyRent ?? 0,
                CurrentStep = step,
                StatusName = application.Status.Name,
                IsReadOnly = !ApplicationStatus.IsEditable(application.Status.Name),
                ApplicantInfo = new ApplicantFormViewModel
                {
                    ApplicantID = applicant.Id,
                    ApplicationID = application.Id,
                    Name = applicant.Name,
                    Phone = applicant.Phone,
                    Email = applicant.Email,
                    CurrentAddress = applicant.CurrentAddress
                },
                Residences = GetAllResidences(application)
                    .OrderByDescending(r => r.MoveInDate)
                    .Select(r => new ResidenceHistoryViewModel
                    {
                        ResidenceID = r.Id,
                        ApplicationID = application.Id,
                        ApplicantID = applicant.Id,
                        Street = r.Street,
                        City = r.City,
                        State = r.State,
                        Zip = r.Zip,
                        LandlordName = r.LandlordName,
                        LandlordPhone = r.LandlordPhone,
                        MoveInDate = r.MoveInDate,
                        MoveOutDate = r.MoveOutDate
                    })
                    .ToList()
            };
        }

        private static int ClampStep(int step) => Math.Min(LastStep, Math.Max(FirstStep, step));

        private PartialViewResult WizardBody(ApplicationWizardViewModel viewModel, int statusCode = StatusCodes.Status200OK)
        {
            var result = PartialView(WizardBodyView, viewModel);
            result.StatusCode = statusCode;
            return result;
        }

        private static bool TryValidate(object instance) =>
            Validator.TryValidateObject(instance, new ValidationContext(instance), new List<ValidationResult>(), validateAllProperties: true);

        private void AddValidationErrors(object instance, string prefix)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);

            foreach (var result in results)
            {
                var members = result.MemberNames.Any() ? result.MemberNames : new[] { string.Empty };
                foreach (var member in members)
                {
                    var key = string.IsNullOrEmpty(prefix) ? member : string.IsNullOrEmpty(member) ? prefix : $"{prefix}.{member}";
                    ModelState.AddModelError(key, result.ErrorMessage ?? "Invalid value.");
                }
            }
        }
    }
}
