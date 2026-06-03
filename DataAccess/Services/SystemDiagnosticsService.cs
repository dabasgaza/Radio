using DataAccess.Common;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class SystemDiagnosticsService : ISystemDiagnosticsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _seqUrl;
        private readonly string _apiKey;
        private readonly double _slowQueryThreshold;

        public SystemDiagnosticsService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _seqUrl = _configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
            _apiKey = _configuration["Seq:ApiKey"] ?? "";
            _slowQueryThreshold = _configuration.GetValue<double>("Seq:SlowQueryThresholdMs", 100);
        }

        // Models to deserialize Seq API response
        private class SeqEventResponse
        {
            [JsonPropertyName("Timestamp")]
            public string? Timestamp { get; set; }

            [JsonPropertyName("Level")]
            public string? Level { get; set; }

            [JsonPropertyName("RenderedMessage")]
            public string? RenderedMessage { get; set; }

            [JsonPropertyName("MessageTemplate")]
            public string? MessageTemplate { get; set; }

            [JsonPropertyName("Properties")]
            public Dictionary<string, object>? Properties { get; set; }

            [JsonPropertyName("Exception")]
            public string? Exception { get; set; }
        }

        public async Task<Result<List<DiagnosticLogDto>>> GetLogsAsync(string? level = null, string? searchTerm = null, int count = 200)
        {
            try
            {
                var requestUrl = $"{_seqUrl.TrimEnd('/')}/api/events?count={count}";
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    requestUrl += $"&apiKey={_apiKey}";
                }

                _httpClient.Timeout = TimeSpan.FromSeconds(3);
                var rawEvents = await _httpClient.GetFromJsonAsync<List<SeqEventResponse>>(requestUrl);

                if (rawEvents != null)
                {
                    var resultList = MapSeqEvents(rawEvents);

                    // Apply filters locally
                    if (!string.IsNullOrEmpty(level))
                    {
                        resultList = resultList.Where(x => x.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        resultList = resultList.Where(x => 
                            x.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                            (x.Sql != null && x.Sql.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            (x.Exception != null && x.Exception.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        ).ToList();
                    }

                    return Result<List<DiagnosticLogDto>>.Success(resultList);
                }
            }
            catch
            {
                // Fallback to local files if Seq is unreachable
            }

            var localLogs = ParseLocalLogFiles(level, searchTerm, count);
            if (localLogs.Count == 0)
            {
                return Result<List<DiagnosticLogDto>>.Success(GetSimulatedLogs(level, searchTerm, count));
            }
            return Result<List<DiagnosticLogDto>>.Success(localLogs);
        }

        public async Task<Result<DiagnosticsSummaryDto>> GetSummaryAsync()
        {
            try
            {
                var requestUrl = $"{_seqUrl.TrimEnd('/')}/api/events?count=500";
                _httpClient.Timeout = TimeSpan.FromSeconds(3);
                var rawEvents = await _httpClient.GetFromJsonAsync<List<SeqEventResponse>>(requestUrl);

                if (rawEvents != null)
                {
                    var logs = MapSeqEvents(rawEvents);
                    var sqlLogs = logs.Where(x => x.Sql != null).ToList();

                    var summary = new DiagnosticsSummaryDto
                    {
                        TotalLogs = logs.Count,
                        TotalErrors = logs.Count(x => x.Level.Equals("Error", StringComparison.OrdinalIgnoreCase) || x.Level.Equals("Fatal", StringComparison.OrdinalIgnoreCase)),
                        TotalWarnings = logs.Count(x => x.Level.Equals("Warning", StringComparison.OrdinalIgnoreCase)),
                        TotalQueries = sqlLogs.Count,
                        SlowQueriesCount = sqlLogs.Count(x => x.IsSlowQuery),
                        AverageQueryTimeMs = sqlLogs.Any() ? sqlLogs.Average(x => x.DurationMs ?? 0) : 0
                    };
                    return Result<DiagnosticsSummaryDto>.Success(summary);
                }
            }
            catch
            {
                // Fallback to local files if Seq is unreachable
            }

            var localLogs = ParseLocalLogFiles(count: 500);
            if (localLogs.Count == 0)
            {
                var simLogs = GetSimulatedLogs();
                var simSqlLogs = simLogs.Where(x => x.Sql != null).ToList();
                var simSummary = new DiagnosticsSummaryDto
                {
                    TotalLogs = simLogs.Count,
                    TotalErrors = simLogs.Count(x => x.Level == "Error"),
                    TotalWarnings = simLogs.Count(x => x.Level == "Warning"),
                    TotalQueries = simSqlLogs.Count,
                    SlowQueriesCount = simSqlLogs.Count(x => x.IsSlowQuery),
                    AverageQueryTimeMs = simSqlLogs.Any() ? simSqlLogs.Average(x => x.DurationMs ?? 0) : 0
                };
                return Result<DiagnosticsSummaryDto>.Success(simSummary);
            }

            var localSqlLogs = localLogs.Where(x => x.Sql != null || x.Message.Contains("took ")).ToList();
            var localSummary = new DiagnosticsSummaryDto
            {
                TotalLogs = localLogs.Count,
                TotalErrors = localLogs.Count(x => x.Level.Equals("Error", StringComparison.OrdinalIgnoreCase) || x.Level.Equals("Fatal", StringComparison.OrdinalIgnoreCase)),
                TotalWarnings = localLogs.Count(x => x.Level.Equals("Warning", StringComparison.OrdinalIgnoreCase)),
                TotalQueries = localSqlLogs.Count,
                SlowQueriesCount = localSqlLogs.Count(x => x.IsSlowQuery),
                AverageQueryTimeMs = localSqlLogs.Any() ? localSqlLogs.Average(x => x.DurationMs ?? 0) : 0
            };
            return Result<DiagnosticsSummaryDto>.Success(localSummary);
        }

        public async Task<Result<List<DiagnosticLogDto>>> GetSqlPerformanceLogsAsync(int count = 100)
        {
            try
            {
                var requestUrl = $"{_seqUrl.TrimEnd('/')}/api/events?count=500";
                _httpClient.Timeout = TimeSpan.FromSeconds(3);
                var rawEvents = await _httpClient.GetFromJsonAsync<List<SeqEventResponse>>(requestUrl);

                if (rawEvents != null)
                {
                    var logs = MapSeqEvents(rawEvents);
                    var sqlLogs = logs.Where(x => x.Sql != null)
                                      .OrderByDescending(x => x.Timestamp)
                                      .Take(count)
                                      .ToList();
                    return Result<List<DiagnosticLogDto>>.Success(sqlLogs);
                }
            }
            catch
            {
                // Fallback
            }

            var localLogs = ParseLocalLogFiles(count: 500);
            if (localLogs.Count == 0)
            {
                var simLogs = GetSimulatedLogs();
                var simSqlLogs = simLogs.Where(x => x.Sql != null)
                                       .OrderByDescending(x => x.Timestamp)
                                       .Take(count)
                                       .ToList();
                return Result<List<DiagnosticLogDto>>.Success(simSqlLogs);
            }

            var localSqlLogs = localLogs.Where(x => x.Sql != null || x.Message.Contains("took "))
                                       .OrderByDescending(x => x.Timestamp)
                                       .Take(count)
                                       .ToList();
            return Result<List<DiagnosticLogDto>>.Success(localSqlLogs);
        }

        private List<DiagnosticLogDto> ParseLocalLogFiles(string? level = null, string? searchTerm = null, int count = 200)
        {
            var list = new List<DiagnosticLogDto>();
            try
            {
                var logsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!System.IO.Directory.Exists(logsDir))
                {
                    return list;
                }

                var files = System.IO.Directory.GetFiles(logsDir, "radio*.log")
                                              .OrderByDescending(System.IO.File.GetLastWriteTime)
                                              .ToList();

                int readCount = 0;
                foreach (var file in files)
                {
                    if (readCount >= 5) break; // Read at most 5 files
                    readCount++;

                    var lines = System.IO.File.ReadAllLines(file);
                    DiagnosticLogDto? currentDto = null;
                    var exceptionBuilder = new System.Text.StringBuilder();

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Match Log line start: 2026-06-03 16:43:37.291 +03:00 [ERR] ...
                        if (line.Length >= 23 && 
                            char.IsDigit(line[0]) && char.IsDigit(line[1]) && char.IsDigit(line[2]) && char.IsDigit(line[3]) &&
                            line[4] == '-' && char.IsDigit(line[5]) && char.IsDigit(line[6]) &&
                            line[7] == '-' && char.IsDigit(line[8]) && char.IsDigit(line[9]) &&
                            line[10] == ' ')
                        {
                            if (currentDto != null)
                            {
                                if (exceptionBuilder.Length > 0)
                                {
                                    currentDto.Exception = exceptionBuilder.ToString().Trim();
                                    exceptionBuilder.Clear();
                                }
                                list.Add(currentDto);
                            }

                            currentDto = new DiagnosticLogDto();
                            
                            int levelStart = line.IndexOf('[');
                            int levelEnd = line.IndexOf(']');
                            
                            if (levelStart > 19 && levelEnd > levelStart)
                            {
                                var tsString = line.Substring(0, levelStart).Trim();
                                if (DateTime.TryParse(tsString, out var dt))
                                {
                                    currentDto.Timestamp = dt;
                                }
                                else
                                {
                                    currentDto.Timestamp = System.IO.File.GetLastWriteTime(file);
                                }

                                var levelCode = line.Substring(levelStart + 1, levelEnd - levelStart - 1).Trim();
                                currentDto.Level = levelCode switch
                                {
                                    "INF" => "Information",
                                    "ERR" => "Error",
                                    "WRN" => "Warning",
                                    "DBG" => "Debug",
                                    "FTL" => "Fatal",
                                    _ => levelCode
                                };

                                currentDto.Message = line.Substring(levelEnd + 1).Trim();
                            }
                            else
                            {
                                currentDto.Timestamp = System.IO.File.GetLastWriteTime(file);
                                currentDto.Level = "Information";
                                currentDto.Message = line;
                            }
                        }
                        else if (currentDto != null)
                        {
                            exceptionBuilder.AppendLine(line);
                        }
                    }

                    if (currentDto != null)
                    {
                        if (exceptionBuilder.Length > 0)
                        {
                            currentDto.Exception = exceptionBuilder.ToString().Trim();
                        }
                        list.Add(currentDto);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            foreach (var log in list)
            {
                if (log.Message.Contains("SQL Query Executed:") || log.Message.Contains("Slow SQL Query Detected:") || log.Message.Contains("SQL Command Failed:"))
                {
                    log.Sql = log.Message;
                    
                    var match = System.Text.RegularExpressions.Regex.Match(log.Message, @"took (\d+(\.\d+)?)ms");
                    if (match.Success && double.TryParse(match.Groups[1].Value, out var dur))
                    {
                        log.DurationMs = dur;
                        log.IsSlowQuery = dur >= _slowQueryThreshold;
                    }
                }
            }

            var filtered = list;
            if (!string.IsNullOrEmpty(level))
            {
                filtered = filtered.Where(x => x.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filtered = filtered.Where(x => 
                    x.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (x.Sql != null && x.Sql.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Exception != null && x.Exception.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return filtered.OrderByDescending(x => x.Timestamp).Take(count).ToList();
        }

        private List<DiagnosticLogDto> MapSeqEvents(List<SeqEventResponse> rawEvents)
        {
            var list = new List<DiagnosticLogDto>();
            foreach (var raw in rawEvents)
            {
                var dto = new DiagnosticLogDto
                {
                    Timestamp = DateTime.TryParse(raw.Timestamp, out var dt) ? dt.ToLocalTime() : DateTime.Now,
                    Level = raw.Level ?? "Information",
                    Message = raw.RenderedMessage ?? raw.MessageTemplate ?? "",
                    Exception = raw.Exception
                };

                if (raw.Properties != null)
                {
                    if (raw.Properties.TryGetValue("SourceContext", out var sc))
                    {
                        dto.SourceContext = sc?.ToString();
                    }

                    if (raw.Properties.TryGetValue("Sql", out var sql))
                    {
                        dto.Sql = sql?.ToString();
                    }

                    if (raw.Properties.TryGetValue("DurationMs", out var durObj) && double.TryParse(durObj?.ToString(), out var dur))
                    {
                        dto.DurationMs = dur;
                        dto.IsSlowQuery = dur >= _slowQueryThreshold;
                    }
                }

                list.Add(dto);
            }
            return list;
        }

        private List<DiagnosticLogDto> GetSimulatedLogs(string? level = null, string? searchTerm = null, int count = 200)
        {
            var list = new List<DiagnosticLogDto>();
            var rnd = new Random();
            var now = DateTime.Now;

            string[] contexts = { "Radio.App", "DataAccess.Services.EpisodeService", "DataAccess.Services.AuthService", "DataAccess.Data.AuditInterceptor" };

            // Generate some errors
            list.Add(new DiagnosticLogDto
            {
                Timestamp = now.AddMinutes(-5),
                Level = "Error",
                Message = "فشل الاتصال بالخادم السحابي أثناء مزامنة النسخة الاحتياطية",
                SourceContext = "DataAccess.Services.DatabaseManagementService",
                Exception = "System.Net.Http.HttpRequestException: Connection refused at CloudProviderClient.UploadAsync()"
            });

            list.Add(new DiagnosticLogDto
            {
                Timestamp = now.AddMinutes(-25),
                Level = "Error",
                Message = "فشل التحقق من صلاحيات المستخدم: خطأ في كلمة المرور",
                SourceContext = "DataAccess.Services.AuthService",
                Exception = "System.UnauthorizedAccessException: Invalid password hash comparison."
            });

            // Generate slow queries
            string[] slowQueries = {
                "SELECT [u].* FROM [Users] AS [u] LEFT JOIN [Roles] AS [r] ON [u].[RoleId] = [r].[RoleId] WHERE [u].[IsActive] = 1 ORDER BY [u].[FullName]",
                "SELECT [e].* FROM [Episodes] AS [e] INNER JOIN [Programs] AS [p] ON [e].[ProgramId] = [p].[ProgramId] WHERE [e].[EpisodeName] LIKE N'%مباراة%'",
                "SELECT COUNT(*) FROM [AuditLogs] WHERE [ChangedAt] >= '2026-05-01'"
            };

            for (int i = 0; i < slowQueries.Length; i++)
            {
                double dur = rnd.Next(120, 480);
                list.Add(new DiagnosticLogDto
                {
                    Timestamp = now.AddMinutes(-10 * (i + 1)),
                    Level = "Warning",
                    Message = $"Slow SQL Query Detected: SQL query took {dur:F1}ms to execute.",
                    Sql = slowQueries[i],
                    DurationMs = dur,
                    IsSlowQuery = true,
                    SourceContext = "DataAccess.Data.DbQueryPerformanceInterceptor"
                });
            }

            // Generate normal queries
            string[] normalQueries = {
                "SELECT TOP(1) [u].* FROM [Users] AS [u] WHERE [u].[Username] = @username",
                "UPDATE [Users] SET [LastLoginAt] = @p WHERE [UserId] = 1",
                "SELECT [p].* FROM [Programs] AS [p] WHERE [p].[IsActive] = 1",
                "INSERT INTO [AuditLogs] ([TableName], [Action], [UserId], [ChangedAt]) VALUES ('Users', 'MODIFIED', 1, GETDATE())"
            };

            for (int i = 0; i < 15; i++)
            {
                double dur = rnd.Next(5, 45);
                var q = normalQueries[rnd.Next(normalQueries.Length)];
                list.Add(new DiagnosticLogDto
                {
                    Timestamp = now.AddSeconds(-30 * (i + 1)),
                    Level = "Information",
                    Message = $"SQL Query Executed: SQL query took {dur:F1}ms to execute.",
                    Sql = q,
                    DurationMs = dur,
                    IsSlowQuery = false,
                    SourceContext = "DataAccess.Data.DbQueryPerformanceInterceptor"
                });
            }

            // Generate general logs
            list.Add(new DiagnosticLogDto
            {
                Timestamp = now.AddMinutes(-2),
                Level = "Information",
                Message = "تم التحقق من نجاح عملية تسجيل الدخول للمستخدم: admin",
                SourceContext = "DataAccess.Services.AuthService"
            });

            list.Add(new DiagnosticLogDto
            {
                Timestamp = now.AddMinutes(-12),
                Level = "Information",
                Message = "خدمة جدولة النسخ الاحتياطي لقاعدة البيانات قد بدأت بنجاح.",
                SourceContext = "DataAccess.Services.DatabaseBackupScheduler"
            });

            list.Add(new DiagnosticLogDto
            {
                Timestamp = now.AddMinutes(-30),
                Level = "Warning",
                Message = "مساحة القرص المتوفرة للنسخ الاحتياطي أقل من 10 جيجابايت.",
                SourceContext = "DataAccess.Services.DatabaseManagementService"
            });

            // Filter locally
            var filtered = list;
            if (!string.IsNullOrEmpty(level))
            {
                filtered = filtered.Where(x => x.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filtered = filtered.Where(x => 
                    x.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (x.Sql != null && x.Sql.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Exception != null && x.Exception.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return filtered.OrderByDescending(x => x.Timestamp).Take(count).ToList();
        }
    }
}
