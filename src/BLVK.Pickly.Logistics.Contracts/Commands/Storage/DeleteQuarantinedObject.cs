namespace BLVK.Pickly.Logistics.Contracts.Commands.Storage;

public sealed record DeleteQuarantinedObject(
    Guid UploadId,
    string ObjectKey) : BaseCommand;
