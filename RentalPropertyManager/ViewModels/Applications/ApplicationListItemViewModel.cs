namespace RentalPropertyManager.ViewModels.Applications
{
    public class ApplicationListItemViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string PrimaryApplicantName { get; set; } = string.Empty;
    }
}
