using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using Microsoft.EntityFrameworkCore;

namespace RentalPropertyManager.Services
{
    public interface IAuditService
    {
        /// <summary>
        /// Adds an ActionHistory row for an application status change. Does not call SaveChanges.
        /// </summary>
        Task LogStatusChangeAsync(string actionTypeName, string userId, int applicationId, string? fromStatus, string? toStatus);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogStatusChangeAsync(string actionTypeName, string userId, int applicationId, string? fromStatus, string? toStatus)
        {
            var actionType = await _context.ActionTypes.FirstOrDefaultAsync(a => a.Name == actionTypeName);
            if (actionType == null)
            {
                actionType = new ActionType { Name = actionTypeName };
                _context.ActionTypes.Add(actionType);
            }

            _context.ActionHistories.Add(new ActionHistory
            {
                ActionType = actionType,
                UserID = userId,
                Date = DateTime.UtcNow,
                ApplicationID = applicationId,
                FromStatus = fromStatus,
                ToStatus = toStatus
            });
        }
    }
}
