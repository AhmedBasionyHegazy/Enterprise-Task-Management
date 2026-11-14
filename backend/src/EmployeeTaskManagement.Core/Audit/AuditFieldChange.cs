using System.Collections.Generic;
using System.Text.Json;

namespace EmployeeTaskManagement.Core.Audit
{
    public record AuditFieldChange(string Field, string? Old, string? New);

    public static class AuditChangeSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public static string Serialize(IEnumerable<AuditFieldChange> changes) =>
            JsonSerializer.Serialize(changes, Options);

        public static List<AuditFieldChange> Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<AuditFieldChange>();
            try { return JsonSerializer.Deserialize<List<AuditFieldChange>>(json, Options) ?? new(); }
            catch (JsonException) { return new List<AuditFieldChange>(); }
        }
    }
}
