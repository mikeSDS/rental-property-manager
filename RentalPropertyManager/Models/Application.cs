namespace RentalPropertyManager.Models
{
    public class Application
    {
        public int Id { get; set; }

        public int UnitID { get; set; }

        public string ApplicantUserID { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public int ApplicationStatusID { get; set; }

        public Unit PropertyUnit { get; set; } = null!;

        public ApplicationStatus Status { get; set; } = null!;

        public ICollection<ApplicationApplicant> ApplicationApplicants { get; set; } = new List<ApplicationApplicant>();
    }
}
