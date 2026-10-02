namespace RentalPropertyManager.Models
{
    public class Applicant
    {
        public int Id { get; set; }

        public string UserID { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string CurrentAddress { get; set; } = string.Empty;

        public ICollection<ApplicationApplicant> ApplicationApplicants { get; set; } = new List<ApplicationApplicant>();
    }
}
