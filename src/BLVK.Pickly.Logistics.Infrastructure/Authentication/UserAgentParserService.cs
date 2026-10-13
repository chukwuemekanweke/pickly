using BLVK.Pickly.Logistics.Domain.Authentication.Services;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class UserAgentParserService : IUserAgentParserService
{
    private static readonly (string Prefix, string Browser)[] BrowserPatterns =
    [
        ("OPR/", "Opera"),
        ("Edg/", "Edge"),
        ("Chrome/", "Chrome"),
        ("Firefox/", "Firefox"),
        ("Safari/", "Safari"),
        ("MSIE ", "Internet Explorer"),
        ("Trident/", "Internet Explorer"),
        ("Mozilla/", "Other")
    ];

    private static readonly (string Prefix, string Platform)[] PlatformPatterns =
    [
        ("Windows NT 10.0", "Windows 10"),
        ("Windows NT 6.3", "Windows 8.1"),
        ("Windows NT 6.2", "Windows 8"),
        ("Windows NT 6.1", "Windows 7"),
        ("Mac OS X", "macOS"),
        ("Linux", "Linux"),
        ("Android", "Android"),
        ("iPhone", "iOS"),
        ("iPad", "iPadOS")
    ];

    public UserAgentInfo Parse(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new UserAgentInfo(null, null, null);
        }

        var browserName = ExtractBrowser(userAgent);
        var (devicePlatform, deviceName) = ExtractPlatformAndDevice(userAgent);

        return new UserAgentInfo(null, devicePlatform, browserName);
    }

    private static string? ExtractBrowser(string userAgent)
    {
        foreach (var (prefix, browser) in BrowserPatterns)
        {
            if (userAgent.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return browser;
            }
        }

        return "Unknown";
    }

    private static (string? Platform, string? Device) ExtractPlatformAndDevice(string userAgent)
    {
        foreach (var (prefix, platform) in PlatformPatterns)
        {
            if (userAgent.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var device = platform switch
                {
                    "Android" => ExtractAndroidDevice(userAgent),
                    "iOS" => ExtractIosDevice(userAgent),
                    "macOS" => userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ? "iPad" : "Mac",
                    _ => null
                };

                return (platform, device);
            }
        }

        return (null, null);
    }

    private static string? ExtractAndroidDevice(string userAgent)
    {
        var startIndex = userAgent.IndexOf("Android", StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return "Android Device";
        }

        var afterAndroid = userAgent[(startIndex + "Android".Length)..].TrimStart();
        var endIndex = afterAndroid.IndexOf(';');
        if (endIndex > 0)
        {
            var deviceInfo = afterAndroid[..endIndex].Trim();
            var versionStart = deviceInfo.IndexOf(' ');
            return versionStart > 0 ? deviceInfo[..versionStart] : deviceInfo;
        }

        return "Android Device";
    }

    private static string? ExtractIosDevice(string userAgent)
    {
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            return "iPad";
        }

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
        {
            return "iPhone";
        }

        return "iOS Device";
    }
}