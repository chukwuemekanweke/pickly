using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryZone : Entity, IAggregateRoot
{
    private DeliveryZone() { }

    private DeliveryZone(string code, string name, string countryCode, int priority, DateTimeOffset effectiveFromUtc, DateTimeOffset? effectiveToUtc, bool isActive)
    {
        Code = Normalize(code, nameof(code), 50).ToUpperInvariant();
        Name = Normalize(name, nameof(name), 100);
        CountryCode = Normalize(countryCode, nameof(countryCode), 2).ToUpperInvariant();
        if (CountryCode.Length != 2) throw new ArgumentException("Country code must be a two-letter ISO code.", nameof(countryCode));
        if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority));
        EffectiveFromUtc = effectiveFromUtc.ToUniversalTime();
        EffectiveToUtc = effectiveToUtc?.ToUniversalTime();
        if (EffectiveToUtc <= EffectiveFromUtc) throw new ArgumentException("Zone effective end must be after its start.", nameof(effectiveToUtc));
        Priority = priority;
        IsActive = isActive;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public DateTimeOffset EffectiveFromUtc { get; private set; }
    public DateTimeOffset? EffectiveToUtc { get; private set; }
    public bool IsActive { get; private set; }

    public static DeliveryZone Create(string code, string name, string countryCode, int priority, DateTimeOffset effectiveFromUtc, DateTimeOffset? effectiveToUtc = null, bool isActive = true) =>
        new(code, name, countryCode, priority, effectiveFromUtc, effectiveToUtc, isActive);

    private static string Normalize(string value, string parameter, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maxLength) throw new ArgumentException($"Value is required and must not exceed {maxLength} characters.", parameter);
        return normalized;
    }
}
