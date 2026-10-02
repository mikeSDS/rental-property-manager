using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RentalPropertyManager.ViewModels.Review;

public class ReviewModalViewModel
{
    public int ApplicationID { get; set; }
    public int UnitID { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string CurrentStatusName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a review outcome.")]
    public int OutcomeStatusID { get; set; }

    [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters.")]
    public string? Comment { get; set; }

    public IEnumerable<SelectListItem> AvailableOutcomes { get; set; } = new List<SelectListItem>();
}
