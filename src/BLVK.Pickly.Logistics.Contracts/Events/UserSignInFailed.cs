namespace BLVK.Pickly.Logistics.Contracts.Events;

public sealed record UserSignInFailed(
    string EmailAddress,
    string IpAddress,
    string UserAgent,
    string FailureReason) : BaseEvent;
