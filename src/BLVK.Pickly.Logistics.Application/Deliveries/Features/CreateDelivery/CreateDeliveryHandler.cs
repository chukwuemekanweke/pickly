using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;

public sealed class CreateDeliveryHandler(
    IRepository<DeliveryQuote> quotes,
    IRepository<Delivery> deliveries,
    IRepository<Currency> currencies,
    IDeliveryBookingPersistence bookingPersistence,
    IStakeholderReadModelRepository stakeholders,
    ICommandSender commandSender,
    IDeliveryTrackingIdGenerator trackingIdGenerator,
    IOptions<DeliveryDispatchOptions> options,
    IOptions<DeliveryNotificationOptions> notificationOptions,
    TimeProvider timeProvider)
{
    public async Task<CreateDeliveryResult> HandleAsync(
        CreateDeliveryCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.ActorContext.StakeholderId.HasValue || command.ActorContext.StakeholderId == Guid.Empty)
        {
            return new(CreateDeliveryStatus.NotAuthenticated);
        }

        if (command.QuoteId == Guid.Empty)
        {
            return new(CreateDeliveryStatus.InvalidRequest);
        }

        var senderId = command.ActorContext.StakeholderId.Value;
        var replay = await FindExistingAsync(senderId, command.QuoteId, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        DeliveryRecipient recipient;
        DeliveryAddress dropOffAddress;
        try
        {
            recipient = DeliveryRecipient.Create(command.RecipientName, command.RecipientPhoneNumber);
        }
        catch (ArgumentException)
        {
            return new(CreateDeliveryStatus.InvalidRequest);
        }

        var normalizedInstructions = string.IsNullOrWhiteSpace(command.DropOffInstructions)
            ? null
            : command.DropOffInstructions.Trim();

        var quote = await quotes.FirstOrDefaultAsync(
            new TrackedDeliveryQuoteByOwnerAndIdSpecification(senderId, command.QuoteId),
            cancellationToken);
        if (quote is null)
        {
            return new(CreateDeliveryStatus.NotFound);
        }

        var now = timeProvider.GetUtcNow();
        if (quote.ConsumptionStatus == DeliveryQuoteConsumptionStatus.Consumed)
        {
            return new(CreateDeliveryStatus.QuoteConsumed);
        }

        if (quote.IsExpired(now))
        {
            return new(CreateDeliveryStatus.QuoteExpired);
        }

        if (quote.DeliveryType == DeliveryType.Scheduled && quote.ScheduledPickupAtUtc <= now)
        {
            return new(CreateDeliveryStatus.ScheduledPickupExpired);
        }

        try
        {
            dropOffAddress = CopyAddress(
                quote.DropOffAddress,
                normalizedInstructions ?? quote.DropOffAddress.AdditionalInformation);
        }
        catch (ArgumentException)
        {
            return new(CreateDeliveryStatus.InvalidRequest);
        }

        DateTimeOffset? dispatchEligibleAtUtc = quote.DeliveryType == DeliveryType.Scheduled
            ? Max(now, quote.ScheduledPickupAtUtc!.Value - options.Value.LeadTime)
            : null;
        var delivery = Delivery.Create(
            senderId,
            quote.Id,
            quote.VehicleCategoryId,
            trackingIdGenerator.Generate(),
            quote.DeliveryType,
            quote.OrderType,
            CopyAddress(quote.PickupAddress, quote.PickupAddress.AdditionalInformation),
            dropOffAddress,
            recipient,
            DeliveryCharge.Create(quote.TotalAmount, quote.CurrencyId),
            quote.Items.Select(item => DeliveryItem.Create(item.Description, item.Quantity, item.WeightKg)),
            now,
            quote.ScheduledPickupAtUtc,
            dispatchEligibleAtUtc);

        // The aggregate identifier remains stable through any tracking-ID collision retry.
        quote.Consume(delivery.Id, now);

        var currency = await currencies.GetByIdAsync(quote.CurrencyId, cancellationToken);
        var sender = await stakeholders.GetByStakeholderIdAsync(senderId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Sender '{senderId}' was not found while preparing the delivery notification.");
        await commandSender.SendAsync(
            new SendNotificationCommand(
                sender.TenantId,
                sender.CountryId,
                NotificationType.OrderCreated,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    sender.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = sender.FirstName,
                        ["IllustrationUrl"] = NotificationIllustrationUrls.OrderCreated,
                        ["TrackingId"] = delivery.TrackingId,
                        ["PickupAddress"] = FormatAddress(delivery.PickupAddress),
                        ["DropOffAddress"] = FormatAddress(delivery.DropOffAddress),
                        ["Items"] = FormatItems(delivery),
                        ["DeliveryType"] = delivery.DeliveryType.ToString(),
                        ["ScheduledPickup"] = FormatScheduledPickup(delivery),
                        ["Amount"] = delivery.Charge.Amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        ["Currency"] = currency?.CurrencyCode ?? string.Empty,
                        ["CurrentStatus"] = delivery.Status.ToString(),
                        ["NextSteps"] = NextSteps(delivery),
                        ["ActionUrl"] = DeliveryUrl(delivery)
                    }))
            {
                MessageId = NotificationMessageIdentity.Create(
                    delivery.Id,
                    NotificationType.OrderCreated,
                    sender.StakeholderId),
                RequestedAt = now,
                StakeholderId = sender.StakeholderId,
                Language = sender.Language
            },
            cancellationToken);

        var commit = await bookingPersistence.CommitAsync(delivery, quote, cancellationToken);
        if (commit.Status == DeliveryBookingCommitStatus.Existing)
        {
            return await FindExistingAsync(senderId, command.QuoteId, cancellationToken)
                ?? new(CreateDeliveryStatus.PersistenceConflict);
        }

        if (commit.Status != DeliveryBookingCommitStatus.Created)
        {
            return new(CreateDeliveryStatus.PersistenceConflict);
        }

        return new(CreateDeliveryStatus.Success, DeliveryResponse.From(delivery, currency?.CurrencyCode ?? string.Empty));
    }

    private async Task<CreateDeliveryResult?> FindExistingAsync(
        Guid senderId,
        Guid quoteId,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveries.FirstOrDefaultAsync(
            new DeliveryByOwnerAndQuoteSpecification(senderId, quoteId), cancellationToken);
        if (delivery is null)
        {
            return null;
        }

        var currency = await currencies.GetByIdAsync(delivery.Charge.CurrencyId, cancellationToken);
        return new(
            CreateDeliveryStatus.Success,
            DeliveryResponse.From(delivery, currency?.CurrencyCode ?? string.Empty),
            IsReplay: true);
    }

    private static DeliveryAddress CopyAddress(DeliveryAddress source, string? additionalInformation) =>
        DeliveryAddress.Create(
            source.AddressLine,
            additionalInformation,
            source.City,
            source.State,
            source.CountryCode,
            source.Latitude,
            source.Longitude);

    private static DateTimeOffset Max(DateTimeOffset first, DateTimeOffset second) => first >= second ? first : second;

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

    private static string FormatItems(Delivery delivery) =>
        string.Join("; ", delivery.Items.Select(item => $"{item.Description} x{item.Quantity}"));

    private static string NextSteps(Delivery delivery) => delivery.Status switch
    {
        DeliveryStatus.AwaitingPayment => "Complete payment to make your delivery eligible for dispatch.",
        DeliveryStatus.AwaitingDispatch => "Rider matching will begin when the scheduled dispatch window opens.",
        DeliveryStatus.PendingAssignment => "Pikly is now looking for an eligible rider near the pickup location.",
        _ => "Open the delivery to see its current status and next steps."
    };
}
