namespace BLVK.Pickly.Logistics.Domain.Authentication.Persistence;

public interface IDeviceAccountLock
{
    Task AcquireAsync(string identifier, CancellationToken cancellationToken = default);
}
