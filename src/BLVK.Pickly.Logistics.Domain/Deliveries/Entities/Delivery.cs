using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class Delivery : Entity, IAggregateRoot
{
    private const int MinTrackingIdLength = 8;
    private const int MaxTrackingIdLength = 40;
    private readonly List<DeliveryItem> _items = [];
    private readonly List<DeliveryStatusHistory> _statusHistory = [];

    private Delivery()
    {
    }

    private Delivery(
        Guid tenantId,
        Guid senderId,
        string trackingId,
        DeliveryType deliveryType,
        DeliveryOrderType orderType,
        DeliveryAddress pickupAddress,
        DeliveryAddress dropOffAddress,
        DeliveryRecipient recipient,
        DeliveryCharge charge,
        DateTimeOffset? scheduledPickupAtUtc,
        DateTimeOffset? dispatchEligibleAtUtc)
    {
        TenantId = tenantId;
        SenderId = senderId;
        TrackingId = trackingId;
        DeliveryType = deliveryType;
        OrderType = orderType;
        PickupAddress = pickupAddress;
        DropOffAddress = dropOffAddress;
        Recipient = recipient;
        Charge = charge;
        ScheduledPickupAtUtc = scheduledPickupAtUtc;
        DispatchEligibleAtUtc = dispatchEligibleAtUtc;
        PaymentStatus = DeliveryPaymentStatus.Pending;
        Status = GetInitialStatus(deliveryType, orderType);
    }

    public Guid TenantId { get; private set; }
    public Guid SenderId { get; private set; }
    public string TrackingId { get; private set; } = string.Empty;
    public DeliveryType DeliveryType { get; private set; }
    public DeliveryOrderType OrderType { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public DeliveryPaymentStatus PaymentStatus { get; private set; }
    public DeliveryAddress PickupAddress { get; private set; } = null!;
    public DeliveryAddress DropOffAddress { get; private set; } = null!;
    public DeliveryRecipient Recipient { get; private set; } = null!;
    public DeliveryCharge Charge { get; private set; } = null!;
    public DateTimeOffset? ScheduledPickupAtUtc { get; private set; }
    public DateTimeOffset? DispatchEligibleAtUtc { get; private set; }
    public Guid? RiderId { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
    public DateTimeOffset? PaymentSatisfiedAtUtc { get; private set; }
    public Guid? CashCollectionId { get; private set; }
    public Guid? ProofOfDeliveryId { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public IReadOnlyCollection<DeliveryItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<DeliveryStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public DeliveryPaymentResponsibleParty PaymentResponsibleParty =>
        OrderType == DeliveryOrderType.PayBeforeDelivery
            ? DeliveryPaymentResponsibleParty.Sender
            : DeliveryPaymentResponsibleParty.Recipient;

    public DeliveryPaymentMethod PaymentMethod =>
        OrderType == DeliveryOrderType.PayBeforeDelivery
            ? DeliveryPaymentMethod.Digital
            : DeliveryPaymentMethod.Cash;

    public bool IsMatchingEligible =>
        Status == DeliveryStatus.PendingAssignment &&
        RiderId is null &&
        (OrderType == DeliveryOrderType.PayOnDelivery || PaymentStatus == DeliveryPaymentStatus.Paid);

    public static Delivery Create(
        Guid tenantId,
        Guid senderId,
        string trackingId,
        DeliveryType deliveryType,
        DeliveryOrderType orderType,
        DeliveryAddress pickupAddress,
        DeliveryAddress dropOffAddress,
        DeliveryRecipient recipient,
        DeliveryCharge charge,
        IEnumerable<DeliveryItem> items,
        DateTimeOffset utcNow,
        DateTimeOffset? scheduledPickupAtUtc = null,
        DateTimeOffset? dispatchEligibleAtUtc = null)
    {
        EnsureRequiredId(tenantId, nameof(tenantId));
        EnsureRequiredId(senderId, nameof(senderId));
        ArgumentNullException.ThrowIfNull(pickupAddress);
        ArgumentNullException.ThrowIfNull(dropOffAddress);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(charge);
        ArgumentNullException.ThrowIfNull(items);

        if (!Enum.IsDefined(deliveryType))
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryType));
        }

        if (!Enum.IsDefined(orderType))
        {
            throw new ArgumentOutOfRangeException(nameof(orderType));
        }

        var normalizedTrackingId = NormalizeTrackingId(trackingId);
        var nowUtc = utcNow.ToUniversalTime();
        DateTimeOffset? pickupAtUtc = null;
        DateTimeOffset? eligibleAtUtc = null;

        if (deliveryType == DeliveryType.Scheduled)
        {
            if (scheduledPickupAtUtc is null)
            {
                throw new ArgumentException("Scheduled pickup time is required for a scheduled delivery.", nameof(scheduledPickupAtUtc));
            }

            pickupAtUtc = scheduledPickupAtUtc.Value.ToUniversalTime();
            if (pickupAtUtc <= nowUtc)
            {
                throw new ArgumentOutOfRangeException(nameof(scheduledPickupAtUtc), "Scheduled pickup time must be in the future.");
            }

            eligibleAtUtc = (dispatchEligibleAtUtc ?? scheduledPickupAtUtc).Value.ToUniversalTime();
            if (eligibleAtUtc < nowUtc || eligibleAtUtc > pickupAtUtc)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dispatchEligibleAtUtc),
                    "Dispatch eligibility must be between creation time and the scheduled pickup time.");
            }
        }
        else if (scheduledPickupAtUtc is not null || dispatchEligibleAtUtc is not null)
        {
            throw new ArgumentException("Instant deliveries cannot have scheduled dispatch timestamps.", nameof(scheduledPickupAtUtc));
        }

        var delivery = new Delivery(
            tenantId,
            senderId,
            normalizedTrackingId,
            deliveryType,
            orderType,
            pickupAddress,
            dropOffAddress,
            recipient,
            charge,
            pickupAtUtc,
            eligibleAtUtc);

        foreach (var item in items)
        {
            delivery.AddItemCore(item);
        }

        if (delivery._items.Count == 0)
        {
            throw new ArgumentException("A delivery must contain at least one parcel.", nameof(items));
        }

        delivery._statusHistory.Add(DeliveryStatusHistory.Create(
            delivery.Id,
            null,
            delivery.Status,
            "Delivery created.",
            nowUtc,
            senderId.ToString(),
            "delivery_creation"));

        return delivery;
    }

    public void AddItem(DeliveryItem item)
    {
        EnsureItemsCanChange();
        AddItemCore(item);
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureItemsCanChange();
        var item = _items.SingleOrDefault(candidate => candidate.Id == itemId)
            ?? throw new ArgumentException("Parcel does not belong to this delivery.", nameof(itemId));

        if (_items.Count == 1)
        {
            throw new AggregateStateException("A delivery must contain at least one parcel.");
        }

        _items.Remove(item);
    }

    public void MarkPrepaidPaymentProcessing()
    {
        EnsurePrepaidOrder();
        if (PaymentStatus != DeliveryPaymentStatus.Pending)
        {
            throw new AggregateStateException($"Payment cannot start processing from '{PaymentStatus}'.");
        }

        PaymentStatus = DeliveryPaymentStatus.Processing;
    }

    public void RecordPrepaidPaymentAttemptFailed()
    {
        EnsurePrepaidOrder();
        if (PaymentStatus != DeliveryPaymentStatus.Processing)
        {
            throw new AggregateStateException($"A payment attempt cannot fail from '{PaymentStatus}'.");
        }

        PaymentStatus = DeliveryPaymentStatus.Pending;
    }

    public void ConfirmVerifiedPrepaidPayment(
        Guid paymentTransactionId,
        decimal verifiedAmount,
        Guid currencyId,
        DateTimeOffset verifiedAtUtc,
        string actor,
        string source)
    {
        EnsurePrepaidOrder();
        EnsureRequiredId(paymentTransactionId, nameof(paymentTransactionId));
        ValidateFullPayment(verifiedAmount, currencyId);

        if (PaymentStatus == DeliveryPaymentStatus.Paid)
        {
            if (PaymentTransactionId == paymentTransactionId)
            {
                return;
            }

            throw new AggregateStateException("Delivery payment has already been satisfied by another transaction.");
        }

        if (PaymentStatus is not (DeliveryPaymentStatus.Pending or DeliveryPaymentStatus.Processing))
        {
            throw new AggregateStateException($"Verified payment cannot be recorded from '{PaymentStatus}'.");
        }

        if (Status != DeliveryStatus.AwaitingPayment)
        {
            throw new AggregateStateException($"Verified payment cannot activate a delivery in '{Status}'.");
        }

        PaymentTransactionId = paymentTransactionId;
        PaymentStatus = DeliveryPaymentStatus.Paid;
        PaymentSatisfiedAtUtc = verifiedAtUtc.ToUniversalTime();
        TransitionTo(
            DeliveryType == DeliveryType.Scheduled
                ? DeliveryStatus.AwaitingDispatch
                : DeliveryStatus.PendingAssignment,
            "Prepaid payment verified.",
            verifiedAtUtc,
            actor,
            source);
    }

    public void OpenDispatch(DateTimeOffset utcNow, string actor, string source)
    {
        if (Status != DeliveryStatus.AwaitingDispatch)
        {
            throw new AggregateStateException($"Dispatch cannot be opened from '{Status}'.");
        }

        if (DeliveryType != DeliveryType.Scheduled || DispatchEligibleAtUtc is null)
        {
            throw new AggregateStateException("Only a scheduled delivery can await dispatch.");
        }

        if (utcNow.ToUniversalTime() < DispatchEligibleAtUtc)
        {
            throw new AggregateStateException("Scheduled delivery is not yet eligible for dispatch.");
        }

        if (OrderType == DeliveryOrderType.PayBeforeDelivery && PaymentStatus != DeliveryPaymentStatus.Paid)
        {
            throw new AggregateStateException("Prepaid delivery requires verified payment before dispatch.");
        }

        TransitionTo(DeliveryStatus.PendingAssignment, "Dispatch eligibility reached.", utcNow, actor, source);
    }

    public void AssignRider(Guid riderId, DateTimeOffset assignedAtUtc, string actor, string source)
    {
        EnsureRequiredId(riderId, nameof(riderId));
        if (!IsMatchingEligible)
        {
            throw new AggregateStateException("Delivery is not eligible for rider assignment.");
        }

        RiderId = riderId;
        TransitionTo(DeliveryStatus.RiderAssigned, "Rider assigned.", assignedAtUtc, actor, source);
    }

    public void MarkPickedUp(DateTimeOffset pickedUpAtUtc, string actor, string source)
    {
        if (Status != DeliveryStatus.RiderAssigned || RiderId is null)
        {
            throw new AggregateStateException("Delivery must have an assigned rider before pickup.");
        }

        TransitionTo(DeliveryStatus.PickedUp, "Parcels picked up.", pickedUpAtUtc, actor, source);
    }

    public void MarkInTransit(DateTimeOffset inTransitAtUtc, string actor, string source)
    {
        if (Status != DeliveryStatus.PickedUp)
        {
            throw new AggregateStateException("Delivery must be picked up before transit begins.");
        }

        TransitionTo(DeliveryStatus.InTransit, "Delivery in transit.", inTransitAtUtc, actor, source);
    }

    public void ConfirmFullCashCollection(
        Guid cashCollectionId,
        decimal collectedAmount,
        Guid currencyId,
        DateTimeOffset collectedAtUtc)
    {
        if (OrderType != DeliveryOrderType.PayOnDelivery)
        {
            throw new AggregateStateException("Cash collection applies only to Pay On Delivery orders.");
        }

        if (Status != DeliveryStatus.InTransit)
        {
            throw new AggregateStateException("Cash collection can be confirmed only during handover.");
        }

        EnsureRequiredId(cashCollectionId, nameof(cashCollectionId));
        ValidateFullPayment(collectedAmount, currencyId);

        if (PaymentStatus == DeliveryPaymentStatus.Paid)
        {
            if (CashCollectionId == cashCollectionId)
            {
                return;
            }

            throw new AggregateStateException("Delivery cash obligation has already been satisfied.");
        }

        CashCollectionId = cashCollectionId;
        PaymentStatus = DeliveryPaymentStatus.Paid;
        PaymentSatisfiedAtUtc = collectedAtUtc.ToUniversalTime();
    }

    public void ConfirmProofOfDelivery(Guid proofOfDeliveryId)
    {
        if (Status != DeliveryStatus.InTransit)
        {
            throw new AggregateStateException("Proof of delivery can be confirmed only for an in-transit delivery.");
        }

        EnsureRequiredId(proofOfDeliveryId, nameof(proofOfDeliveryId));
        if (ProofOfDeliveryId is not null && ProofOfDeliveryId != proofOfDeliveryId)
        {
            throw new AggregateStateException("Proof of delivery has already been confirmed.");
        }

        ProofOfDeliveryId = proofOfDeliveryId;
    }

    public void Complete(DateTimeOffset deliveredAtUtc, string actor, string source)
    {
        if (Status != DeliveryStatus.InTransit || RiderId is null)
        {
            throw new AggregateStateException("Only an in-transit delivery with an assigned rider can be completed.");
        }

        if (ProofOfDeliveryId is null)
        {
            throw new AggregateStateException("Proof of delivery is required before completion.");
        }

        if (PaymentStatus != DeliveryPaymentStatus.Paid)
        {
            throw new AggregateStateException("The full delivery charge must be satisfied before completion.");
        }

        DeliveredAtUtc = deliveredAtUtc.ToUniversalTime();
        TransitionTo(DeliveryStatus.Delivered, "Delivery completed.", deliveredAtUtc, actor, source);
    }

    public void Cancel(string reason, DateTimeOffset cancelledAtUtc, string actor, string source)
    {
        if (Status is not (DeliveryStatus.AwaitingPayment or DeliveryStatus.AwaitingDispatch or DeliveryStatus.PendingAssignment or DeliveryStatus.RiderAssigned))
        {
            throw new AggregateStateException($"Delivery cannot be cancelled from '{Status}'.");
        }

        EnsureReason(reason);
        TransitionTo(DeliveryStatus.Cancelled, reason, cancelledAtUtc, actor, source);
    }

    public void MarkFailed(string reason, DateTimeOffset failedAtUtc, string actor, string source)
    {
        if (Status is not (DeliveryStatus.RiderAssigned or DeliveryStatus.PickedUp or DeliveryStatus.InTransit))
        {
            throw new AggregateStateException($"Delivery cannot fail from '{Status}'.");
        }

        EnsureReason(reason);
        TransitionTo(DeliveryStatus.DeliveryFailed, reason, failedAtUtc, actor, source);
    }

    private static DeliveryStatus GetInitialStatus(DeliveryType deliveryType, DeliveryOrderType orderType) =>
        (deliveryType, orderType) switch
        {
            (_, DeliveryOrderType.PayBeforeDelivery) => DeliveryStatus.AwaitingPayment,
            (DeliveryType.Instant, DeliveryOrderType.PayOnDelivery) => DeliveryStatus.PendingAssignment,
            (DeliveryType.Scheduled, DeliveryOrderType.PayOnDelivery) => DeliveryStatus.AwaitingDispatch,
            _ => throw new ArgumentOutOfRangeException(nameof(orderType))
        };

    private void AddItemCore(DeliveryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.Any(existing => existing.Id == item.Id))
        {
            throw new ArgumentException("A parcel with the same identifier already exists in the delivery.", nameof(item));
        }

        item.AttachTo(Id);
        _items.Add(item);
    }

    private void EnsureItemsCanChange()
    {
        if (Status is not (DeliveryStatus.AwaitingPayment or DeliveryStatus.AwaitingDispatch or DeliveryStatus.PendingAssignment))
        {
            throw new AggregateStateException("Parcels cannot change after fulfillment has begun.");
        }
    }

    private void EnsurePrepaidOrder()
    {
        if (OrderType != DeliveryOrderType.PayBeforeDelivery)
        {
            throw new AggregateStateException("Digital payment state applies only to Pay Before Delivery orders.");
        }
    }

    private void ValidateFullPayment(decimal amount, Guid currencyId)
    {
        if (currencyId != Charge.CurrencyId)
        {
            throw new AggregateStateException("Payment currency does not match the delivery charge.");
        }

        if (amount != Charge.Amount)
        {
            throw new AggregateStateException("The full delivery charge must be satisfied in a single payment.");
        }
    }

    private void TransitionTo(
        DeliveryStatus newStatus,
        string? reason,
        DateTimeOffset occurredAtUtc,
        string actor,
        string source)
    {
        if (Status is DeliveryStatus.Delivered or DeliveryStatus.DeliveryFailed or DeliveryStatus.Cancelled)
        {
            throw new AggregateStateException($"Delivery is already in terminal state '{Status}'.");
        }

        var previousStatus = Status;
        Status = newStatus;
        _statusHistory.Add(DeliveryStatusHistory.Create(
            Id,
            previousStatus,
            newStatus,
            reason,
            occurredAtUtc.ToUniversalTime(),
            actor,
            source));
    }

    private static string NormalizeTrackingId(string trackingId)
    {
        var normalized = trackingId?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is < MinTrackingIdLength or > MaxTrackingIdLength ||
            normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException(
                $"Tracking ID must be {MinTrackingIdLength}-{MaxTrackingIdLength} ASCII letters, digits, or hyphens.",
                nameof(trackingId));
        }

        return normalized;
    }

    private static void EnsureRequiredId(Guid id, string argumentName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", argumentName);
        }
    }

    private static void EnsureReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required.", nameof(reason));
        }
    }
}
