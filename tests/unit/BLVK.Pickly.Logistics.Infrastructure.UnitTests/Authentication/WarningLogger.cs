using Microsoft.Extensions.Logging;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Authentication;

internal sealed class WarningLogger<T> : ILogger<T>
{
    public List<(EventId EventId, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        null;

    public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            Entries.Add((eventId, formatter(state, exception), exception));
        }
    }
}
