using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDeliveryQuote;

public sealed class CreateDeliveryQuoteHandler(DeliveryQuotePreparationService preparationService, IRepository<DeliveryQuote> quotes, IUnitOfWork unitOfWork, IOptions<DeliveryQuoteOptions> options, TimeProvider timeProvider, ICustomTelemetryContext telemetry)
{
    public async Task<CreateDeliveryQuoteResult> HandleAsync(CreateDeliveryQuoteCommand command, CancellationToken cancellationToken)
    {
        var prepared = await preparationService.PrepareAsync(command.ActorContext.StakeholderId, command.PickupLocationId, command.DropOffLocationId, command.DeliveryType, command.OrderType, command.ScheduledPickupAtUtc, command.Items, command.VehicleCategoryId, cancellationToken);
        if (prepared.Status != DeliveryQuotePreparationStatus.Success)
        {
            telemetry.AddCustomEvent(Observability.EventNames.Deliveries.QuoteCreationFailed, new() { [Observability.PropertyNames.Deliveries.Outcome] = prepared.Status.ToString() });
            return new(prepared.Status);
        }
        var selected = prepared.Options!.Single();
        var now = timeProvider.GetUtcNow();
        var quote = DeliveryQuote.Create(command.ActorContext.StakeholderId!.Value, prepared.PickupLocationId, prepared.DropOffLocationId, prepared.PickupAddress!, prepared.DropOffAddress!, command.DeliveryType, command.OrderType, command.ScheduledPickupAtUtc, selected.Vehicle, prepared.Zones!, selected.Policy, selected.Pricing, command.Items.Select(x => (x.Description, x.Quantity, x.WeightKg)), now, now + options.Value.ValidityDuration);
        await quotes.AddAsync(quote, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        telemetry.AddCustomEvent(Observability.EventNames.Deliveries.QuoteCreated);
        return new(DeliveryQuotePreparationStatus.Success, DeliveryQuoteResponse.From(quote, selected.CurrencyCode, now));
    }
}
