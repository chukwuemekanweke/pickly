using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Features.CheckEmailExistence;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailExistenceChecks;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitingPolicyNames.EmailOperationsPolicy)]
[Route(EndpointUrl.EmailExistenceChecks.Route)]
public sealed class EmailExistenceChecksController(
    CheckEmailExistenceHandler handler,
    IValidator<EmailExistenceCheckRequest> validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CheckEmailExistenceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CheckEmailExistenceResponse>> Handle(
        [FromBody] EmailExistenceCheckRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(
            new CheckEmailExistenceCommand(request.Email),
            cancellationToken);

        return Ok(new CheckEmailExistenceResponse(result.Exists));
    }
}
