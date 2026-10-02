namespace RentalPropertyManager.Models
{
    public class ApplicationStatus
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string Returned = "Returned";
        public const string Approved = "Approved";
        public const string Denied = "Denied";
        public const string Withdrawn = "Withdrawn";

        public static readonly string[] AllNames = [Draft, Submitted, Returned, Approved, Denied, Withdrawn];

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// True when an application in the given status may be edited by the applicant.
        /// </summary>
        public static bool IsEditable(string? statusName) =>
            statusName == Draft || statusName == Returned;
    }
}
