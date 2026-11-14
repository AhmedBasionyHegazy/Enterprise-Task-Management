using System;
using System.Collections.Generic;

namespace EmployeeTaskManagement.Application.DTOs.Logging
{
    public class SystemLogQueryParameters
    {
        public string? Level { get; set; }
        public int? UserId { get; set; }
        public int? TaskId { get; set; }
        public string? Action { get; set; }
        public string? CorrelationId { get; set; }
        public string? Search { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class SystemLogEntryDto
    {
        public DateTime TimestampUtc { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public string? TraceId { get; set; }
        public string? ClientIp { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? TaskId { get; set; }
        public string? Action { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
        public int? StatusCode { get; set; }
        public double? ElapsedMs { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
    }
}
