namespace BLVK.Pickly.Logistics.Domain.Authentication.Services;

public interface IUserAgentParserService
{
    UserAgentInfo Parse(string userAgent);
}
