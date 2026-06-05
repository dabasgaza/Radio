using DataAccess.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class DiagnosticLogDto
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "Information";
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string? SourceContext { get; set; }
        public string? Sql { get; set; }
        public double? DurationMs { get; set; }
        public bool IsSlowQuery { get; set; }
    }

    public class DiagnosticsSummaryDto
    {
        public int TotalLogs { get; set; }
        public int TotalErrors { get; set; }
        public int TotalWarnings { get; set; }
        public int TotalQueries { get; set; }
        public int SlowQueriesCount { get; set; }
        public double AverageQueryTimeMs { get; set; }
    }

    public interface ISystemDiagnosticsService
    {
        Task<Result<List<DiagnosticLogDto>>> GetLogsAsync(string? level = null, string? searchTerm = null, int count = 200);
        Task<Result<DiagnosticsSummaryDto>> GetSummaryAsync();
        Task<Result<List<DiagnosticLogDto>>> GetSqlPerformanceLogsAsync(int count = 100);
        Task<Result> ClearLogsAsync();
    }
}
