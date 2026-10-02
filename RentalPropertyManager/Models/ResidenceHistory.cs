namespace RentalPropertyManager.Models
{
    public class ResidenceHistory
    {
        public int Id { get; set; }

        public int ApplicationApplicantID { get; set; }

        public string Street { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string Zip { get; set; } = string.Empty;

        public string LandlordName { get; set; } = string.Empty;

        public string LandlordPhone { get; set; } = string.Empty;

        public DateTime MoveInDate { get; set; }

        public DateTime? MoveOutDate { get; set; }

        public ApplicationApplicant ApplicationApplicant { get; set; } = null!;
    }
}
