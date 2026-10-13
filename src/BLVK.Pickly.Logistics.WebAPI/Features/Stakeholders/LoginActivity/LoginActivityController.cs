using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route($"{EndpointUrl.Stakeholders.Route}/me/login-activity")]
public sealed class LoginActivityController(
    GetLoginActivityHistoryHandler handler,
    GetLoginActivityValidator validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<LoginActivityHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginActivityHistoryResponse>> Handle(
        [FromQuery] GetLoginActivityRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        if (!Guid.TryParse(currentActor.ActorId, out _) ||
            !currentActor.TenantId.HasValue || currentActor.TenantId.Value == Guid.Empty)
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(
            new GetLoginActivityHistoryCommand(
                request.Limit,
                request.Cursor,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status == GetLoginActivityHistoryStatus.NotAuthenticated
            ? Unauthorized()
            : Ok(new LoginActivityHistoryResponse(result.Activities, result.NextCursor));
    }
}
