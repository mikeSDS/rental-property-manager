namespace RentalPropertyManager.Models
{
    public class Lease
    {
        public int Id { get; set; }

        public int UnitID { get; set; }

        public int? ApplicationID { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        /// <summary>
        /// Copied from Unit.MonthlyRent at lease signing
        /// </summary>
        public decimal MonthlyRent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public Unit PropertyUnit { get; set; } = null!;

        public Application? Application { get; set; }
    }
}
