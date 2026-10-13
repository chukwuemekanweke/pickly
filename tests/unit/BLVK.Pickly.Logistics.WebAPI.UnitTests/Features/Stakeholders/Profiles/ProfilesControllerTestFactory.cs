using BLVK.Pickly.Logistics.Application.Common.FileUploads;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdateProfile;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles;

internal static class ProfilesControllerTestFactory
{
    public static ProfilesController Create(
        IStakeholderReadModelRepository stakeholderReadModelRepository,
        IRepository<Stakeholder> stakeholderRepository,
        ICustomTelemetryContext customTelemetryContext,
        IUnitOfWork unitOfWork,
        ICurrentActor currentActor,
        IObjectStorageService? objectStorageService = null,
        IRepository<FileUploadSession>? fileUploadSessionRepository = null,
        ILogger<ProfilesController>? logger = null)
    {
        objectStorageService ??= Substitute.For<IObjectStorageService>();
        fileUploadSessionRepository ??= Substitute.For<IRepository<FileUploadSession>>();

        return new ProfilesController(
            new GetProfileHandler(stakeholderReadModelRepository),
            new CreateAvatarUploadHandler(
                stakeholderRepository,
                fileUploadSessionRepository,
                new FileUploadService(objectStorageService),
                customTelemetryContext,
                unitOfWork,
                TimeProvider.System),
            new CompleteAvatarUploadHandler(
                stakeholderRepository,
                fileUploadSessionRepository,
                new FileUploadService(objectStorageService),
                Substitute.For<ICommandSender>(),
                customTelemetryContext,
                unitOfWork,
                TimeProvider.System),
            new UpdateProfileHandler(stakeholderRepository, customTelemetryContext, unitOfWork),
            currentActor,
            logger ?? NullLogger<ProfilesController>.Instance);
    }
}
