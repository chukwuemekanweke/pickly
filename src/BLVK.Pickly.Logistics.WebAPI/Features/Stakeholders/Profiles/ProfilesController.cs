using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdateProfile;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route($"{EndpointUrl.Stakeholders.Route}/me/profile")]
public sealed class ProfilesController(
    GetProfileHandler getProfileHandler,
    CreateAvatarUploadHandler createAvatarUploadHandler,
    CompleteAvatarUploadHandler completeAvatarUploadHandler,
    UpdateProfileHandler updateProfileHandler,
    ICurrentActor currentActor,
    ILogger<ProfilesController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<GetProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetProfileResponse>> GetProfile(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentActor.ActorId, out var stakeholderId))
        {
            logger.LogError(
                "Unable to resolve the authenticated stakeholder from ActorId {ActorId}.",
                currentActor.ActorId);
            return Unauthorized();
        }

        var actorContext = new ActorContext(
            stakeholderId,
            currentActor.TenantId,
            currentActor.CorrelationId,
            currentActor.FlowId);

        var result = await getProfileHandler.HandleAsync(
            new GetProfileQuery(actorContext),
            cancellationToken);

        return result.Status switch
        {
            GetProfileStatus.Success => Ok(result.Profile),
            GetProfileStatus.NotAuthenticated => Unauthorized(),
            _ => NotFound()
        };
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateProfileHandler.HandleAsync(
            new UpdateProfileCommand(request.FirstName, request.LastName, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            UpdateProfileStatus.NotAuthenticated => Unauthorized(),
            UpdateProfileStatus.StakeholderNotFound => NotFound(),
            UpdateProfileStatus.ValidationFailed => BadRequest(result.Error ?? "Invalid profile payload."),
            _ => NoContent()
        };
    }

    [HttpPost("avatar/uploads")]
    [ProducesResponseType<CreateAvatarUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateAvatarUploadResponse>> CreateAvatarUpload(
        [FromBody] CreateAvatarUploadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createAvatarUploadHandler.HandleAsync(
            new CreateAvatarUploadCommand(
                request.FileName,
                request.ContentType,
                request.ContentLength,
                ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            CreateAvatarUploadStatus.NotAuthenticated => Unauthorized(),
            CreateAvatarUploadStatus.StakeholderNotFound => NotFound(),
            CreateAvatarUploadStatus.InvalidFile => BadRequest(result.Error ?? "Invalid avatar file."),
            _ => Ok(new CreateAvatarUploadResponse(
                result.UploadId!.Value,
                result.UploadUrl!,
                "PUT",
                result.Headers!,
                result.ExpiresAtUtc!.Value))
        };
    }

    [HttpPost("avatar/uploads/{uploadId:guid}/complete")]
    [ProducesResponseType<CompleteAvatarUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompleteAvatarUploadResponse>> CompleteAvatarUpload(
        [FromRoute] Guid uploadId,
        CancellationToken cancellationToken)
    {
        var result = await completeAvatarUploadHandler.HandleAsync(
            new CompleteAvatarUploadCommand(uploadId, ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            CompleteAvatarUploadStatus.NotAuthenticated => Unauthorized(),
            CompleteAvatarUploadStatus.StakeholderNotFound => NotFound(),
            CompleteAvatarUploadStatus.UploadNotFound => NotFound(),
            CompleteAvatarUploadStatus.Expired => BadRequest(result.Error ?? "Avatar upload has expired."),
            CompleteAvatarUploadStatus.InvalidFile => BadRequest(result.Error ?? "Invalid avatar upload."),
            CompleteAvatarUploadStatus.UploadChanged => Conflict(result.Error ?? "Avatar upload changed during validation."),
            _ => Ok(new CompleteAvatarUploadResponse(result.AvatarUrl!))
        };
    }

    private ActorContext ResolveActorContext() =>
        Guid.TryParse(currentActor.ActorId, out var stakeholderId)
            ? new ActorContext(stakeholderId, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId)
            : ActorContext.FromAnonymousActor(currentActor);
}
