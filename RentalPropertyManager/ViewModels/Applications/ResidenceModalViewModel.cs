using System.ComponentModel.DataAnnotations;

namespace RentalPropertyManager.ViewModels.Applications
{
    /// <summary>
    /// Form view model bound to the residence add/edit modal.
    /// </summary>
    public class ResidenceModalViewModel : IValidatableObject
    {
        public int ResidenceID { get; set; }

        public int ApplicationID { get; set; }

        public int ApplicantID { get; set; }

        [Required]
        [StringLength(200)]
        public string Street { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Zip { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Landlord Name")]
        public string LandlordName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Landlord Phone")]
        public string LandlordPhone { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Move-In Date")]
        public DateTime? MoveInDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Move-Out Date")]
        public DateTime? MoveOutDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MoveInDate.HasValue && MoveOutDate.HasValue && MoveOutDate.Value < MoveInDate.Value)
            {
                yield return new ValidationResult(
                    "Move-out date must be on or after the move-in date.",
                    new[] { nameof(MoveOutDate) });
            }
        }
    }
}
