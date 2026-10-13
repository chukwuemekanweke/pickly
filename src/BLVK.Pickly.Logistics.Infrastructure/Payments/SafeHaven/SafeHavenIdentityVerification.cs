using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenIdentityVerification(
    [property: JsonPropertyName("_id")] 
string Id,
    string ClientId,
    string IdentityNumber,
    string Type,
    decimal Amount,
    string Status,
    string DebitAccountNumber,
    decimal Vat,
    decimal StampDuty,
    bool IsDeleted,
    bool OtpVerified,
    int OtpResendCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string DebitMessage,
    int DebitResponsCode,
    string DebitSessionId,
    string OtpId,
    SafeHavenProviderResponse ProviderResponse);
