using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Microsoft.FeatureManagement;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class RegistrationPolicyProvider(IFeatureManagerSnapshot featureManager)
    : IRegistrationPolicyProvider
{
    public async Task<RegistrationPolicy> GetAsync() =>
        RegistrationPolicy.Create(await featureManager.IsEnabledAsync(FeatureFlagNames.IsMvp));
}
