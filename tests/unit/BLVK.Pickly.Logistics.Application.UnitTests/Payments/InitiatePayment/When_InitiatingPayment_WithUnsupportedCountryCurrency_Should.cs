using BLVK.Pickly.Logistics.Application.Payments.Features.InitiatePayment;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.InitiatePayment;

public sealed class When_InitiatingPayment_WithUnsupportedCountryCurrency_Should
{
    [Fact]
    public async Task ThrowInvalidOperationException()
    {
        var context = new PaymentsFlowTestContext();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholder = context.CreateStakeholder(Guid.CreateVersion7(), tenantId, countryId);
        var currency = context.CreateCurrency("NGN");

        context.StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.CurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Domain.Payments.Entities.Currency>>(), Arg.Any<CancellationToken>())
            .Returns(currency);
        context.CountryCurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Domain.Payments.Entities.CountryCurrency>>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Payments.Entities.CountryCurrency?)null);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            context.CreateInitiatePaymentHandler().HandleAsync(
                new InitiatePaymentCommand(500m, currency.Id, PaymentIntent.WalletTopUp, Guid.CreateVersion7(), new ActorContext(stakeholder.Id, tenantId, Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
                CancellationToken.None));

        exception.Message.ShouldContain("not supported");
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
