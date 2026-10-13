using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenTokenResponse(
    [property: JsonPropertyName("access_token")] 
    string AccessToken,
    [property: JsonPropertyName("client_id")] 
    string ClientId,
    [property: JsonPropertyName("expires_in")] 
    int ExpiresIn,
    [property: JsonPropertyName("ibs_client_id")] 
    string IbsClientId,
    [property: JsonPropertyName("ibs_user_id")] 
    string IbsUserId,
    [property: JsonPropertyName("refresh_token")] 
    string? RefreshToken,
    [property: JsonPropertyName("token_type")] 
    string TokenType);
