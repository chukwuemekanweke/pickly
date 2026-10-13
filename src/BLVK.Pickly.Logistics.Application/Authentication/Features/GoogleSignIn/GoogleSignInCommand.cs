namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record GoogleSignInCommand(
    string IdToken,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext,
    string FlowToken = "");
