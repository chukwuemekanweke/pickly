namespace BLVK.Pickly.Logistics.Domain.Common.Observability;

public interface ICustomTelemetryContext
{
    ICustomTelemetryContext SetProperty(string key, string value);

    ICustomTelemetryContext AddCustomEvent(string name, Dictionary<string, string>? properties = null);
}
