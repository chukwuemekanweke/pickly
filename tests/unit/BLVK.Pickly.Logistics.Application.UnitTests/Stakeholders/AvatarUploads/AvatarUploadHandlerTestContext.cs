using BLVK.Pickly.Logistics.Application.Common.FileUploads;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

internal sealed class AvatarUploadHandlerTestContext
{
    public Guid StakeholderId { get; } = Guid.CreateVersion7();
    public Guid TenantId { get; } = Guid.CreateVersion7();
    public DateTimeOffset UtcNow { get; } = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IRepository<FileUploadSession> FileUploadSessionRepository { get; } = Substitute.For<IRepository<FileUploadSession>>();
    public IObjectStorageService ObjectStorageService { get; } = Substitute.For<IObjectStorageService>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public ICustomTelemetryContext Telemetry { get; } = Substitute.For<ICustomTelemetryContext>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    public Stakeholder Stakeholder { get; }

    public AvatarUploadHandlerTestContext()
    {
        Stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(),
            TenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe");
        StakeholderRepository.GetByIdAsync(StakeholderId, Arg.Any<CancellationToken>()).Returns(Stakeholder);
    }

    public ActorContext ActorContext() =>
        new(StakeholderId, TenantId, Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"));

    public FileUploadSession PendingUpload(
        string contentType = "image/png",
        long contentLength = 12,
        DateTimeOffset? expiresAtUtc = null)
    {
        var uploadId = Guid.CreateVersion7();
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".png"
        };
        return FileUploadSession.Create(
            uploadId,
            TenantId,
            "stakeholder",
            StakeholderId,
            StakeholderId,
            "stakeholder-profile-avatar",
            "stakeholder-avatar-v1",
            $"avatar{extension}",
            contentType,
            contentLength,
            extension,
            $"quarantine/{uploadId:N}{extension}",
            $"avatars/{uploadId:N}{extension}",
            ObjectStorageVisibility.Public,
            expiresAtUtc ?? UtcNow.AddMinutes(10));
    }

    public CreateAvatarUploadHandler CreateHandler() =>
        new(
            StakeholderRepository,
            FileUploadSessionRepository,
            new FileUploadService(ObjectStorageService),
            Telemetry,
            UnitOfWork,
            new FixedTimeProvider(UtcNow));

    public CompleteAvatarUploadHandler CompleteHandler() =>
        new(
            StakeholderRepository,
            FileUploadSessionRepository,
            new FileUploadService(ObjectStorageService),
            CommandSender,
            Telemetry,
            UnitOfWork,
            new FixedTimeProvider(UtcNow));

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
