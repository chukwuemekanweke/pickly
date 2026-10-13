namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "BLVK.Pickly.Logistics";
    public string Audience { get; init; } = "BLVK.Pickly.Logistics.Clients";
    public string SigningKey { get; init; } = "super-secret-template-signing-key-change-me";
    public int LifetimeMinutes { get; init; } = 5;
}
