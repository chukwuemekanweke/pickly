using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;

public sealed record CreateAvatarUploadCommand(
    string FileName,
    string ContentType,
    long ContentLength,
    ActorContext ActorContext);
