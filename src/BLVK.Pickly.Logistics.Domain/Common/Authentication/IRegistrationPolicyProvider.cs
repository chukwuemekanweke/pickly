namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public interface IRegistrationPolicyProvider
{
    Task<RegistrationPolicy> GetAsync();
}
