using System.Threading.Tasks;

namespace EmployeeTaskManagement.Application.Interfaces.Audit
{
    /// <summary>
    /// Writes explicit audit events (access, authentication, rejected operations).
    /// Data changes are captured automatically by the DbContext in the same transaction as the change.
    /// Uses an independent connection so failure events survive a rolled-back business transaction.
    /// </summary>
    public interface IAuditService
    {
        Task LogAsync(
            string action,
            string entityName,
            string? recordId,
            bool isSuccess,
            string? description,
            int? userId = null,
            string? username = null,
            int? relatedTaskId = null);
    }
}
