using BLVK.Pickly.Logistics.Application.Common.FileUploads;
using BLVK.Pickly.Logistics.Application.Stakeholders.AvatarUploads;
using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;

public sealed class CreateAvatarUploadHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<FileUploadSession> fileUploadSessionRepository,
    FileUploadService fileUploadService,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CreateAvatarUploadResult> HandleAsync(
        CreateAvatarUploadCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.ActorContext.StakeholderId.HasValue || !command.ActorContext.TenantId.HasValue)
        {
            RecordFailure(command, null, ObservabilityFailureReasons.NotAuthenticated);
            return new CreateAvatarUploadResult(CreateAvatarUploadStatus.NotAuthenticated);
        }

        var stakeholderId = command.ActorContext.StakeholderId.Value;
        var tenantId = command.ActorContext.TenantId.Value;
        var preparation = fileUploadService.Prepare(
            new FileUploadPreparationRequest(
                tenantId,
                AvatarUploadOwnerTypes.Stakeholder,
                stakeholderId,
                command.FileName,
                command.ContentType,
                command.ContentLength),
            AvatarUploadPolicy.Instance,
            timeProvider.GetUtcNow());
        if (!preparation.IsValid)
        {
            RecordFailure(command, stakeholderId, ObservabilityFailureReasons.InvalidFile);
            return new CreateAvatarUploadResult(
                CreateAvatarUploadStatus.InvalidFile,
                Error: preparation.Error);
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken);
        if (stakeholder is null || stakeholder.TenantId != tenantId)
        {
            RecordFailure(command, stakeholderId, ObservabilityFailureReasons.StakeholderNotFound);
            return new CreateAvatarUploadResult(CreateAvatarUploadStatus.StakeholderNotFound);
        }

        var upload = FileUploadSession.Create(
            preparation.UploadId!.Value,
            stakeholder.TenantId,
            AvatarUploadOwnerTypes.Stakeholder,
            stakeholder.Id,
            stakeholder.Id,
            AvatarUploadPurposes.ProfileAvatar,
            AvatarUploadPolicy.Instance.Key,
            preparation.OriginalFileName!,
            preparation.ContentType!,
            preparation.ContentLength!.Value,
            preparation.FileExtension!,
            preparation.QuarantineObjectKey!,
            preparation.FinalObjectKey!,
            AvatarUploadPolicy.Instance.DestinationVisibility,
            preparation.ExpiresAtUtc!.Value);

        await fileUploadSessionRepository.AddAsync(upload, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var presignedUpload = await fileUploadService.CreatePresignedUploadAsync(preparation, cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadInitiated,
            CreateEventProperties(command, stakeholderId, upload.Id, preparation.ContentType!, command.ContentLength));

        return new CreateAvatarUploadResult(
            CreateAvatarUploadStatus.Success,
            upload.Id,
            presignedUpload.UploadUrl,
            presignedUpload.Headers,
            presignedUpload.ExpiresAtUtc);
    }

    private void RecordFailure(CreateAvatarUploadCommand command, Guid? stakeholderId, string reason)
    {
        customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, reason);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadFailed,
            ObservabilityEventProperties.Create(command.ActorContext, stakeholderId, reason));
    }

    private static Dictionary<string, string> CreateEventProperties(
        CreateAvatarUploadCommand command,
        Guid stakeholderId,
        Guid uploadId,
        string contentType,
        long contentLength) =>
        ObservabilityEventProperties.Create(
            command.ActorContext,
            stakeholderId,
            additionalProperties: new Dictionary<string, string>
            {
                [Observability.PropertyNames.Common.UploadId] = uploadId.ToString(),
                [Observability.PropertyNames.Common.ContentType] = contentType,
                [Observability.PropertyNames.Common.ContentLength] = contentLength.ToString()
            });
}
