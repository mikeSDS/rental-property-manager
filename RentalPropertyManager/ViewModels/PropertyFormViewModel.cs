using System.ComponentModel.DataAnnotations;

namespace RentalPropertyManager.ViewModels
{
    public class PropertyFormViewModel
    {
        /// <summary>
        /// Property ID for edit operations (null for create)
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Property name (required)
        /// </summary>
        [Required(ErrorMessage = "Property name is required.")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "Property name must be between 1 and 150 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Street address (optional)
        /// </summary>
        [StringLength(200, ErrorMessage = "Street address must not exceed 200 characters.")]
        public string? StreetAddress { get; set; }

        /// <summary>
        /// City name (optional)
        /// </summary>
        [StringLength(100, ErrorMessage = "City must not exceed 100 characters.")]
        public string? City { get; set; }

        /// <summary>
        /// State abbreviation (optional)
        /// </summary>
        [StringLength(50, ErrorMessage = "State must not exceed 50 characters.")]
        public string? State { get; set; }

        /// <summary>
        /// Zip code (optional)
        /// </summary>
        [StringLength(20, ErrorMessage = "Zip code must not exceed 20 characters.")]
        public string? ZipCode { get; set; }
    }
}
