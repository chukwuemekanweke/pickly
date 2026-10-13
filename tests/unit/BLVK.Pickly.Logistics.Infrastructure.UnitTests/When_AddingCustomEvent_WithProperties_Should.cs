using BLVK.Pickly.Logistics.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using Shouldly;
using System.Diagnostics;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_AddingCustomEvent_WithProperties_Should
{
    [Fact]
    public void EmitStructuredTelemetryLog()
    {
        using var activity = new Activity("custom-telemetry-test").Start();
        var logger = new CapturingLogger<CustomTelemetryContext>();
        var sut = new CustomTelemetryContext(logger);

        sut.AddCustomEvent("PasswordSignInStarted", new Dictionary<string, string>
        {
            ["flow.id"] = "flow-123",
            ["correlation_id"] = "correlation-456"
        });

        activity.Events.Count().ShouldBe(1);

        logger.Messages.Count.ShouldBe(1);

        using var payload = JsonDocument.Parse(logger.Messages.Single());
        payload.RootElement.GetProperty("custom_event_name").GetString().ShouldBe("PasswordSignInStarted");
        payload.RootElement.GetProperty("flow_id").GetString().ShouldBe("flow-123");
        payload.RootElement.GetProperty("correlation_id").GetString().ShouldBe("correlation-456");
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
