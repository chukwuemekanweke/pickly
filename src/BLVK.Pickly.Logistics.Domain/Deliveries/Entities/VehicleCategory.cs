using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class VehicleCategory : Entity, IAggregateRoot
{
    private VehicleCategory() { }

    private VehicleCategory(string code, string name, string? description, decimal maximumWeightKg, string routingProfile, bool isActive)
    {
        Code = NormalizeRequired(code, nameof(code), 50).ToUpperInvariant();
        Name = NormalizeRequired(name, nameof(name), 100);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (Description?.Length > 500) throw new ArgumentException("Description must not exceed 500 characters.", nameof(description));
        if (maximumWeightKg <= 0m || decimal.Round(maximumWeightKg, 3) != maximumWeightKg)
            throw new ArgumentOutOfRangeException(nameof(maximumWeightKg), "Maximum weight must be positive with at most three decimal places.");
        RoutingProfile = NormalizeRequired(routingProfile, nameof(routingProfile), 50).ToLowerInvariant();
        MaximumWeightKg = maximumWeightKg;
        IsActive = isActive;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal MaximumWeightKg { get; private set; }
    public string RoutingProfile { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static VehicleCategory Create(string code, string name, string? description, decimal maximumWeightKg, string routingProfile, bool isActive = true) =>
        new(code, name, description, maximumWeightKg, routingProfile, isActive);

    public bool CanCarry(decimal totalWeightKg) => IsActive && totalWeightKg > 0m && totalWeightKg <= MaximumWeightKg;

    private static string NormalizeRequired(string value, string parameter, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maxLength) throw new ArgumentException($"Value is required and must not exceed {maxLength} characters.", parameter);
        return normalized;
    }
}
