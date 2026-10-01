using Microsoft.AspNetCore.Identity;

namespace RentalPropertyManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// User type: "Applicant" or "PropertyManager"
        /// </summary>
        public string? UserType { get; set; }

        /// <summary>
        /// Account creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
