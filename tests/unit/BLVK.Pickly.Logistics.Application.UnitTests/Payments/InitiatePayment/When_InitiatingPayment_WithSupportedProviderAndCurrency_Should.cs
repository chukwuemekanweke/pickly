using BLVK.Pickly.Logistics.Application.Payments.Features.InitiatePayment;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.InitiatePayment;

public sealed class When_InitiatingPayment_WithSupportedProviderAndCurrency_Should
{
    [Fact]
    public async Task ReturnPaymentInstructions()
    {
        var context = new PaymentsFlowTestContext();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var stakeholder = context.CreateStakeholder(userId, tenantId, countryId);
        var currency = context.CreateCurrency("NGN");
        var countryCurrency = context.CreateCountryCurrency(countryId, currency.Id);
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        provider.SetConfiguration(currency.Id, PaymentIntent.WalletTopUp, PaymentMethodType.PaymentLink, true);
        var providerConfiguration = provider.Configurations.Single();
        var paymentProviderService = Substitute.For<IPaymentProviderService>();
        PaymentTransaction? capturedTransaction = null;

        context.StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.CurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>())
            .Returns(currency);
        context.CountryCurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<CountryCurrency>>(), Arg.Any<CancellationToken>())
            .Returns(countryCurrency);
        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentProviderConfigurationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProviderConfiguration>>(), Arg.Any<CancellationToken>())
            .Returns(providerConfiguration);
        context.PaymentTransactionRepository.AddAsync(Arg.Do<PaymentTransaction>(transaction => capturedTransaction = transaction), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        paymentProviderService.ProviderKey.Returns(PaymentProviderKeys.Credo);
        paymentProviderService.InitiatePaymentAsync(Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderInitiationResult(
                "cr_provider_ref",
                PaymentProviderKeys.Credo,
                PaymentMethodType.PaymentLink,
                context.Clock.GetUtcNow().AddMinutes(30),
                new Dictionary<string, string> { ["paymentLink"] = "https://pay.local" }));
        context.PaymentProviderServices.Add(paymentProviderService);

        var result = await context.CreateInitiatePaymentHandler().HandleAsync(
            new InitiatePaymentCommand(1250m, currency.Id, PaymentIntent.WalletTopUp, provider.Id, new ActorContext(stakeholder.Id, tenantId, Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.PaymentStatus.ShouldBe(PaymentStatus.Initiated);
        result.PaymentProviderId.ShouldBe(provider.Id);
        result.PaymentProviderName.ShouldBe("Credo");
        result.PaymentMethodType.ShouldBe(PaymentMethodType.PaymentLink);
        result.InstructionFields["paymentLink"].ShouldBe("https://pay.local");
        capturedTransaction.ShouldNotBeNull();
        capturedTransaction.PaymentStatus.ShouldBe(PaymentStatus.Initiated);
        capturedTransaction.ProviderReference.ShouldBe("cr_provider_ref");
        capturedTransaction.PaymentMethodType.ShouldBe(PaymentMethodType.PaymentLink);
        capturedTransaction.ProviderPayloadMetadata["paymentLink"].ShouldBe("https://pay.local");
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
