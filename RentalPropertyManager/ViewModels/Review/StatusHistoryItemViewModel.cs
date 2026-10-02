namespace RentalPropertyManager.ViewModels.Review;

public class StatusHistoryItemViewModel
{
    public int HistoryID { get; set; }
    public DateTime Timestamp { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string BadgeCssClass { get; set; } = "bg-secondary";
}
