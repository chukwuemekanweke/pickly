using System.Collections.Concurrent;

namespace BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;

public sealed class BackgroundServiceReadinessState(IEnumerable<BackgroundServiceDescriptor> descriptors)
{
    private readonly ConcurrentDictionary<string, bool> _states = new(
        descriptors
            .DistinctBy(descriptor => descriptor.Name)
            .ToDictionary(descriptor => descriptor.Name, _ => false));

    public bool IsReady => _states.Count > 0 && _states.All(entry => entry.Value);

    public IReadOnlyCollection<string> PendingServices =>
        _states
            .Where(entry => !entry.Value)
            .Select(entry => entry.Key)
            .ToArray();

    public void MarkReady(string serviceName)
    {
        if (!_states.ContainsKey(serviceName))
        {
            throw new InvalidOperationException(
                $"The background service '{serviceName}' was not registered for readiness tracking.");
        }

        _states[serviceName] = true;
    }
}
