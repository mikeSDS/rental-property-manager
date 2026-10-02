using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels.Review;

namespace RentalPropertyManager.ViewComponents
{
    public class StatusHistoryTimelineViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public StatusHistoryTimelineViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int applicationId)
        {
            var histories = await _context.ActionHistories
                .AsNoTracking()
                .Where(h => h.ApplicationID == applicationId)
                .Select(h => new
                {
                    h.Id,
                    h.Date,
                    Actor = h.User.Email ?? h.User.UserName,
                    Action = h.ActionType.Name,
                    h.FromStatus,
                    h.ToStatus
                })
                .ToListAsync();

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.ApplicationID == applicationId && r.Comment != null)
                .Select(r => new { r.UserID, r.ReviewDate, r.Comment })
                .ToListAsync();

            var items = histories
                .Select(h =>
                {
                    // Match the review comment recorded with the same decision
                    var comment = reviews
                        .Where(r => Math.Abs((r.ReviewDate - h.Date).TotalSeconds) < 5)
                        .Select(r => r.Comment)
                        .FirstOrDefault();

                    return new StatusHistoryItemViewModel
                    {
                        HistoryID = h.Id,
                        Timestamp = h.Date,
                        ActorName = h.Actor ?? string.Empty,
                        ActionName = h.Action,
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus ?? string.Empty,
                        Comment = comment,
                        BadgeCssClass = ApplicationStatus.BadgeCssClass(h.ToStatus)
                    };
                })
                .OrderByDescending(i => i.Timestamp)
                .ToList();

            return View("Default", items);
        }
    }
}
