using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels.Applications;

namespace RentalPropertyManager.Controllers
{
    [Authorize]
    public class ApplicationListController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApplicationListController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var model = await BuildAsync(status, search);
            return View("ListView", model);
        }

        [HttpGet]
        public async Task<IActionResult> Filter(string? status, string? search)
        {
            var model = await BuildAsync(status, search);
            return PartialView("_ApplicationRows", model.Items);
        }

        private async Task<ApplicationListViewModel> BuildAsync(string? status, string? search)
        {
            var query = _context.Applications.AsNoTracking().AsQueryable();

            // Privacy scoping happens in the database query
            if (!User.IsInRole("PropertyManager"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(a => a.ApplicantUserID == userId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status.Name == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a =>
                    a.PropertyUnit.UnitNumber.Contains(term) ||
                    a.PropertyUnit.Property.Name.Contains(term) ||
                    a.ApplicationApplicants.Any(x => x.Applicant.Name.Contains(term)));
            }

            var items = await query
                .OrderByDescending(a => a.Date)
                .Select(a => new ApplicationListItemViewModel
                {
                    Id = a.Id,
                    Date = a.Date,
                    PropertyName = a.PropertyUnit.Property.Name,
                    UnitNumber = a.PropertyUnit.UnitNumber,
                    StatusName = a.Status.Name,
                    PrimaryApplicantName = a.ApplicationApplicants
                        .Where(x => x.IsPrimary)
                        .Select(x => x.Applicant.Name)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToListAsync();

            return new ApplicationListViewModel
            {
                Status = status,
                Search = search,
                Statuses = ApplicationStatus.AllNames,
                Items = items
            };
        }
    }
}
