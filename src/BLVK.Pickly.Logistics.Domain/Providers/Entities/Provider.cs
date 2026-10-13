using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Providers.Entities;

public enum ProviderType
{
    Email = 1,
    FileStorage = 2
}

public sealed class Provider : Entity, IAggregateRoot
{
    private Provider()
    {
    }

    private Provider(ProviderType providerType, string providerName, string providerKey, bool isActive)
    {
        ProviderType = providerType;
        ProviderName = providerName.Trim();
        ProviderKey = providerKey.Trim();
        IsActive = isActive;
    }

    public ProviderType ProviderType { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static Provider Create(
        ProviderType providerType,
        string providerName,
        string providerKey,
        bool isActive) =>
        new(providerType, providerName, providerKey, isActive);

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
