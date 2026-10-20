using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Domain.Riders.Specifications;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;

public sealed class AcceptRiderOfferHandler(
    RiderActorResolver actorResolver,
    IRepository<RiderDeliveryOffer> offers,
    IRiderTransactionFactory transactionFactory,
    IRiderConsistencyLock consistencyLock,
    IRiderOfferAcceptancePersistence persistence,
    IStakeholderReadModelRepository stakeholders,
    ICommandSender commandSender,
    IRiderMatchingTelemetry telemetry,
    IOptions<RiderMatchingOptions> options,
    IOptions<DeliveryNotificationOptions> notificationOptions,
    TimeProvider timeProvider)
{
    public async Task<AcceptRiderOfferResult> HandleAsync(
        AcceptRiderOfferCommand command,
        CancellationToken cancellationToken)
    {
        var resolved = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: false, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(resolved.Status == RiderActorResolutionStatus.NotAuthenticated
                ? AcceptRiderOfferStatus.NotAuthenticated
                : AcceptRiderOfferStatus.NotRider);
        }

        var preview = await offers.FirstOrDefaultAsync(
            new RiderDeliveryOfferByIdSpecification(command.OfferId),
            cancellationToken);
        if (preview is null)
        {
            return new(AcceptRiderOfferStatus.NotFound);
        }

        if (preview.RiderProfileId != resolved.Profile!.Id)
        {
            return new(AcceptRiderOfferStatus.Forbidden);
        }

        try
        {
            return await transactionFactory.ExecuteAsync(
                // Read committed: the advisory locks serialize rider operations and LoadAsync row-locks the Delivery
                // against writers outside them, so every read after locking is current and contention needs no retry.
                RiderTransactionIsolation.ReadCommitted,
                new(nameof(AcceptRiderOfferHandler), command.OfferId),
                async (transaction, token) =>
                {
                    await consistencyLock.AcquireAssignmentAsync(
                        preview.DeliveryId,
                        resolved.Profile.StakeholderId,
                        token);
                    var tracked = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: true, token);
                    if (tracked.Status != RiderActorResolutionStatus.Success)
                    {
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.NotRider);
                    }

                    var profile = tracked.Profile!;
                    var state = await persistence.LoadAsync(command.OfferId, profile.Id, token);
                    if (state.Offer is null || state.Delivery is null || state.MatchingSession is null)
                    {
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.NotFound);
                    }

                    if (state.Offer.RiderProfileId != profile.Id)
                    {
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.Forbidden);
                    }

                    if (state.Offer.Status == RiderDeliveryOfferStatus.Accepted &&
                        state.Delivery.RiderId == profile.StakeholderId)
                    {
                        await transaction.CommitAsync(token);
                        return new AcceptRiderOfferResult(
                            AcceptRiderOfferStatus.Success,
                            RiderDeliverySnapshotFactory.Create(state.Delivery));
                    }

                    if (state.Offer.Status != RiderDeliveryOfferStatus.Pending)
                    {
                        return new AcceptRiderOfferResult(state.Offer.Status == RiderDeliveryOfferStatus.Expired
                            ? AcceptRiderOfferStatus.Expired
                            : AcceptRiderOfferStatus.Conflict);
                    }

                    var now = timeProvider.GetUtcNow();
                    if (now >= state.Offer.ExpiresAtUtc)
                    {
                        RiderOfferRelease.Expire(state.Offer, state.MatchingSession, now, options.Value);
                        await transaction.CommitAsync(token);
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.Expired);
                    }

                    var geography = await persistence.GetGeographicEligibilityAsync(
                        profile,
                        state.Delivery,
                        state.Location,
                        state.MatchingSession.SearchRadiusMeters,
                        now,
                        token);
                    var hasFreshLocation = state.Location is not null &&
                        state.Location.CapturedAtUtc >= now.ToUniversalTime() - options.Value.LocationFreshness;
                    var isEligible = profile.IsApproved &&
                        profile.HasEnabledAvailability &&
                        hasFreshLocation &&
                        profile.VehicleCategoryId == state.Delivery.VehicleCategoryId &&
                        await persistence.IsVehicleCategoryActiveAsync(profile.VehicleCategoryId, token) &&
                        geography.IsEligible &&
                        state.Delivery.IsMatchingEligible &&
                        !await persistence.HasActiveDeliveryAsync(profile.StakeholderId, token);
                    if (!isEligible)
                    {
                        telemetry.RecordAssignmentConflict();
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.Conflict);
                    }

                    state.Offer.Accept(now);
                    state.Delivery.AssignRider(
                        profile.StakeholderId,
                        now,
                        profile.StakeholderId.ToString(),
                        "rider_offer_acceptance");
                    persistence.AddStatusHistory(state.Delivery.StatusHistory.Last());
                    state.MatchingSession.Complete(now);

                    var conflictingOffers = await persistence.ListConflictingOffersAsync(
                        state.Offer.Id,
                        state.Delivery.Id,
                        profile.Id,
                        token);
                    foreach (var conflictingOffer in conflictingOffers)
                    {
                        conflictingOffer.Revoke(now);
                    }

                    await EnqueueAssignmentNotificationsAsync(state.Delivery, profile.StakeholderId, now, token);
                    await transaction.CommitAsync(token);
                    telemetry.RecordOfferAccepted();
                    return new AcceptRiderOfferResult(
                        AcceptRiderOfferStatus.Success,
                        RiderDeliverySnapshotFactory.Create(state.Delivery));
                },
                cancellationToken);
        }
        catch (AggregateStateException)
        {
            telemetry.RecordAssignmentConflict();
            return new(AcceptRiderOfferStatus.Conflict);
        }
        catch (RiderPersistenceConflictException)
        {
            telemetry.RecordAssignmentConflict();
            return new(AcceptRiderOfferStatus.Conflict);
        }
        catch (RiderTransientPersistenceException)
        {
            return new(AcceptRiderOfferStatus.TemporarilyUnavailable);
        }
        catch (RiderCommitOutcomeUnknownException)
        {
            var acceptedDelivery = await persistence.FindAcceptedDeliveryAsync(
                command.OfferId,
                resolved.Profile.Id,
                resolved.Profile.StakeholderId,
                cancellationToken);
            if (acceptedDelivery is null)
            {
                throw;
            }

            telemetry.RecordOfferAccepted();
            return new(
                AcceptRiderOfferStatus.Success,
                RiderDeliverySnapshotFactory.Create(acceptedDelivery));
        }
    }

    private async Task EnqueueAssignmentNotificationsAsync(
        Delivery delivery,
        Guid riderStakeholderId,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken)
    {
        var sender = await stakeholders.GetByStakeholderIdAsync(delivery.SenderId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Sender '{delivery.SenderId}' was not found while preparing assignment notification.");
        var rider = await stakeholders.GetByStakeholderIdAsync(riderStakeholderId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Rider '{riderStakeholderId}' was not found while preparing assignment notification.");
        var deliveryUrl = DeliveryUrl(delivery);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                sender.TenantId,
                sender.CountryId,
                NotificationType.RiderAssigned,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    sender.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = sender.FirstName,
                        ["IllustrationUrl"] = NotificationIllustrationUrls.RiderAssigned,
                        ["TrackingId"] = delivery.TrackingId,
                        ["RiderName"] = $"{rider.FirstName} {rider.LastName}".Trim(),
                        ["PickupAddress"] = FormatAddress(delivery.PickupAddress),
                        ["DropOffAddress"] = FormatAddress(delivery.DropOffAddress),
                        ["CurrentStatus"] = "Rider assigned",
                        ["ScheduledPickup"] = FormatScheduledPickup(delivery),
                        ["NextSteps"] = "Your rider will proceed to the pickup location. Follow the delivery page for status updates.",
                        ["ActionUrl"] = deliveryUrl
                    }))
            {
                MessageId = NotificationMessageIdentity.Create(
                    delivery.Id,
                    NotificationType.RiderAssigned,
                    sender.StakeholderId),
                RequestedAt = requestedAt,
                StakeholderId = sender.StakeholderId,
                Language = sender.Language
            },
            cancellationToken);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                rider.TenantId,
                rider.CountryId,
                NotificationType.RiderAcceptedInstructions,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    rider.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = rider.FirstName,
                        ["IllustrationUrl"] = NotificationIllustrationUrls.RiderInstructions,
                        ["TrackingId"] = delivery.TrackingId,
                        ["PickupAddress"] = FormatAddress(delivery.PickupAddress),
                        ["PickupInstructions"] = OptionalText(delivery.PickupAddress.AdditionalInformation),
                        ["DropOffAddress"] = FormatAddress(delivery.DropOffAddress),
                        ["DropOffInstructions"] = OptionalText(delivery.DropOffAddress.AdditionalInformation),
                        ["RecipientName"] = delivery.Recipient.Name,
                        ["RecipientPhone"] = delivery.Recipient.PhoneNumber,
                        ["Items"] = string.Join("; ", delivery.Items.Select(item => $"{item.Description} x{item.Quantity}")),
                        ["ScheduledPickup"] = FormatScheduledPickup(delivery),
                        ["DeliveryInstructions"] = OptionalText(delivery.DropOffAddress.AdditionalInformation),
                        ["ActionUrl"] = deliveryUrl
                    }))
            {
                MessageId = NotificationMessageIdentity.Create(
                    delivery.Id,
                    NotificationType.RiderAcceptedInstructions,
                    rider.StakeholderId),
                RequestedAt = requestedAt,
                StakeholderId = rider.StakeholderId,
                Language = rider.Language
            },
            cancellationToken);
    }

    private string DeliveryUrl(Delivery delivery) =>
        new Uri(
            new Uri(notificationOptions.Value.ApplicationBaseUrl.TrimEnd('/') + "/"),
            $"deliveries/{Uri.EscapeDataString(delivery.TrackingId)}").AbsoluteUri;

    private string FormatScheduledPickup(Delivery delivery)
    {
        if (!delivery.ScheduledPickupAtUtc.HasValue)
        {
            return "As soon as possible";
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(notificationOptions.Value.DisplayTimeZoneId);
        return TimeZoneInfo.ConvertTime(delivery.ScheduledPickupAtUtc.Value, timeZone)
            .ToString("dd MMM yyyy, h:mm tt 'WAT'", System.Globalization.CultureInfo.GetCultureInfo("en-NG"));
    }

    private static string FormatAddress(DeliveryAddress address) =>
        string.Join(", ", new[] { address.AddressLine, address.City, address.State, address.CountryCode });

    private static string OptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "None provided" : value.Trim();
}
