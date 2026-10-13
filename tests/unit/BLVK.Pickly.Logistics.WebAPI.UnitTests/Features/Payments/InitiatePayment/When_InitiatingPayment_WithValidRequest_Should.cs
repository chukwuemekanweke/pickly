using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Payments.InitiatePayment;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.InitiatePayment;

public sealed class When_InitiatingPayment_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnPaymentInstructions()
    {
        var context = new PaymentsControllerTestContext();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholder = context.CreateStakeholder(Guid.CreateVersion7(), tenantId, countryId);
        var currency = context.CreateCurrency("NGN");
        var countryCurrency = context.CreateCountryCurrency(countryId, currency.Id);
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        var providerService = Substitute.For<IPaymentProviderService>();
        var request = new InitiatePaymentRequest(1250m, currency.Id, nameof(PaymentIntent.WalletTopUp), provider.Id);

        provider.SetConfiguration(currency.Id, PaymentIntent.WalletTopUp, PaymentMethodType.PaymentLink, true);
        context.CurrentActor.ActorId.Returns(stakeholder.Id.ToString());
        context.StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.CurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>())
            .Returns(currency);
        context.CountryCurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<CountryCurrency>>(), Arg.Any<CancellationToken>())
            .Returns(countryCurrency);
        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentProviderConfigurationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProviderConfiguration>>(), Arg.Any<CancellationToken>())
            .Returns(provider.Configurations.Single());
        providerService.ProviderKey.Returns(PaymentProviderKeys.Credo);
        providerService.InitiatePaymentAsync(Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderInitiationResult(
                "provider-ref",
                PaymentProviderKeys.Credo,
                PaymentMethodType.PaymentLink,
                context.Clock.GetUtcNow().AddMinutes(30),
                new Dictionary<string, string> { ["paymentLink"] = "https://pay.local" }));
        context.PaymentProviderServices.Add(providerService);

        var sut = new PaymentsController(context.CreateInitiatePaymentHandler(), new InitiatePaymentValidator(), context.CurrentActor);

        var result = await sut.Handle(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<InitiatePaymentResponse>();
        payload.PaymentStatus.ShouldBe(nameof(PaymentStatus.Initiated));
        payload.PaymentProviderId.ShouldBe(provider.Id);
        payload.PaymentProviderName.ShouldBe("Credo");
        payload.PaymentMethodType.ShouldBe(nameof(PaymentMethodType.PaymentLink));
        payload.PaymentInstruction["paymentLink"].ShouldBe("https://pay.local");
    }
}
