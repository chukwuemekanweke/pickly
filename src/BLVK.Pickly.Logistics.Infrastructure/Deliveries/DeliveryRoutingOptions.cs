namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

public sealed class DeliveryRoutingOptions
{
    public const string SectionName = "Deliveries:Routing:Mapbox";
    public string BaseUrl { get; init; } = "https://api.mapbox.com/";
    public string AccessToken { get; init; } = string.Empty;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(8);
    public int MaxRetries { get; init; } = 2;
    public TimeSpan MaximumCacheAge { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan CoordinationLeaseDuration { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan CoordinationWaitTimeout { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan CoordinationPollInterval { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan DefaultRateLimitRetryAfter { get; init; } = TimeSpan.FromSeconds(30);
    public string ServiceAreaTimeZone { get; init; } = "Africa/Lagos";
    public string ScheduleVersion { get; init; } = "v1";
    public IReadOnlyList<RoutingWindowOptions> Windows { get; init; } = [];

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("Deliveries:Routing:Mapbox:BaseUrl must be an absolute HTTPS URI.");
        if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromSeconds(30)) throw new InvalidOperationException("Routing timeout must be between zero and 30 seconds.");
        if (MaxRetries is < 0 or > 3) throw new InvalidOperationException("Routing retries must be between zero and three.");
        if (MaximumCacheAge <= TimeSpan.Zero || MaximumCacheAge > TimeSpan.FromHours(24)) throw new InvalidOperationException("Routing cache age must be greater than zero and no more than 24 hours.");
        if (CoordinationLeaseDuration <= TimeSpan.Zero || CoordinationWaitTimeout <= TimeSpan.Zero || CoordinationPollInterval <= TimeSpan.Zero) throw new InvalidOperationException("Routing coordination durations must be positive.");
        if (string.IsNullOrWhiteSpace(ScheduleVersion)) throw new InvalidOperationException("A routing schedule version is required.");
        _ = TimeZoneInfo.FindSystemTimeZoneById(ServiceAreaTimeZone);
        ValidateWindows();
    }

    private void ValidateWindows()
    {
        if (Windows.Count == 0 || Windows.Any(x => string.IsNullOrWhiteSpace(x.Id) || x.Start == x.End || x.ApplicableDays.Count == 0)) throw new InvalidOperationException("Routing windows must have an id, distinct start/end times and applicable days.");
        if (Windows.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != Windows.Count) throw new InvalidOperationException("Routing window ids must be unique.");
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var previous = day == DayOfWeek.Sunday ? DayOfWeek.Saturday : (DayOfWeek)((int)day - 1);
            var ranges = Windows.SelectMany(window => window.RangesFor(day, previous))
                .Where(x => x.End > x.Start)
                .OrderBy(x => x.Start)
                .ToArray();
            var cursor = TimeSpan.Zero;
            foreach (var range in ranges)
            {
                if (range.Start != cursor) throw new InvalidOperationException($"Routing windows overlap or leave missing coverage on {day}.");
                cursor = range.End;
            }
            if (cursor != TimeSpan.FromDays(1)) throw new InvalidOperationException($"Routing windows do not cover all of {day}.");
        }
    }

}

public sealed class RoutingWindowOptions
{
    public string Id { get; init; } = string.Empty;
    public TimeSpan Start { get; init; }
    public TimeSpan End { get; init; }
    public IReadOnlyList<DayOfWeek> ApplicableDays { get; init; } = [];

    internal IEnumerable<(TimeSpan Start, TimeSpan End)> RangesFor(DayOfWeek day, DayOfWeek previous)
    {
        if (Start < End && ApplicableDays.Contains(day)) yield return (Start, End);
        if (Start > End && ApplicableDays.Contains(day)) yield return (Start, TimeSpan.FromDays(1));
        if (Start > End && ApplicableDays.Contains(previous)) yield return (TimeSpan.Zero, End);
    }
}
