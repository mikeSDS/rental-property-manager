namespace RentalPropertyManager.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int ApplicationID { get; set; }

        public string UserID { get; set; } = string.Empty;

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public int OutcomeApplicationStatusID { get; set; }

        public string? Comment { get; set; }

        public Application Application { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public ApplicationStatus OutcomeStatus { get; set; } = null!;
    }
}
