using Newtonsoft.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo.Payloads;

internal sealed record CredoInitializeTransactionPayload(
    [property: JsonProperty("amount")] long Amount,
    [property: JsonProperty("email")] string Email,
    [property: JsonProperty("customerPhoneNumber")] string? CustomerPhoneNumber,
    [property: JsonProperty("customerFirstName")] string? CustomerFirstName,
    [property: JsonProperty("customerLastName")] string? CustomerLastName,
    [property: JsonProperty("currency")] string Currency,
    [property: JsonProperty("reference")] string Reference,
    [property: JsonProperty("callbackUrl")] string? CallbackUrl,
    [property: JsonProperty("channels")] IReadOnlyList<string> Channels,
    [property: JsonProperty("bearer")] int Bearer,
    [property: JsonProperty("metadata")] IReadOnlyDictionary<string, string> Metadata,
    [property: JsonProperty("narration")] string Narration,
    [property: JsonProperty("initializeAccount")] int InitializeAccount);
