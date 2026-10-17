using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryQuote;

public sealed class GetDeliveryQuoteHandler(IRepository<DeliveryQuote> quotes, IRepository<Currency> currencies, TimeProvider timeProvider)
{
    public async Task<GetDeliveryQuoteResult> HandleAsync(GetDeliveryQuoteQuery query, CancellationToken cancellationToken)
    {
        if (!query.ActorContext.StakeholderId.HasValue || query.ActorContext.StakeholderId == Guid.Empty) return new(GetDeliveryQuoteStatus.NotAuthenticated);
        var quote = await quotes.FirstOrDefaultAsync(new DeliveryQuoteByOwnerAndIdSpecification(query.ActorContext.StakeholderId.Value, query.QuoteId), cancellationToken);
        if (quote is null) return new(GetDeliveryQuoteStatus.NotFound);
        var currency = await currencies.GetByIdAsync(quote.CurrencyId, cancellationToken);
        return new(GetDeliveryQuoteStatus.Success, DeliveryQuoteResponse.From(quote, currency?.CurrencyCode ?? "", timeProvider.GetUtcNow()));
    }
}
