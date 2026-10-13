using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Providers.Features.ActivateProvider;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Providers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Providers.Route)]
public sealed class ProvidersController(
    ActivateProviderHandler activateProviderHandler,
    ActivateProviderValidator validator) : ControllerBase
{
    [HttpPut("active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProvider(
        [FromBody] ActivateProviderRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        _ = Enum.TryParse(request.ProviderType, true, out Domain.Providers.Entities.ProviderType providerType);

        var result = await activateProviderHandler.HandleAsync(
            new ActivateProviderCommand(providerType, request.ProviderKey),
            cancellationToken);

        return result.Status switch
        {
            ActivateProviderStatus.ProviderNotFound => NotFound(result.Error ?? "Provider not found."),
            _ => NoContent()
        };
    }
}
