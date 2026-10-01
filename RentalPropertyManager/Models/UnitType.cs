namespace RentalPropertyManager.Models
{
    public class UnitType
    {
        public int Id { get; set; }

        /// <summary>
        /// Lookup table name (e.g., "Active", "Inactive", "Studio", "1-Bedroom")
        /// </summary>
        public string UnitTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Whether this unit type is active and available for use
        /// </summary>
        public bool ActiveBool { get; set; } = true;
    }
}
