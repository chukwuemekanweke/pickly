namespace BLVK.Pickly.Logistics.Domain.Common.Storage;

public sealed class ObjectStoragePreconditionFailedException(string message, Exception? innerException = null)
    : Exception(message, innerException);
