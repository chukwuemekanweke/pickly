namespace BLVK.Pickly.Logistics.WebAPI.Features.Riders;

public sealed record RiderLocationRequest(
    decimal Latitude,
    decimal Longitude,
    DateTimeOffset CapturedAtUtc);
