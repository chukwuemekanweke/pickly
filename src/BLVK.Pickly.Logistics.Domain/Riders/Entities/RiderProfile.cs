using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Domain.Riders.Entities;

public sealed class RiderProfile : Entity, IAggregateRoot
{
    private RiderProfile()
    {
    }

    private RiderProfile(Guid stakeholderId, Guid tenantId, Guid vehicleCategoryId)
    {
        EnsureRequired(stakeholderId, nameof(stakeholderId));
        EnsureRequired(tenantId, nameof(tenantId));
        EnsureRequired(vehicleCategoryId, nameof(vehicleCategoryId));
        StakeholderId = stakeholderId;
        TenantId = tenantId;
        VehicleCategoryId = vehicleCategoryId;
        AvailabilityStatus = RiderAvailabilityStatus.Offline;
        VerificationStatus = RiderVerificationStatus.Pending;
    }

    public Guid StakeholderId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid VehicleCategoryId { get; private set; }
    public RiderAvailabilityStatus AvailabilityStatus { get; private set; }
    public RiderVerificationStatus VerificationStatus { get; private set; }

    public bool IsApproved => VerificationStatus == RiderVerificationStatus.Approved;
    public bool HasEnabledAvailability => AvailabilityStatus == RiderAvailabilityStatus.Available;

    public static RiderProfile Create(Guid stakeholderId, Guid tenantId, Guid vehicleCategoryId) =>
        new(stakeholderId, tenantId, vehicleCategoryId);

    public void Approve()
    {
        if (VerificationStatus == RiderVerificationStatus.Suspended)
        {
            throw new AggregateStateException("A suspended rider must be explicitly reinstated.");
        }

        VerificationStatus = RiderVerificationStatus.Approved;
    }

    public void Suspend()
    {
        VerificationStatus = RiderVerificationStatus.Suspended;
        AvailabilityStatus = RiderAvailabilityStatus.Offline;
    }

    public void Reinstate() => VerificationStatus = RiderVerificationStatus.Approved;

    public void SetAvailability(bool isAvailable)
    {
        if (isAvailable && !IsApproved)
        {
            throw new AggregateStateException("Only an approved rider can become available.");
        }

        AvailabilityStatus = isAvailable
            ? RiderAvailabilityStatus.Available
            : RiderAvailabilityStatus.Offline;
    }

    public void Pause()
    {
        if (!IsApproved)
        {
            throw new AggregateStateException("Only an approved rider can pause availability.");
        }

        AvailabilityStatus = RiderAvailabilityStatus.Paused;
    }

    public void ChangeVehicleCategory(Guid vehicleCategoryId)
    {
        EnsureRequired(vehicleCategoryId, nameof(vehicleCategoryId));
        VehicleCategoryId = vehicleCategoryId;
        AvailabilityStatus = RiderAvailabilityStatus.Offline;
    }

    private static void EnsureRequired(Guid value, string parameter)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty identifier is required.", parameter);
        }
    }
}
