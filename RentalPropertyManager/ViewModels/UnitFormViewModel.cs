using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RentalPropertyManager.ViewModels
{
    public class UnitFormViewModel
    {
        /// <summary>
        /// Unit ID for edit operations (null for create)
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Foreign key to Property (required)
        /// </summary>
        [Required(ErrorMessage = "Property is required.")]
        public int PropertyID { get; set; }

        /// <summary>
        /// Name of the property (for display)
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Unit number or identifier (required)
        /// </summary>
        [Required(ErrorMessage = "Unit number is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Unit number must be between 1 and 50 characters.")]
        public string UnitNumber { get; set; } = string.Empty;

        /// <summary>
        /// Number of bedrooms (required, 1-10)
        /// </summary>
        [Required(ErrorMessage = "Number of bedrooms is required.")]
        [Range(1, 10, ErrorMessage = "Bedrooms must be between 1 and 10.")]
        public int Bedrooms { get; set; }

        /// <summary>
        /// Monthly rent amount (required, 0.01 - 100000.00)
        /// </summary>
        [Required(ErrorMessage = "Monthly rent is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Monthly rent must be between 0.01 and 100,000.00.")]
        public decimal MonthlyRent { get; set; }

        /// <summary>
        /// Foreign key to UnitType (required)
        /// </summary>
        [Required(ErrorMessage = "Unit type is required.")]
        public int UnitTypeID { get; set; }

        /// <summary>
        /// List of available unit types for dropdown (populated by controller)
        /// </summary>
        public IEnumerable<SelectListItem> AvailableUnitTypes { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// List of available properties for dropdown (populated by controller)
        /// </summary>
        public IEnumerable<SelectListItem> AvailableProperties { get; set; } = new List<SelectListItem>();
    }
}
