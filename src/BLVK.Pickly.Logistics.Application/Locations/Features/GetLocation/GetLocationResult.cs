namespace BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;

public sealed record GetLocationResult(
    GetLocationStatus Status,
    LocationResponse? Location = null);
