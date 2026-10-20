using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Constants;
using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;
using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;

[ApiController]
[ApiVersion("1.0")]
[EnableRateLimiting(RateLimitingPolicyNames.SignUpPolicy)]
[Route(EndpointUrl.Registrations.Route)]
public sealed class RegistrationsController(
    SignUpHandler handler,
    GoogleSignUpHandler googleSignUpHandler,
    IValidator<SignUpRequest> validator,
    IValidator<GoogleSignUpRequest> googleSignUpValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SignUpResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SignUpResponse>> Handle(
        [FromBody] SignUpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var command = new SignUpCommand(
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.CountryId,
            request.FirstName,
            request.LastName,
            HttpContext.GetClientIpAddress(),
            ActorContext.FromAnonymousActor(currentActor),
            request.Language);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Status switch
        {
            SignUpStatus.DuplicateEmail => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already exists",
                detail: "An account with this email address already exists."),
            SignUpStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(result.ValidationErrors ?? new Dictionary<string, string[]>()))),
            SignUpStatus.CountryMismatch => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Country does not match request location",
                detail: "The selected country does not match the country resolved from the request IP address."),
            _ => Accepted((string?)null, new SignUpResponse(
                request.Email,
                "The sign-up request has been accepted. The account verification OTP will be sent shortly.",
                result.RetryAtUtc!.Value))
        };
    }

    [HttpPost("google")]
    [ProducesResponseType<GoogleSignUpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GoogleSignUpResponse>> HandleGoogle(
        [FromBody] GoogleSignUpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await googleSignUpValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await googleSignUpHandler.HandleAsync(
            new GoogleSignUpCommand(
                request.FlowToken,
                request.CountryId,
                request.FirstName,
                request.LastName,
                HttpContext.GetClientIpAddress(),
                ActorContext.FromAnonymousActor(currentActor),
                request.Language,
                HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty),
            cancellationToken);

        return result.Status switch
        {
            GoogleSignUpStatus.DuplicateEmail => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already exists",
                detail: "An account with this email address already exists."),
            GoogleSignUpStatus.DuplicateGoogleAccount => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Google account already linked",
                detail: "This Google account is already linked to another user."),
            GoogleSignUpStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(result.ValidationErrors ?? new Dictionary<string, string[]>()))),
            GoogleSignUpStatus.CountryMismatch => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Country does not match request location",
                detail: "The selected country does not match the country resolved from the request IP address."),
            GoogleSignUpStatus.GoogleFlowInvalid => AuthenticationProblemDetails.Create(
                StatusCodes.Status400BadRequest, AuthenticationErrorCodes.GoogleFlowInvalid,
                "Invalid Google flow", "The Google authentication flow is invalid."),
            GoogleSignUpStatus.GoogleFlowExpired => AuthenticationProblemDetails.Create(
                StatusCodes.Status410Gone, AuthenticationErrorCodes.GoogleFlowExpired,
                "Google flow expired", "The Google authentication flow has expired."),
            GoogleSignUpStatus.GoogleFlowConsumed => AuthenticationProblemDetails.Create(
                StatusCodes.Status409Conflict, AuthenticationErrorCodes.GoogleFlowConsumed,
                "Google flow consumed", "The Google authentication flow has already been used."),
            GoogleSignUpStatus.EmailVerificationRequired => AuthenticationProblemDetails.Create(
                StatusCodes.Status403Forbidden, AuthenticationErrorCodes.EmailVerificationRequired,
                "Email verification required", "Confirm the account email before signing in.",
                new Dictionary<string, object?>
                {
                    ["email"] = result.Email
                        ?? throw new InvalidOperationException("Email is required for the email-verification continuation."),
                    ["retryAtUtc"] = result.RetryAtUtc
                        ?? throw new InvalidOperationException("Retry time is required for the email-verification continuation.")
                }),
            _ => Ok(new GoogleSignUpResponse(
                AuthenticationOutcomes.Authenticated,
                result.Email ?? string.Empty,
                result.Tokens!.AccessToken.Value,
                result.Tokens.AccessToken.ExpiresAtUtc,
                result.Tokens.RefreshToken.Value,
                result.Tokens.RefreshToken.ExpiresAtUtc,
                "Bearer"))
        };
    }
}
