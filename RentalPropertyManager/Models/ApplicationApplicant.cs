namespace RentalPropertyManager.Models
{
    /// <summary>
    /// Cross-reference (junction) entity: many-to-many between Application and Applicant.
    /// </summary>
    public class ApplicationApplicant
    {
        public int Id { get; set; }

        public int ApplicationID { get; set; }

        public int ApplicantID { get; set; }

        public bool IsPrimary { get; set; } = true;

        public Application Application { get; set; } = null!;

        public Applicant Applicant { get; set; } = null!;

        public ICollection<ResidenceHistory> ResidenceHistories { get; set; } = new List<ResidenceHistory>();
    }
}
