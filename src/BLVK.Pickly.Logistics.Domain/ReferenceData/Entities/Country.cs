using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;

public sealed class Country : Entity, IAggregateRoot
{
    private Country()
    {
    }

    private Country(
        string name,
        string shortCode,
        string? callingCode,
        string flagUrl)
    {
        Name = name;
        ShortCode = shortCode;
        CallingCode = callingCode;
        FlagUrl = flagUrl;
    }

    public string Name { get; private set; } = string.Empty;
    public string ShortCode { get; private set; } = string.Empty;
    public string? CallingCode { get; private set; }
    public string FlagUrl { get; private set; } = string.Empty;

    public static Country Create(
        string name,
        string shortCode,
        string? callingCode,
        string flagUrl) =>
        new(name, shortCode, callingCode, flagUrl);
}
