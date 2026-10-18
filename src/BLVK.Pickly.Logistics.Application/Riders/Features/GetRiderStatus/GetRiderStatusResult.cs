using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderStatus;

public sealed record GetRiderStatusResult(GetRiderStatusStatus Status, RiderStatusSnapshot? RiderStatus = null);
