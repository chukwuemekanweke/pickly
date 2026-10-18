using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRequestRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        Validate(options.AuthenticatedGlobalPolicy, nameof(RateLimitingOptions.AuthenticatedGlobalPolicy));
        Validate(options.AnonymousGlobalPolicy, nameof(RateLimitingOptions.AnonymousGlobalPolicy));
        Validate(options.SignInPolicy, nameof(RateLimitingOptions.SignInPolicy));
        Validate(options.RefreshSessionPolicy, nameof(RateLimitingOptions.RefreshSessionPolicy));
        Validate(options.SignUpPolicy, nameof(RateLimitingOptions.SignUpPolicy));
        Validate(options.EmailOperationsPolicy, nameof(RateLimitingOptions.EmailOperationsPolicy));
        Validate(options.PasswordResetPolicy, nameof(RateLimitingOptions.PasswordResetPolicy));
        Validate(options.TwoFactorVerificationPolicy, nameof(RateLimitingOptions.TwoFactorVerificationPolicy));
        Validate(options.LocationResolutionPolicy, nameof(RateLimitingOptions.LocationResolutionPolicy));
        Validate(options.RiderLocationPolicy, nameof(RateLimitingOptions.RiderLocationPolicy));

        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.GlobalLimiter = CreateGlobalLimiter(options);

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.SignInPolicy,
                context => CreateFixedWindowPartition(options.SignInPolicy, ResolveClientIpPartitionKey(context)));

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.RefreshSessionPolicy,
                context => CreateFixedWindowPartition(options.RefreshSessionPolicy, ResolveClientIpPartitionKey(context)));

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.SignUpPolicy,
                context => CreateFixedWindowPartition(options.SignUpPolicy, ResolveClientIpPartitionKey(context)));

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.EmailOperationsPolicy,
                context => CreateFixedWindowPartition(options.EmailOperationsPolicy, ResolveClientIpPartitionKey(context)));

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.PasswordResetPolicy,
                context => CreateFixedWindowPartition(options.PasswordResetPolicy, ResolveClientIpPartitionKey(context)));

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.TwoFactorVerificationPolicy,
                context => CreateFixedWindowPartition(options.TwoFactorVerificationPolicy, ResolveClientIpPartitionKey(context)));

            // Location resolution can trigger paid geocoding, so it is limited per authenticated user on top of the
            // global limiter.
            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.LocationResolutionPolicy,
                context => CreateFixedWindowPartition(options.LocationResolutionPolicy, ResolveAuthenticatedUserPartitionKey(context)));

            rateLimiterOptions.AddPolicy(
                RateLimitingPolicyNames.RiderLocationPolicy,
                context => CreateFixedWindowPartition(options.RiderLocationPolicy, ResolveAuthenticatedUserPartitionKey(context)));

            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                TimeSpan? retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue
                    : null;

                if (retryAfter is not null)
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.Value.TotalSeconds).ToString("F0");
                }

                var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Too many requests were sent in a short period. Please wait and try again.",
                        Extensions = { ["code"] = AuthenticationErrorCodes.RateLimited }
                    }
                });
            };
        });

        return services;
    }

    private static PartitionedRateLimiter<HttpContext> CreateGlobalLimiter(RateLimitingOptions options)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
            {
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    return CreateTokenBucketPartition(options.AuthenticatedGlobalPolicy, ResolveAuthenticatedUserPartitionKey(context));
                }

                return CreateSlidingWindowPartition(options.AnonymousGlobalPolicy, ResolveAnonymousPartitionKey(context));
            });
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        RateLimitingOptions.FixedWindowPolicyOptions policy,
        string partitionKey)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromMinutes(policy.WindowMinutes),
                QueueLimit = policy.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    private static RateLimitPartition<string> CreateSlidingWindowPartition(
        RateLimitingOptions.SlidingWindowPolicyOptions policy,
        string partitionKey)
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromMinutes(policy.WindowMinutes),
                SegmentsPerWindow = policy.SegmentsPerWindow,
                QueueLimit = policy.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    private static RateLimitPartition<string> CreateTokenBucketPartition(
        RateLimitingOptions.TokenBucketPolicyOptions policy,
        string partitionKey)
    {
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = policy.TokenLimit,
                TokensPerPeriod = policy.TokensPerPeriod,
                ReplenishmentPeriod = TimeSpan.FromSeconds(policy.ReplenishmentPeriodSeconds),
                AutoReplenishment = true,
                QueueLimit = policy.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    }

    private static string ResolveAuthenticatedUserPartitionKey(HttpContext context)
    {
        var appUserId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        return Guid.TryParse(appUserId, out var parsedAppUserId)
            ? $"user:{parsedAppUserId:D}"
            : ResolveClientIpPartitionKey(context);
    }

    private static string ResolveAnonymousPartitionKey(HttpContext context) => ResolveClientIpPartitionKey(context);

    private static string ResolveClientIpPartitionKey(HttpContext context)
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        if (remoteIpAddress is null)
        {
            return "ip:unknown";
        }

        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            remoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        return $"ip:{remoteIpAddress}";
    }

    private static void Validate(RateLimitingOptions.FixedWindowPolicyOptions policy, string name)
    {
        if (policy.PermitLimit <= 0)
        {
            throw new InvalidOperationException($"{name} permit limit must be greater than zero.");
        }

        if (policy.WindowMinutes <= 0)
        {
            throw new InvalidOperationException($"{name} window minutes must be greater than zero.");
        }

        if (policy.QueueLimit < 0)
        {
            throw new InvalidOperationException($"{name} queue limit must be zero or greater.");
        }
    }

    private static void Validate(RateLimitingOptions.SlidingWindowPolicyOptions policy, string name)
    {
        if (policy.PermitLimit <= 0)
        {
            throw new InvalidOperationException($"{name} permit limit must be greater than zero.");
        }

        if (policy.WindowMinutes <= 0)
        {
            throw new InvalidOperationException($"{name} window minutes must be greater than zero.");
        }

        if (policy.SegmentsPerWindow <= 0)
        {
            throw new InvalidOperationException($"{name} segments per window must be greater than zero.");
        }

        if (policy.QueueLimit < 0)
        {
            throw new InvalidOperationException($"{name} queue limit must be zero or greater.");
        }
    }

    private static void Validate(RateLimitingOptions.TokenBucketPolicyOptions policy, string name)
    {
        if (policy.TokenLimit <= 0)
        {
            throw new InvalidOperationException($"{name} token limit must be greater than zero.");
        }

        if (policy.TokensPerPeriod <= 0)
        {
            throw new InvalidOperationException($"{name} tokens per period must be greater than zero.");
        }

        if (policy.ReplenishmentPeriodSeconds <= 0)
        {
            throw new InvalidOperationException($"{name} replenishment period seconds must be greater than zero.");
        }

        if (policy.QueueLimit < 0)
        {
            throw new InvalidOperationException($"{name} queue limit must be zero or greater.");
        }
    }
}
