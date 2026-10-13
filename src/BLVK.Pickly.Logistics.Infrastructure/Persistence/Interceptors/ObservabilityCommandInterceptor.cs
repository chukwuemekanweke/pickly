using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Diagnostics;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Interceptors;

internal sealed class ObservabilityCommandInterceptor(
    ILogger<ObservabilityCommandInterceptor> logger,
    ICurrentActor currentActor) : DbCommandInterceptor
{
    private static readonly TimeSpan SlowQueryThreshold = TimeSpan.FromMilliseconds(500);
    private const int MaxSqlLength = 2000;

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        TrackSlowQuery(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        TrackSlowQuery(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        TrackSlowQuery(command, eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        TrackSlowQuery(command, eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        TrackSlowQuery(command, eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        TrackSlowQuery(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void TrackSlowQuery(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration < SlowQueryThreshold)
        {
            return;
        }

        var durationMs = eventData.Duration.TotalMilliseconds;
        var tenantId = currentActor.TenantId?.ToString() ?? string.Empty;
        var commandText = Truncate(command.CommandText, MaxSqlLength);

        logger.LogWarning(
            "Slow SQL query detected. DurationMs={DurationMs}, TenantId={TenantId}, ActorId={ActorId}, CorrelationId={CorrelationId}, CommandSource={CommandSource}, Sql={Sql}",
            durationMs,
            tenantId,
            currentActor.ActorId,
            currentActor.CorrelationId,
            eventData.CommandSource,
            commandText);

        Activity.Current?.AddEvent(new ActivityEvent(
            "db.slow_query",
            tags: new ActivityTagsCollection
            {
                ["db.duration.ms"] = durationMs,
                ["db.command_source"] = eventData.CommandSource.ToString(),
                ["tenant.id"] = tenantId,
                ["actor.id"] = currentActor.ActorId,
                ["correlation.id"] = currentActor.CorrelationId
            }));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
