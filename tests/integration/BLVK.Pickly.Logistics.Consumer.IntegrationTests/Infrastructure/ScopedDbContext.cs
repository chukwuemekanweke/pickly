using Microsoft.Extensions.DependencyInjection;
using AppDbContext = BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;

public sealed class ScopedDbContext(IServiceScope scope) : IDisposable
{
    public AppDbContext DbContext { get; } = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    public void Dispose() => scope.Dispose();
}
