namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public TokenBucketPolicyOptions AuthenticatedGlobalPolicy { get; init; } = new(30, 10, 10, 0);

    public SlidingWindowPolicyOptions AnonymousGlobalPolicy { get; init; } = new(30, 1, 3, 0);

    public FixedWindowPolicyOptions SignInPolicy { get; init; } = new(5, 2, 0);
    public FixedWindowPolicyOptions RefreshSessionPolicy { get; init; } = new(10, 5, 0);

    public FixedWindowPolicyOptions SignUpPolicy { get; init; } = new(5, 10, 0);

    public FixedWindowPolicyOptions EmailOperationsPolicy { get; init; } = new(5, 5, 0);

    public FixedWindowPolicyOptions PasswordResetPolicy { get; init; } = new(5, 15, 0);
    public FixedWindowPolicyOptions TwoFactorVerificationPolicy { get; init; } = new(5, 5, 0);
    public FixedWindowPolicyOptions LocationResolutionPolicy { get; init; } = new(20, 10, 0);

    public sealed record FixedWindowPolicyOptions(
        int PermitLimit,
        int WindowMinutes,
        int QueueLimit);

    public sealed record SlidingWindowPolicyOptions(
        int PermitLimit,
        int WindowMinutes,
        int SegmentsPerWindow,
        int QueueLimit);

    public sealed record TokenBucketPolicyOptions(
        int TokenLimit,
        int TokensPerPeriod,
        int ReplenishmentPeriodSeconds,
        int QueueLimit);
}
