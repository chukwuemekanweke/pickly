using BLVK.Pickly.Logistics.Application.Common.FileUploads;
using BLVK.Pickly.Logistics.Application.Stakeholders.AvatarUploads;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;
using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Specifications;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;

public sealed class CompleteAvatarUploadHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<FileUploadSession> fileUploadSessionRepository,
    FileUploadService fileUploadService,
    ICommandSender commandSender,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CompleteAvatarUploadResult> HandleAsync(
        CompleteAvatarUploadCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.ActorContext.StakeholderId.HasValue || !command.ActorContext.TenantId.HasValue)
        {
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.NotAuthenticated);
        }

        var stakeholderId = command.ActorContext.StakeholderId.Value;
        var tenantId = command.ActorContext.TenantId.Value;
        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken);
        if (stakeholder is null || stakeholder.TenantId != tenantId)
        {
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.StakeholderNotFound);
        }

        var upload = await fileUploadSessionRepository.FirstOrDefaultAsync(
            new FileUploadSessionByIdAndOwnerSpecification(
                command.UploadId,
                tenantId,
                AvatarUploadOwnerTypes.Stakeholder,
                stakeholderId,
                AvatarUploadPurposes.ProfileAvatar),
            cancellationToken);
        if (upload is null)
        {
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.UploadNotFound);
        }

        if (upload.Status == FileUploadStatus.Completed)
        {
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.Success, upload.FinalLocation);
        }

        if (upload.Status != FileUploadStatus.Pending)
        {
            return new CompleteAvatarUploadResult(
                upload.Status == FileUploadStatus.Expired
                    ? CompleteAvatarUploadStatus.Expired
                    : CompleteAvatarUploadStatus.InvalidFile);
        }

        if (!string.Equals(upload.PolicyKey, AvatarUploadPolicy.Instance.Key, StringComparison.Ordinal))
        {
            return await RejectAsync(upload, command, "invalid_policy", cancellationToken);
        }

        if (timeProvider.GetUtcNow() > upload.ExpiresAtUtc)
        {
            upload.MarkExpired();
            await QueueQuarantineDeletionAsync(upload, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.Expired, Error: "Avatar upload has expired.");
        }

        var completion = await fileUploadService.CompleteAsync(
            new FileUploadCompletionRequest(
                upload.QuarantineObjectKey,
                upload.FinalObjectKey,
                upload.ExpectedContentType,
                upload.ExpectedContentLength,
                upload.DestinationVisibility),
            AvatarUploadPolicy.Instance,
            cancellationToken);
        if (completion.Status == FileUploadCompletionStatus.ObjectChanged)
        {
            return await RejectChangedAsync(upload, command, cancellationToken);
        }
        if (completion.Status != FileUploadCompletionStatus.Success)
        {
            return await RejectAsync(upload, command, completion.FailureReason!, cancellationToken);
        }

        stakeholder.SetAvatarUrl(completion.FinalUrl!);
        upload.MarkCompleted(completion.FinalUrl!, completion.ValidatedETag!);
        await QueueQuarantineDeletionAsync(upload, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadCompleted,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                stakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Common.UploadId] = upload.Id.ToString()
                }));

        return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.Success, completion.FinalUrl);
    }

    private async Task<CompleteAvatarUploadResult> RejectAsync(
        FileUploadSession upload,
        CompleteAvatarUploadCommand command,
        string reason,
        CancellationToken cancellationToken)
    {
        upload.Reject(reason);
        await QueueQuarantineDeletionAsync(upload, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        RecordFailure(command, upload, reason);
        return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.InvalidFile, Error: "Avatar upload is invalid.");
    }

    private async Task<CompleteAvatarUploadResult> RejectChangedAsync(
        FileUploadSession upload,
        CompleteAvatarUploadCommand command,
        CancellationToken cancellationToken)
    {
        const string reason = "object_changed";
        upload.Reject(reason);
        await QueueQuarantineDeletionAsync(upload, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        RecordFailure(command, upload, reason);
        return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.UploadChanged, Error: "Avatar upload changed during validation.");
    }

    private Task QueueQuarantineDeletionAsync(FileUploadSession upload, CancellationToken cancellationToken) =>
        commandSender.SendAsync(
            new DeleteQuarantinedObject(upload.Id, upload.QuarantineObjectKey)
            {
                StakeholderId = upload.InitiatedByStakeholderId,
                TenantId = upload.TenantId
            },
            cancellationToken);

    private void RecordFailure(CompleteAvatarUploadCommand command, FileUploadSession upload, string reason)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadFailed,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                upload.InitiatedByStakeholderId,
                reason,
                new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Common.UploadId] = upload.Id.ToString()
                }));
    }
}
