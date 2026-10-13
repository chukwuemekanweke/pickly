using BLVK.Pickly.Logistics.WebAPI.Features.Payments.InitiatePayment;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.InitiatePayment;

public sealed class When_InitiatingPayment_WithInvalidRequest_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new PaymentsControllerTestContext();
        var sut = new PaymentsController(context.CreateInitiatePaymentHandler(), new InitiatePaymentValidator(), context.CurrentActor);

        var result = await sut.Handle(
            new InitiatePaymentRequest(0m, Guid.Empty, string.Empty, Guid.Empty),
            CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
