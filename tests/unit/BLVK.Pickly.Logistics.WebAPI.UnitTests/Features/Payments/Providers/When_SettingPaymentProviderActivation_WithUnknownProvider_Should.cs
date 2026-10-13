using BLVK.Pickly.Logistics.WebAPI.Features.Payments.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.Providers;

public sealed class When_SettingPaymentProviderActivation_WithUnknownProvider_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new PaymentsControllerTestContext();
        var sut = new PaymentProvidersController(context.CreateActivatePaymentProviderHandler());

        var result = await sut.SetActivation(Guid.CreateVersion7(), new SetPaymentProviderActivationRequest(true), CancellationToken.None);

        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
