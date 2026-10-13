namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class IpInfoOptions
{
    public const string SectionName = "IpGeolocation:IpInfo";

    public string BaseUrl { get; init; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BaseUrl);
    }
}
