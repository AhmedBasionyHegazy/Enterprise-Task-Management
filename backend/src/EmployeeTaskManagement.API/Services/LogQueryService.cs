using System.Globalization;
using System.Text.Json;
using EmployeeTaskManagement.API.Logging;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Logging;
using Microsoft.Extensions.Options;

namespace EmployeeTaskManagement.API.Services
{
    public interface ILogQueryService
    {
        Task<PaginatedResult<SystemLogEntryDto>> QueryAsync(SystemLogQueryParameters query, CancellationToken ct = default);
    }

    /// <summary>
    /// Reads daily rolling JSON-lines log files produced by FileLoggerProvider and applies server-side filtering.
    /// Read-only and bounded so large log archives cannot exhaust memory.
    /// </summary>
    public sealed class LogQueryService : ILogQueryService
    {
        private readonly string _directory;

        public LogQueryService(IOptions<FileLoggingOptions> options, IHostEnvironment env)
        {
            var dir = options.Value.Directory;
            _directory = Path.IsPathRooted(dir) ? dir : Path.Combine(env.ContentRootPath, dir);
        }

        public async Task<PaginatedResult<SystemLogEntryDto>> QueryAsync(SystemLogQueryParameters query, CancellationToken ct = default)
        {
            var page = Math.Max(1, query.PageNumber);
            var size = Math.Clamp(query.PageSize, 1, 100);

            if (!Directory.Exists(_directory))
                return new PaginatedResult<SystemLogEntryDto>(Array.Empty<SystemLogEntryDto>(), 0, page, size);

            var files = Directory.EnumerateFiles(_directory, "app-*.log")
                .OrderByDescending(f => f)
                .ToList();

            var matches = new List<SystemLogEntryDto>();
            const int maxScannedMatches = 2000;

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                if (!FileInDateWindow(file, query.FromUtc, query.ToUtc)) continue;

                var lines = new List<string>();
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync(ct)) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
                    }
                }

                for (var i = lines.Count - 1; i >= 0; i--)
                {
                    if (TryParse(lines[i], out var dto) && Matches(dto, query))
                    {
                        matches.Add(dto);
                        if (matches.Count >= maxScannedMatches) break;
                    }
                }

                if (matches.Count >= maxScannedMatches) break;
            }

            var total = matches.Count;
            var paged = matches
                .OrderByDescending(x => x.TimestampUtc)
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            return new PaginatedResult<SystemLogEntryDto>(paged, total, page, size);
        }

        private static bool FileInDateWindow(string path, DateTime? fromUtc, DateTime? toUtc)
        {
            var name = Path.GetFileNameWithoutExtension(path); // app-yyyyMMdd
            if (name.Length != 12 || !DateTime.TryParseExact(name.AsSpan(4), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var day))
                return true;
            var dayUtc = DateTime.SpecifyKind(day.Date, DateTimeKind.Utc);
            if (fromUtc.HasValue && dayUtc.AddDays(1) < fromUtc.Value.ToUniversalTime()) return false;
            if (toUtc.HasValue && dayUtc > toUtc.Value.ToUniversalTime()) return false;
            return true;
        }

        private static bool Matches(SystemLogEntryDto e, SystemLogQueryParameters q)
        {
            if (!string.IsNullOrWhiteSpace(q.Level) &&
                !string.Equals(e.Level, q.Level.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            if (q.UserId.HasValue)
            {
                var target = q.UserId.Value.ToString(CultureInfo.InvariantCulture);
                if (!string.Equals(e.UserId, target, StringComparison.Ordinal))
                    return false;
            }

            if (q.TaskId.HasValue)
            {
                var tStr = q.TaskId.Value.ToString(CultureInfo.InvariantCulture);
                var matchedTask = string.Equals(e.TaskId, tStr, StringComparison.Ordinal)
                    || (!string.IsNullOrEmpty(e.Path) && (e.Path.EndsWith($"/tasks/{tStr}", StringComparison.OrdinalIgnoreCase)
                                                          || e.Path.Contains($"/tasks/{tStr}/", StringComparison.OrdinalIgnoreCase)));
                if (!matchedTask) return false;
            }

            if (!string.IsNullOrWhiteSpace(q.Action))
            {
                var act = q.Action.Trim();
                if ((e.Action == null || !e.Action.Contains(act, StringComparison.OrdinalIgnoreCase)) &&
                    !e.Message.Contains(act, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(q.CorrelationId) &&
                !string.Equals(e.CorrelationId, q.CorrelationId.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            if (q.FromUtc.HasValue && e.TimestampUtc < q.FromUtc.Value.ToUniversalTime())
                return false;
            if (q.ToUtc.HasValue && e.TimestampUtc > q.ToUtc.Value.ToUniversalTime())
                return false;

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                var hit = e.Message.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || e.Category.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || (e.Username?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (e.Path?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (e.CorrelationId?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (e.ExceptionMessage?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false);
                if (!hit) return false;
            }

            return true;
        }

        private static bool TryParse(string json, out SystemLogEntryDto dto)
        {
            dto = new SystemLogEntryDto();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                dto.TimestampUtc = DateTime.TryParse(GetStr(root, "timestamp"), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var ts)
                    ? DateTime.SpecifyKind(ts, DateTimeKind.Utc)
                    : DateTime.UtcNow;
                dto.Level = GetStr(root, "level") ?? string.Empty;
                dto.Category = GetStr(root, "category") ?? string.Empty;
                dto.Message = GetStr(root, "message") ?? string.Empty;
                dto.CorrelationId = GetStr(root, "CorrelationId");
                dto.TraceId = GetStr(root, "TraceId");
                dto.ClientIp = GetStr(root, "ClientIp");
                dto.UserId = GetScalar(root, "UserId") ?? GetScalar(root, "userId");
                dto.Username = GetStr(root, "Username") ?? GetStr(root, "username");
                dto.TaskId = GetScalar(root, "TaskId") ?? GetScalar(root, "taskId");
                dto.Action = GetStr(root, "Action") ?? GetStr(root, "action");
                dto.Method = GetStr(root, "Method");
                dto.Path = GetStr(root, "Path");
                dto.StatusCode = root.TryGetProperty("StatusCode", out var sc) && sc.TryGetInt32(out var code) ? code : null;
                dto.ElapsedMs = root.TryGetProperty("ElapsedMs", out var ms) && ms.TryGetDouble(out var elapsed) ? elapsed : null;

                if (root.TryGetProperty("exception", out var ex) && ex.ValueKind == JsonValueKind.Object)
                {
                    dto.ExceptionType = GetStr(ex, "type");
                    dto.ExceptionMessage = GetStr(ex, "message");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? GetStr(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static string? GetScalar(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var v)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                _ => null
            };
        }
    }
}
