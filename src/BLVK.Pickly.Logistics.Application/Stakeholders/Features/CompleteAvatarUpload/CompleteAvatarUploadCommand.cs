using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;

public sealed record CompleteAvatarUploadCommand(Guid UploadId, ActorContext ActorContext);
