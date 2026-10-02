namespace RentalPropertyManager.Models
{
    public class ActionHistory
    {
        public int Id { get; set; }

        public int ActionTypeID { get; set; }

        public string UserID { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public int? ApplicationID { get; set; }

        public int? UnitID { get; set; }

        public int? PropertyID { get; set; }

        public string? FromStatus { get; set; }

        public string? ToStatus { get; set; }

        public string? FromObject { get; set; }

        public string? ToObject { get; set; }

        public ActionType ActionType { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public Application? Application { get; set; }

        public Unit? Unit { get; set; }

        public Property? Property { get; set; }
    }
}
