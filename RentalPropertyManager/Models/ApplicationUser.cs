using Microsoft.AspNetCore.Identity;

namespace RentalPropertyManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Account creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
