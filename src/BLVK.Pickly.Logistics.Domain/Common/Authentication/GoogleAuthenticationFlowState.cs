namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public enum GoogleAuthenticationFlowState
{
    Initiated = 1,
    LinkRequired = 2,
    RegistrationRequired = 3
}
