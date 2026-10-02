namespace RentalPropertyManager.Models
{
    public class Unit
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to Property
        /// </summary>
        public int PropertyID { get; set; }

        /// <summary>
        /// Unit number or identifier (e.g., "101", "A", "1B")
        /// </summary>
        public string UnitNumber { get; set; } = string.Empty;

        /// <summary>
        /// Number of bedrooms (1-10)
        /// </summary>
        public int Bedrooms { get; set; }

        /// <summary>
        /// Monthly rent amount with decimal precision (18, 2)
        /// </summary>
        public decimal MonthlyRent { get; set; }

        /// <summary>
        /// Foreign key to UnitType
        /// </summary>
        public int UnitTypeID { get; set; }

        /// <summary>
        /// Navigation property to associated Property
        /// </summary>
        public Property Property { get; set; } = null!;

        /// <summary>
        /// Navigation property to associated UnitType
        /// </summary>
        public UnitType UnitType { get; set; } = null!;
    }
}
