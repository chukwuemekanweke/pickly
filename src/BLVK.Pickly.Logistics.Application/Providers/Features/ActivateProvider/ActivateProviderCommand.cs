using BLVK.Pickly.Logistics.Domain.Providers.Entities;

namespace BLVK.Pickly.Logistics.Application.Providers.Features.ActivateProvider;

public sealed record ActivateProviderCommand(ProviderType ProviderType, string ProviderKey);
