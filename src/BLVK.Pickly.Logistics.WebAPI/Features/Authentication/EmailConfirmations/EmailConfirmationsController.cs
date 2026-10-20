using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUpOtp;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;

[ApiController]
[ApiVersion("1.0")]
[EnableRateLimiting(RateLimitingPolicyNames.EmailOperationsPolicy)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.EmailConfirmations.Route)]
public sealed class EmailConfirmationsController(
    SignUpOtpHandler handler,
    RequestEmailConfirmationOtpHandler requestEmailConfirmationOtpHandler,
    IValidator<SignUpOtpRequest> validator,
    IValidator<RequestEmailConfirmationOtpRequest> requestEmailConfirmationOtpValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SignUpOtpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SignUpOtpResponse>> Handle(
        [FromBody] SignUpOtpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var command = new SignUpOtpCommand(
            request.Email,
            request.Otp,
            HttpContext.GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            ActorContext.FromAnonymousActor(currentActor));

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Status switch
        {
            SignUpOtpStatus.Success => Ok(new SignUpOtpResponse(
                result.Tokens!.AccessToken.Value,
                result.Tokens.AccessToken.ExpiresAtUtc,
                result.Tokens.RefreshToken.Value,
                result.Tokens.RefreshToken.ExpiresAtUtc,
                "Bearer")),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Unable to confirm email",
                detail: "The confirmation request is invalid, expired, already completed, or unavailable.")
        };
    }

    [HttpPost("confirmation-code")]
    [ProducesResponseType<RequestEmailConfirmationOtpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestEmailConfirmationOtpResponse>> RequestCode(
        [FromBody] RequestEmailConfirmationOtpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await requestEmailConfirmationOtpValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await requestEmailConfirmationOtpHandler.HandleAsync(
            new RequestEmailConfirmationOtpCommand(request.Email, ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return Ok(new RequestEmailConfirmationOtpResponse(
            "If the account exists and still requires verification, an OTP will be sent when no active code exists.",
            result.RetryAtUtc));
    }
}
