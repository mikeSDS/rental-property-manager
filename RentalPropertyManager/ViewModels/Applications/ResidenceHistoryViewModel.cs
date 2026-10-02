namespace RentalPropertyManager.ViewModels.Applications
{
    /// <summary>
    /// Read-only row representation of a prior residence (table and summary display).
    /// </summary>
    public class ResidenceHistoryViewModel
    {
        public int ResidenceID { get; set; }

        public int ApplicationID { get; set; }

        public int ApplicantID { get; set; }

        public string Street { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string Zip { get; set; } = string.Empty;

        public string LandlordName { get; set; } = string.Empty;

        public string LandlordPhone { get; set; } = string.Empty;

        public DateTime MoveInDate { get; set; }

        public DateTime? MoveOutDate { get; set; }
    }
}
