namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class IpWhoIsOptions
{
    public const string SectionName = "IpGeolocation:IpWhoIs";

    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BaseUrl);
    }
}
