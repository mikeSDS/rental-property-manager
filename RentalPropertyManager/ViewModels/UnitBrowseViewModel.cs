namespace RentalPropertyManager.ViewModels
{
    public class UnitBrowseViewModel
    {
        /// <summary>
        /// Unit ID
        /// </summary>
        public int UnitID { get; set; }

        /// <summary>
        /// Name of the property
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Unit number/identifier
        /// </summary>
        public string UnitNumber { get; set; } = string.Empty;

        /// <summary>
        /// Number of bedrooms
        /// </summary>
        public int Bedrooms { get; set; }

        /// <summary>
        /// Monthly rent amount
        /// </summary>
        public decimal MonthlyRent { get; set; }

        /// <summary>
        /// Unit type name (e.g., "Active", "Inactive")
        /// </summary>
        public string UnitTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the unit is available for application
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}
