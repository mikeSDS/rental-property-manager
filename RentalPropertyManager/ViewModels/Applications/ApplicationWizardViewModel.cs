namespace RentalPropertyManager.ViewModels.Applications
{
    /// <summary>
    /// Single view model driving all three wizard steps.
    /// </summary>
    public class ApplicationWizardViewModel
    {
        public int ApplicationID { get; set; }

        public int UnitID { get; set; }

        public string UnitNumber { get; set; } = string.Empty;

        public string PropertyName { get; set; } = string.Empty;

        public decimal MonthlyRent { get; set; }

        /// <summary>
        /// Current wizard step: 1 = Applicant Info, 2 = Residence History, 3 = Summary.
        /// </summary>
        public int CurrentStep { get; set; } = 1;

        public bool IsReadOnly { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public ApplicantFormViewModel ApplicantInfo { get; set; } = new();

        public List<ResidenceHistoryViewModel> Residences { get; set; } = new();
    }
}
