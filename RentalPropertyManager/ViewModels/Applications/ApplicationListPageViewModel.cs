namespace RentalPropertyManager.ViewModels.Applications
{
    public class ApplicationListViewModel
    {
        public string? Status { get; set; }
        public string? Search { get; set; }
        public IReadOnlyList<string> Statuses { get; set; } = [];
        public List<ApplicationListItemViewModel> Items { get; set; } = [];
    }
}
