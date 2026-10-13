namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdateProfile;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    ActorContext ActorContext);
