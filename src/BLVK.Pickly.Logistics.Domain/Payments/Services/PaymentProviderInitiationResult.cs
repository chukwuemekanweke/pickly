using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Domain.Payments.Services;

public sealed record PaymentProviderInitiationResult(
    string ProviderReference,
    string PaymentProvider,
    PaymentMethodType PaymentMethodType,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyDictionary<string, string> InstructionFields);
