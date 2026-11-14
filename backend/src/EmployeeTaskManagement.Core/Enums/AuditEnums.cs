namespace EmployeeTaskManagement.Core.Enums
{
    public static class AuditActions
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string StatusChange = "StatusChange";
        public const string Assign = "Assign";
        public const string Comment = "Comment";
        public const string Login = "Login";
        public const string LoginFailed = "LoginFailed";
        public const string Logout = "Logout";
        public const string LogoutAll = "LogoutAll";
        public const string TokenReuse = "TokenReuse";
        public const string SessionRevoked = "SessionRevoked";
        public const string AccessDenied = "AccessDenied";

        public static readonly string[] All =
        [
            Create, Update, Delete, StatusChange, Assign, Comment, Login, LoginFailed,
            Logout, LogoutAll, TokenReuse, SessionRevoked, AccessDenied
        ];
    }

    public static class NotificationCategories
    {
        public const string Task = "Task";
        public const string Comment = "Comment";
        public const string Admin = "Admin";

        public static readonly string[] All = [Task, Comment, Admin];
    }

    public static class NotificationTypes
    {
        public const string TaskAssigned = "TaskAssigned";
        public const string TaskReassigned = "TaskReassigned";
        public const string TaskUnassigned = "TaskUnassigned";
        public const string TaskUpdated = "TaskUpdated";
        public const string TaskStatusChanged = "TaskStatusChanged";
        public const string TaskDeleted = "TaskDeleted";
        public const string TaskCommented = "TaskCommented";
        public const string UserCreated = "UserCreated";
        public const string UserStatusChanged = "UserStatusChanged";
    }
}
