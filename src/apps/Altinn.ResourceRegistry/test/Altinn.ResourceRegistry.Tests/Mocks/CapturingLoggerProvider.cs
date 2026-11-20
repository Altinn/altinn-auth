using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Tests.Mocks;

/// <summary>
/// Logger provider that keeps every log entry in memory, so a test can assert what was logged.
/// Register it with <c>services.AddSingleton&lt;ILoggerProvider&gt;(provider)</c>.
/// </summary>
public sealed class CapturingLoggerProvider
    : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries)
        : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => entries.Enqueue(new(category, logLevel, exception, formatter(state, exception)));
    }
}

/// <summary>
/// A log entry captured by <see cref="CapturingLoggerProvider"/>.
/// </summary>
public sealed record LogEntry(string Category, LogLevel Level, Exception? Exception, string Message);
