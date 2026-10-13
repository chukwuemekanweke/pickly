using BLVK.Pickly.Logistics.Domain.Common.Observability;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Observability;

public sealed class CustomTelemetryContext(ILogger<CustomTelemetryContext> logger) : ICustomTelemetryContext
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public ICustomTelemetryContext SetProperty(string key, string value)
    {
        Activity.Current?.SetTag(key, value);
        return this;
    }

    public ICustomTelemetryContext AddCustomEvent(string name, Dictionary<string, string>? properties = null)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            if (properties is null || properties.Count == 0)
            {
                activity.AddEvent(new ActivityEvent(name));
            }
            else
            {
                var tags = new ActivityTagsCollection();
                foreach (var property in properties)
                {
                    tags.Add(property.Key, property.Value);
                }

                activity.AddEvent(new ActivityEvent(name, tags: tags));
            }
        }

        WriteCustomEventLog(name, properties);
        return this;
    }

    private void WriteCustomEventLog(string name, Dictionary<string, string>? properties)
    {
        var logEntry = new Dictionary<string, string>
        {
            ["custom_event_name"] = name
        };

        if (properties is not null)
        {
            foreach (var property in properties)
            {
                logEntry[NormalizePropertyName(property.Key)] = property.Value;
            }
        }

        logger.LogInformation("{CustomTelemetryEvent}", JsonSerializer.Serialize(logEntry, SerializerOptions));
    }

    private static string NormalizePropertyName(string propertyName) =>
        propertyName.Replace(".", "_", StringComparison.Ordinal);
}
