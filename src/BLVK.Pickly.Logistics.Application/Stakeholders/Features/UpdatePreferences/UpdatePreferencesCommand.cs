using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdatePreferences;

public sealed record UpdatePreferencesCommand(
    string Theme,
    string Language,
    ActorContext ActorContext);
