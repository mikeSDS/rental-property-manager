namespace RentalPropertyManager.Models
{
    public class ActionType
    {
        public const string Submission = "Submission";
        public const string StartReview = "StartReview";
        public const string CompleteReview = "CompleteReview";
        public const string Approve = "Approve";
        public const string Return = "Return";
        public const string Deny = "Deny";
        public const string Withdraw = "Withdraw";

        public static readonly string[] AllNames =
        [
            Submission, StartReview, CompleteReview, Approve, Return, Deny, Withdraw,
            "AddProperty", "EditProperty", "RemoveProperty", "AddUnit", "EditUnit", "RemoveUnit"
        ];

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
