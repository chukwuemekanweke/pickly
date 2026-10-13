using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Payments.Features.ActivatePaymentProvider;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.Providers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.PaymentProviders.Route)]
public sealed class PaymentProvidersController(ActivatePaymentProviderHandler handler) : ControllerBase
{
    [HttpPut("{id:guid}/activation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActivation(
        [FromRoute] Guid id,
        [FromBody] SetPaymentProviderActivationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ActivatePaymentProviderCommand(id, request.IsActive),
            cancellationToken);

        return result.Status switch
        {
            ActivatePaymentProviderStatus.ProviderNotFound => NotFound(result.Error ?? "Payment provider not found."),
            _ => NoContent()
        };
    }
}
