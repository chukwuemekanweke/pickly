using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Features.CompletePasswordReset;
using BLVK.Pickly.Logistics.Application.Authentication.Features.RequestPasswordReset;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.PasswordResets;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitingPolicyNames.PasswordResetPolicy)]
[Route(EndpointUrl.PasswordResets.Route)]
public sealed class PasswordResetsController(
    RequestPasswordResetHandler handler,
    CompletePasswordResetHandler completePasswordResetHandler,
    IValidator<PasswordResetRequest> validator,
    IValidator<CompletePasswordResetRequest> completePasswordResetValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RequestPasswordResetResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestPasswordResetResponse>> Handle(
        [FromBody] PasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(new RequestPasswordResetCommand(request.Email, ActorContext.FromAnonymousActor(currentActor)), cancellationToken);

        return result.Status switch
        {
            RequestPasswordResetStatus.UserNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Email not found",
                detail: "No account could be found for the supplied email address."),
            _ => Accepted((string?)null, new RequestPasswordResetResponse(
                "If the account is eligible, a password reset OTP will be sent shortly."))
        };
    }

    [HttpPost("completions")]
    [ProducesResponseType<CompletePasswordResetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompletePasswordResetResponse>> Complete(
        [FromBody] CompletePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await completePasswordResetValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await completePasswordResetHandler.HandleAsync(
            new CompletePasswordResetCommand(
                request.Email,
                request.Otp,
                request.Password,
                request.ConfirmPassword,
                ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            CompletePasswordResetStatus.Success => Ok(new CompletePasswordResetResponse(
                "Password reset successful. You can now sign in with the new password.")),
            CompletePasswordResetStatus.UserNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Email not found",
                detail: "No account could be found for the supplied email address."),
            CompletePasswordResetStatus.ValidationFailed => BadRequest(
                new ValidationProblemDetails(
                    (result.ValidationErrors ?? new Dictionary<string, string[]>())
                    .ToDictionary(entry => entry.Key, entry => entry.Value))),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid OTP",
                detail: "The OTP is invalid, expired, or has already been consumed.")
        };
    }
}
