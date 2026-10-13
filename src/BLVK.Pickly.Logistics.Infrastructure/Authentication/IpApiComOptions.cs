namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class IpApiComOptions
{
    public const string SectionName = "IpGeolocation:IpApiCom";

    public string BaseUrl { get; init; } = string.Empty;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BaseUrl);
    }
}
