using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Constants;
using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;
using BLVK.Pickly.Logistics.Application.Authentication.Features.LinkGoogleAccount;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.GoogleLinks;

[ApiController]
[ApiVersion("1.0")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.GoogleLinks.Route)]
public sealed class GoogleLinksController(
    LinkGoogleAccountHandler handler,
    IValidator<GoogleLinkRequest> validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicyNames.SignInPolicy)]
    [ProducesResponseType<GoogleSignInResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GoogleSignInResponse>> Handle(
        [FromBody] GoogleLinkRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(
            new LinkGoogleAccountCommand(
                request.FlowToken,
                request.Password,
                HttpContext.GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            LinkGoogleAccountStatus.Success => Ok(new GoogleSignInResponse(
                AuthenticationOutcomes.Authenticated,
                result.Tokens!.AccessToken.Value,
                result.Tokens.AccessToken.ExpiresAtUtc,
                result.Tokens.RefreshToken.Value,
                result.Tokens.RefreshToken.ExpiresAtUtc,
                "Bearer")),
            LinkGoogleAccountStatus.InvalidCredentials => AuthenticationProblemDetails.Create(
                StatusCodes.Status401Unauthorized, AuthenticationErrorCodes.InvalidCredentials,
                "Invalid credentials", "The supplied password is invalid."),
            LinkGoogleAccountStatus.AccountLocked => AuthenticationProblemDetails.Create(
                StatusCodes.Status423Locked, AuthenticationErrorCodes.AccountLocked, "Account locked",
                result.LockedUntilUtc.HasValue
                    ? $"The account is locked until {result.LockedUntilUtc.Value:O}."
                    : "The account is currently locked."),
            LinkGoogleAccountStatus.EmailVerificationRequired => AuthenticationProblemDetails.Create(
                StatusCodes.Status403Forbidden, AuthenticationErrorCodes.EmailVerificationRequired,
                "Email verification required", "Confirm the account email before signing in."),
            LinkGoogleAccountStatus.GoogleFlowExpired => AuthenticationProblemDetails.Create(
                StatusCodes.Status410Gone, AuthenticationErrorCodes.GoogleFlowExpired,
                "Google flow expired", "The Google authentication flow has expired."),
            LinkGoogleAccountStatus.GoogleFlowConsumed => AuthenticationProblemDetails.Create(
                StatusCodes.Status409Conflict, AuthenticationErrorCodes.GoogleFlowConsumed,
                "Google flow consumed", "The Google authentication flow has already been used."),
            LinkGoogleAccountStatus.GoogleAccountAlreadyLinked => AuthenticationProblemDetails.Create(
                StatusCodes.Status409Conflict, AuthenticationErrorCodes.GoogleAccountAlreadyLinked,
                "Google account already linked", "The Google account is linked to another user."),
            _ => AuthenticationProblemDetails.Create(
                StatusCodes.Status400BadRequest, AuthenticationErrorCodes.GoogleFlowInvalid,
                "Invalid Google flow", "The Google authentication flow is invalid.")
        };
    }
}
