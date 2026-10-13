using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.LinkGoogleAccount;

public sealed record LinkGoogleAccountCommand(
    string FlowToken,
    string Password,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
