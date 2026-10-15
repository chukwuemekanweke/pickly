namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

/// <summary>
/// Infrastructure-only coordination row backing <see cref="LocationResolutionLease"/>. It holds a hashed resolution
/// key, never the requested address.
/// </summary>
public sealed class LocationResolutionLeaseRecord
{
    public string ResolutionKey { get; set; } = string.Empty;
    public Guid LeaseId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
