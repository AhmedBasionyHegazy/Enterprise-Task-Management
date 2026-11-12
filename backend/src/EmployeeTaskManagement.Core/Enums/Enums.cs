namespace EmployeeTaskManagement.Core.Enums
{
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Employee = "Employee";
    }

    public static class TaskPriorities
    {
        public const string Low = "Low";
        public const string Medium = "Medium";
        public const string High = "High";

        public static readonly string[] All = [Low, Medium, High];
    }

    public static class TaskStatuses
    {
        public const string New = "New";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";

        public static readonly string[] All = [New, InProgress, Completed];
    }
}
