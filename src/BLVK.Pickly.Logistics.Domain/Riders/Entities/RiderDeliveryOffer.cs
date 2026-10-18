using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Domain.Riders.Entities;

public sealed class RiderDeliveryOffer : Entity, IAggregateRoot
{
    private RiderDeliveryOffer()
    {
    }

    private RiderDeliveryOffer(
        Guid matchingSessionId,
        Guid deliveryId,
        Guid riderProfileId,
        DateTimeOffset offeredAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (matchingSessionId == Guid.Empty || deliveryId == Guid.Empty || riderProfileId == Guid.Empty)
        {
            throw new ArgumentException("Offer identifiers must be non-empty.");
        }

        OfferedAtUtc = offeredAtUtc.ToUniversalTime();
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        if (ExpiresAtUtc <= OfferedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));
        }

        MatchingSessionId = matchingSessionId;
        DeliveryId = deliveryId;
        RiderProfileId = riderProfileId;
        Status = RiderDeliveryOfferStatus.Pending;
    }

    public Guid MatchingSessionId { get; private set; }
    public Guid DeliveryId { get; private set; }
    public Guid RiderProfileId { get; private set; }
    public RiderDeliveryOfferStatus Status { get; private set; }
    public DateTimeOffset OfferedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RespondedAtUtc { get; private set; }

    public static RiderDeliveryOffer Create(
        Guid matchingSessionId,
        Guid deliveryId,
        Guid riderProfileId,
        DateTimeOffset offeredAtUtc,
        DateTimeOffset expiresAtUtc) =>
        new(matchingSessionId, deliveryId, riderProfileId, offeredAtUtc, expiresAtUtc);

    public void Accept(DateTimeOffset utcNow)
    {
        EnsurePendingAndUnexpired(utcNow);
        Status = RiderDeliveryOfferStatus.Accepted;
        RespondedAtUtc = utcNow.ToUniversalTime();
    }

    public void Reject(DateTimeOffset utcNow)
    {
        if (Status == RiderDeliveryOfferStatus.Rejected)
        {
            return;
        }

        EnsurePendingAndUnexpired(utcNow);
        Status = RiderDeliveryOfferStatus.Rejected;
        RespondedAtUtc = utcNow.ToUniversalTime();
    }

    public void Expire(DateTimeOffset utcNow)
    {
        if (Status != RiderDeliveryOfferStatus.Pending)
        {
            return;
        }

        Status = RiderDeliveryOfferStatus.Expired;
        RespondedAtUtc = utcNow.ToUniversalTime();
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        if (Status != RiderDeliveryOfferStatus.Pending)
        {
            return;
        }

        Status = RiderDeliveryOfferStatus.Revoked;
        RespondedAtUtc = utcNow.ToUniversalTime();
    }

    private void EnsurePendingAndUnexpired(DateTimeOffset utcNow)
    {
        if (Status != RiderDeliveryOfferStatus.Pending)
        {
            throw new AggregateStateException($"Offer is already '{Status}'.");
        }

        if (utcNow.ToUniversalTime() >= ExpiresAtUtc)
        {
            throw new AggregateStateException("Offer has expired.");
        }
    }
}
