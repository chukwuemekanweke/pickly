using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

public sealed class Tenant : Entity, IAggregateRoot
{
    private Tenant()
    {
    }

    private Tenant(
        Guid id,
        string name,
        string brandKey)
    {
        Id = id;
        Name = name.Trim();
        BrandKey = brandKey.Trim().ToLowerInvariant();
    }

    public string Name { get; private set; } = string.Empty;
    public string BrandKey { get; private set; } = string.Empty;

    public static Tenant Create(
        Guid id,
        string name,
        string brandKey) =>
        new(id, name, brandKey);
}
