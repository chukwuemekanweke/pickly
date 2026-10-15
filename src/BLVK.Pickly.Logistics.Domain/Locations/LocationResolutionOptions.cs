namespace BLVK.Pickly.Logistics.Domain.Locations;

public sealed class LocationResolutionOptions
{
    public const string SectionName = "Locations:Resolution";

    /// <summary>ISO 3166-1 alpha-2 countries Pickly currently serves. Resolution outside them is refused.</summary>
    public IReadOnlyList<string> SupportedCountryCodes { get; init; } = ["NG"];

    public int MaxProviderCandidates { get; init; } = 5;
    public TimeSpan CandidateSelectionLifetime { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan ResolutionLeaseDuration { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan ResolutionLeaseWaitTimeout { get; init; } = TimeSpan.FromSeconds(8);
    public TimeSpan ResolutionLeasePollInterval { get; init; } = TimeSpan.FromMilliseconds(250);
    public int LandmarkFallbackRadiusMeters { get; init; } = 3000;
    public int LandmarkFallbackLimit { get; init; } = 10;
    public int MaxNearbyRadiusMeters { get; init; } = 10000;
    public int MaxResults { get; init; } = 25;

    public bool IsSupportedCountry(string countryCode) =>
        SupportedCountryCodes.Any(code => string.Equals(code, countryCode?.Trim(), StringComparison.OrdinalIgnoreCase));

    public void Validate()
    {
        if (SupportedCountryCodes.Count == 0 || SupportedCountryCodes.Any(code => code is not { Length: 2 }))
            throw new InvalidOperationException("Locations:Resolution:SupportedCountryCodes must contain two-letter ISO codes.");
        if (MaxProviderCandidates is < 1 or > 10)
            throw new InvalidOperationException("Locations:Resolution:MaxProviderCandidates must be between 1 and 10.");
        if (CandidateSelectionLifetime < TimeSpan.FromMinutes(1) || CandidateSelectionLifetime > TimeSpan.FromHours(1))
            throw new InvalidOperationException("Locations:Resolution:CandidateSelectionLifetime must be between one minute and one hour.");
        if (ResolutionLeaseDuration < TimeSpan.FromSeconds(5) || ResolutionLeaseDuration > TimeSpan.FromMinutes(2))
            throw new InvalidOperationException("Locations:Resolution:ResolutionLeaseDuration must be between 5 seconds and 2 minutes.");
        if (ResolutionLeaseWaitTimeout < TimeSpan.Zero || ResolutionLeaseWaitTimeout > TimeSpan.FromSeconds(30))
            throw new InvalidOperationException("Locations:Resolution:ResolutionLeaseWaitTimeout must be between zero and 30 seconds.");
        if (ResolutionLeasePollInterval < TimeSpan.FromMilliseconds(50) || ResolutionLeasePollInterval > TimeSpan.FromSeconds(5))
            throw new InvalidOperationException("Locations:Resolution:ResolutionLeasePollInterval must be between 50 milliseconds and 5 seconds.");
        if (MaxNearbyRadiusMeters is < 100 or > 50000)
            throw new InvalidOperationException("Locations:Resolution:MaxNearbyRadiusMeters must be between 100 and 50000.");
        if (LandmarkFallbackRadiusMeters < 100 || LandmarkFallbackRadiusMeters > MaxNearbyRadiusMeters)
            throw new InvalidOperationException("Locations:Resolution:LandmarkFallbackRadiusMeters must be between 100 and MaxNearbyRadiusMeters.");
        if (MaxResults is < 1 or > 100)
            throw new InvalidOperationException("Locations:Resolution:MaxResults must be between 1 and 100.");
        if (LandmarkFallbackLimit < 1 || LandmarkFallbackLimit > MaxResults)
            throw new InvalidOperationException("Locations:Resolution:LandmarkFallbackLimit must be between 1 and MaxResults.");
    }
}
