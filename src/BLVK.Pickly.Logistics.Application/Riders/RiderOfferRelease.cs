using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Application.Riders;

/// <summary>
/// Every offer that leaves Pending without an assignment returns its delivery to matching after the retry backoff,
/// whichever operation observed it. Callers must hold the delivery and rider locks.
/// </summary>
public static class RiderOfferRelease
{
    public static void Expire(
        RiderDeliveryOffer offer,
        DeliveryMatchingSession? session,
        DateTimeOffset utcNow,
        RiderMatchingOptions options)
    {
        offer.Expire(utcNow);
        BackOff(session, utcNow, options);
    }

    public static void Reject(
        RiderDeliveryOffer offer,
        DeliveryMatchingSession? session,
        DateTimeOffset utcNow,
        RiderMatchingOptions options)
    {
        offer.Reject(utcNow);
        BackOff(session, utcNow, options);
    }

    public static void Revoke(
        RiderDeliveryOffer offer,
        DeliveryMatchingSession? session,
        DateTimeOffset utcNow,
        RiderMatchingOptions options)
    {
        offer.Revoke(utcNow);
        BackOff(session, utcNow, options);
    }

    private static void BackOff(DeliveryMatchingSession? session, DateTimeOffset utcNow, RiderMatchingOptions options)
    {
        if (session?.Status == DeliveryMatchingSessionStatus.Active)
        {
            session.RecordAttempt(utcNow, utcNow + options.RetryBackoff, session.SearchRadiusMeters);
        }
    }
}
