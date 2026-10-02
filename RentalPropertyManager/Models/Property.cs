namespace RentalPropertyManager.Models
{
    public class Property
    {
        public int Id { get; set; }

        /// <summary>
        /// Name of the property (e.g., "Downtown Apartments")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Street address (e.g., "123 Main St")
        /// </summary>
        public string StreetAddress { get; set; } = string.Empty;

        /// <summary>
        /// City name
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// State abbreviation (e.g., "CA")
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Zip code
        /// </summary>
        public string ZipCode { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when property was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to associated units
        /// </summary>
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}
