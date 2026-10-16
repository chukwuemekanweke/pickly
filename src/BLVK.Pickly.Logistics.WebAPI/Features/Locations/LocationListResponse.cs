namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed record LocationListResponse<TItem>(IReadOnlyList<TItem> Items);
