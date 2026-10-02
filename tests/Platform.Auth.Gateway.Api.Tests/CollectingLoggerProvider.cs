using Microsoft.Extensions.Logging;

namespace Platform.Auth.Gateway.Api.Tests;

internal sealed class CollectingLoggerProvider : ILoggerProvider
{
    public List<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CollectingLogger(Messages);

    public void Dispose()
    {
    }

    private sealed class CollectingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add(formatter(state, exception));
        }
    }
}
