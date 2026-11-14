using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmployeeTaskManagement.API.Logging
{
    public class FileLoggingOptions
    {
        public bool Enabled { get; set; } = true;
        public string Directory { get; set; } = "logs";
        public int RetentionDays { get; set; } = 30;
    }

    /// <summary>
    /// Structured (JSON-lines) rolling file logger built on Microsoft.Extensions.Logging - no extra dependencies.
    /// One file per day, old files purged according to RetentionDays, sensitive values redacted.
    /// Search with e.g.:  Select-String -Path logs\*.log -Pattern '"CorrelationId":"abc..."'
    /// </summary>
    [ProviderAlias("File")]
    public sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly FileLoggingOptions _options;
        private readonly string _directory;
        private readonly BlockingCollection<string> _queue = new(new ConcurrentQueue<string>(), 10_000);
        private readonly Thread _worker;
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
        private DateTime _lastPurge = DateTime.MinValue;

        public FileLoggerProvider(FileLoggingOptions options, string contentRoot)
        {
            _options = options;
            _directory = Path.IsPathRooted(options.Directory) ? options.Directory : Path.Combine(contentRoot, options.Directory);
            System.IO.Directory.CreateDirectory(_directory);
            _worker = new Thread(Run) { IsBackground = true, Name = "FileLogger" };
            _worker.Start();
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this);
        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;
        internal IExternalScopeProvider Scopes => _scopes;
        internal bool Enabled => _options.Enabled;

        internal void Enqueue(string line)
        {
            if (!_queue.IsAddingCompleted) _queue.TryAdd(line);   // drop rather than block requests when saturated
        }

        private void Run()
        {
            foreach (var line in _queue.GetConsumingEnumerable())
            {
                try
                {
                    var path = Path.Combine(_directory, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
                    using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    using var w = new StreamWriter(fs);
                    w.WriteLine(line);
                    while (_queue.TryTake(out var more)) w.WriteLine(more);
                    PurgeOldFiles();
                }
                catch { /* logging must never crash the app */ }
            }
        }

        private void PurgeOldFiles()
        {
            if ((DateTime.UtcNow - _lastPurge).TotalHours < 6) return;
            _lastPurge = DateTime.UtcNow;
            var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays));
            foreach (var f in System.IO.Directory.EnumerateFiles(_directory, "app-*.log"))
                if (File.GetLastWriteTimeUtc(f) < cutoff) { try { File.Delete(f); } catch { } }
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _worker.Join(TimeSpan.FromSeconds(3));
        }
    }

    internal sealed class FileLogger : ILogger
    {
        private static readonly Regex Secrets = new(
            @"(?i)(password|passwd|pwd|token|secret|authorization|cookie|api[-_]?key|refresh)(""?\s*[:=]\s*""?)[^\s,;""']+",
            RegexOptions.Compiled);
        private static readonly Regex Jwt = new(@"eyJ[\w-]+\.[\w-]+\.[\w-]+", RegexOptions.Compiled);
        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
            { "password", "token", "secret", "authorization", "cookie", "refreshtoken", "accesstoken" };

        private readonly string _category;
        private readonly FileLoggerProvider _provider;

        public FileLogger(string category, FileLoggerProvider provider) { _category = category; _provider = provider; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _provider.Scopes.Push(state);
        public bool IsEnabled(LogLevel logLevel) => _provider.Enabled && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var entry = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["level"] = logLevel.ToString(),
                ["category"] = _category,
                ["message"] = Redact(formatter(state, exception))
            };
            if (eventId.Id != 0) entry["eventId"] = eventId.Id;

            // Scope values (CorrelationId, TraceId, ...)
            _provider.Scopes.ForEachScope((scope, dict) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> kvps)
                    foreach (var kv in kvps) AddField(dict, kv.Key, kv.Value);
            }, entry);

            // Structured properties of the message template
            if (state is IEnumerable<KeyValuePair<string, object?>> props)
                foreach (var kv in props)
                    if (kv.Key != "{OriginalFormat}") AddField(entry, kv.Key, kv.Value);

            if (exception != null)
            {
                var stack = exception.StackTrace ?? string.Empty;
                entry["exception"] = new
                {
                    type = exception.GetType().FullName,
                    message = Redact(exception.Message),
                    stackTrace = Redact(stack.Length > 4000 ? stack[..4000] : stack)
                };
            }

            _provider.Enqueue(JsonSerializer.Serialize(entry));
        }

        private static void AddField(Dictionary<string, object?> dict, string key, object? value)
        {
            if (SensitiveKeys.Contains(key)) { dict[key] = "[redacted]"; return; }
            dict[key] = value is string s ? Redact(s) : value;
        }

        private static string Redact(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Jwt.Replace(Secrets.Replace(input, "$1$2[redacted]"), "[redacted-jwt]");
        }
    }

    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, IConfiguration configuration, string contentRoot)
        {
            var options = configuration.GetSection("FileLogging").Get<FileLoggingOptions>() ?? new FileLoggingOptions();
            if (options.Enabled)
                builder.Services.AddSingleton<ILoggerProvider>(new FileLoggerProvider(options, contentRoot));
            return builder;
        }
    }
}
