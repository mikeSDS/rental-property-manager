using System.ComponentModel.DataAnnotations;

namespace RentalPropertyManager.ViewModels.Applications
{
    public class ApplicantFormViewModel
    {
        public int ApplicantID { get; set; }

        public int ApplicationID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Current Address")]
        public string CurrentAddress { get; set; } = string.Empty;
    }
}
