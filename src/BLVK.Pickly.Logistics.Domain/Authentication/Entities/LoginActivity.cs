using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Entities;

public sealed class LoginActivity : Entity, IAggregateRoot
{
    private const int MaxUserAgentLength = 500;
    private const int MaxDeviceNameLength = 200;
    private const int MaxDevicePlatformLength = 100;
    private const int MaxBrowserNameLength = 100;

    private LoginActivity()
    {
    }

    private LoginActivity(
        Guid stakeholderId,
        Guid tenantId,
        Guid ipAddressId,
        Guid? ipAddressLocationId,
        string userAgent,
        string? deviceName,
        string? devicePlatform,
        string? browserName,
        DateTimeOffset occurredAtUtc,
        LoginActivityType activityType)
    {
        StakeholderId = stakeholderId;
        TenantId = tenantId;
        IpAddressId = ipAddressId;
        IpAddressLocationId = ipAddressLocationId;
        UserAgent = NormalizeUserAgent(userAgent);
        DeviceName = NormalizeOptional(deviceName, MaxDeviceNameLength);
        DevicePlatform = NormalizeOptional(devicePlatform, MaxDevicePlatformLength);
        BrowserName = NormalizeOptional(browserName, MaxBrowserNameLength);
        OccurredAtUtc = occurredAtUtc;
        ActivityType = activityType;
    }

    public Guid StakeholderId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid IpAddressId { get; private set; }
    public IpAddress? IpAddress { get; private set; }
    public Guid? IpAddressLocationId { get; private set; }
    public IpAddressLocation? IpAddressLocation { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string? DeviceName { get; private set; }
    public string? DevicePlatform { get; private set; }
    public string? BrowserName { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public LoginActivityType ActivityType { get; private set; }

    public static LoginActivity CreateInitialLogin(
        Guid stakeholderId,
        Guid tenantId,
        Guid ipAddressId,
        Guid? ipAddressLocationId,
        string userAgent,
        string? deviceName,
        string? devicePlatform,
        string? browserName,
        DateTimeOffset occurredAtUtc) =>
        new(
            stakeholderId,
            tenantId,
            ipAddressId,
            ipAddressLocationId,
            userAgent,
            deviceName,
            devicePlatform,
            browserName,
            occurredAtUtc,
            LoginActivityType.InitialLogin);

    public static LoginActivity CreateTokenRefresh(
        Guid stakeholderId,
        Guid tenantId,
        Guid ipAddressId,
        Guid? ipAddressLocationId,
        string userAgent,
        string? deviceName,
        string? devicePlatform,
        string? browserName,
        DateTimeOffset occurredAtUtc) =>
        new(
            stakeholderId,
            tenantId,
            ipAddressId,
            ipAddressLocationId,
            userAgent,
            deviceName,
            devicePlatform,
            browserName,
            occurredAtUtc,
            LoginActivityType.TokenRefresh);

    private static string NormalizeUserAgent(string userAgent)
    {
        var normalized = userAgent.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("User agent is required.", nameof(userAgent));
        }

        if (normalized.Length > MaxUserAgentLength)
        {
            throw new ArgumentException($"User agent must not exceed {MaxUserAgentLength} characters.", nameof(userAgent));
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", nameof(value));
        }

        return normalized;
    }
}

public enum LoginActivityType
{
    InitialLogin = 0,
    TokenRefresh = 1
}
