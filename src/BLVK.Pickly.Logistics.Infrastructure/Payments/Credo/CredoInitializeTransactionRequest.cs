namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;

internal sealed record CredoInitializeTransactionRequest(
    long Amount,
    string Email,
    string? CustomerPhoneNumber,
    string? CustomerFirstName,
    string? CustomerLastName,
    string Currency,
    string Reference,
    string Narration);
