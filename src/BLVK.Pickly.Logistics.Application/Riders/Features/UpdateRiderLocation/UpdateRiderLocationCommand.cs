using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;

public sealed record UpdateRiderLocationCommand(
    decimal Latitude,
    decimal Longitude,
    DateTimeOffset CapturedAtUtc,
    ActorContext ActorContext);
