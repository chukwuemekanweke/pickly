using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Features.DisableTwoFactor;
using BLVK.Pickly.Logistics.Application.Authentication.Features.GetTwoFactorStatus;
using BLVK.Pickly.Logistics.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;
using BLVK.Pickly.Logistics.Application.Authentication.Features.SetupTwoFactor;
using BLVK.Pickly.Logistics.Application.Authentication.Features.VerifyTwoFactorEnrollment;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Security;

[ApiController]
[ApiVersion("1.0")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.AuthenticationSecurity.Route)]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Policy = AuthorizationPolicyNames.RequireActiveSession)]
public sealed class TwoFactorController(
    GetTwoFactorStatusHandler statusHandler,
    SetupTwoFactorHandler setupHandler,
    VerifyTwoFactorEnrollmentHandler enrollmentHandler,
    RegenerateTwoFactorRecoveryCodesHandler recoveryCodesHandler,
    DisableTwoFactorHandler disableHandler,
    IValidator<TwoFactorEnrollmentRequest> enrollmentValidator,
    IValidator<TwoFactorProofRequest> proofValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("two-factor")]
    public async Task<ActionResult<TwoFactorStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await statusHandler.HandleAsync(
            new GetTwoFactorStatusQuery(ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status == GetTwoFactorStatusStatus.Success
            ? Ok(new TwoFactorStatusResponse(result.Enabled, result.RecoveryCodesRemaining))
            : Unauthorized();
    }

    [HttpPost("two-factor/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> Setup(CancellationToken cancellationToken)
    {
        var result = await setupHandler.HandleAsync(
            new SetupTwoFactorCommand(ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            SetupTwoFactorStatus.Success => Ok(new TwoFactorSetupResponse(result.SharedKey!, result.AuthenticatorUri!)),
            SetupTwoFactorStatus.AlreadyEnabled => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Two-factor authentication already enabled"
            }),
            SetupTwoFactorStatus.NotAuthenticated => Unauthorized(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to set up two-factor authentication")
        };
    }

    [HttpPost("two-factor/verify")]
    public async Task<ActionResult<RecoveryCodesResponse>> VerifyEnrollment(
        [FromBody] TwoFactorEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await enrollmentValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        if (!TryGetCurrentSessionId(out var currentSessionId))
            return Unauthorized();

        var result = await enrollmentHandler.HandleAsync(
            new VerifyTwoFactorEnrollmentCommand(
                request.Code,
                currentSessionId,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            VerifyTwoFactorEnrollmentStatus.Success => Ok(new RecoveryCodesResponse(result.RecoveryCodes!)),
            VerifyTwoFactorEnrollmentStatus.InvalidCode => InvalidTwoFactorCode(),
            VerifyTwoFactorEnrollmentStatus.AlreadyEnabled => Conflict(),
            VerifyTwoFactorEnrollmentStatus.NotAuthenticated => Unauthorized(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to enable two-factor authentication")
        };
    }

    [HttpPost("two-factor/recovery-codes")]
    public async Task<ActionResult<RecoveryCodesResponse>> RegenerateRecoveryCodes(
        [FromBody] TwoFactorProofRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await proofValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        if (!TryGetCurrentSessionId(out var currentSessionId))
            return Unauthorized();
        var result = await recoveryCodesHandler.HandleAsync(
            new RegenerateTwoFactorRecoveryCodesCommand(
                ParseMethod(request.VerificationMethod),
                request.Code,
                currentSessionId,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            RegenerateTwoFactorRecoveryCodesStatus.Success => Ok(new RecoveryCodesResponse(result.RecoveryCodes!)),
            RegenerateTwoFactorRecoveryCodesStatus.InvalidProof => InvalidTwoFactorCode(),
            RegenerateTwoFactorRecoveryCodesStatus.NotEnabled => Conflict(),
            RegenerateTwoFactorRecoveryCodesStatus.NotAuthenticated => Unauthorized(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to regenerate recovery codes")
        };
    }

    [HttpPost("two-factor/disable")]
    public async Task<IActionResult> Disable(
        [FromBody] TwoFactorProofRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await proofValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        if (!TryGetCurrentSessionId(out var currentSessionId))
            return Unauthorized();
        var result = await disableHandler.HandleAsync(
            new DisableTwoFactorCommand(
                ParseMethod(request.VerificationMethod),
                request.Code,
                currentSessionId,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result switch
        {
            DisableTwoFactorResult.Success => NoContent(),
            DisableTwoFactorResult.InvalidProof => InvalidTwoFactorCode(),
            DisableTwoFactorResult.NotEnabled => Conflict(),
            DisableTwoFactorResult.NotAuthenticated => Unauthorized(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to disable two-factor authentication")
        };
    }

    private static TwoFactorVerificationMethod ParseMethod(string method) =>
        method == "authenticator"
            ? TwoFactorVerificationMethod.Authenticator
            : TwoFactorVerificationMethod.RecoveryCode;

    private static ObjectResult InvalidTwoFactorCode() =>
        AuthenticationProblemDetails.Create(
            StatusCodes.Status401Unauthorized,
            AuthenticationErrorCodes.InvalidTwoFactorCode,
            "Invalid two-factor code",
            "The supplied two-factor code is invalid.");

    private bool TryGetCurrentSessionId(out Guid sessionId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value ??
            User.FindFirst(ClaimTypes.Sid)?.Value, out sessionId);
}
