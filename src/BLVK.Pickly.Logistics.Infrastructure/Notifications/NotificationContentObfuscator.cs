namespace BLVK.Pickly.Logistics.Infrastructure.Notifications;

internal static class NotificationContentObfuscator
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "passwd",
        "pwd",
        "otp",
        "token",
        "secret",
        "passcode",
        "pin"
    ];

    public static Dictionary<string, string> Obfuscate(Dictionary<string, string> content)
    {
        return content.ToDictionary(
            pair => pair.Key,
            pair => IsSensitive(pair.Key) ? "***" : pair.Value);
    }

    private static bool IsSensitive(string key) =>
        SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
