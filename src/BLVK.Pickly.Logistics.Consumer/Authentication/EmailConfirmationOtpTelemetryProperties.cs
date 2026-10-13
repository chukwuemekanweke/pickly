using BLVK.Pickly.Logistics.Domain.Common.Observability;
using System.Globalization;

namespace BLVK.Pickly.Logistics.Consumer.Authentication;

internal static class EmailConfirmationOtpTelemetryProperties
{
    public static Dictionary<string, string> Create(
        DateTimeOffset requestedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset processedAtUtc) =>
        new()
        {
            [Observability.PropertyNames.Common.RequestedAt] = requestedAtUtc.ToString("O"),
            [Observability.PropertyNames.Common.ExpiresAtUtc] = expiresAtUtc.ToString("O"),
            [Observability.PropertyNames.Common.QueueDelayMilliseconds] =
                (processedAtUtc - requestedAtUtc).TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
            [Observability.PropertyNames.Common.RemainingLifetimeMilliseconds] =
                (expiresAtUtc - processedAtUtc).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)
        };
}
