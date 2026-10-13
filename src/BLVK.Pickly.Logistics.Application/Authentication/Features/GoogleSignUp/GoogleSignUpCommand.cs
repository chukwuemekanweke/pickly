namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record GoogleSignUpCommand(
    string FlowToken,
    Guid CountryId,
    string FirstName,
    string LastName,
    string IpAddress,
    ActorContext ActorContext,
    string Language,
    string UserAgent = "");
