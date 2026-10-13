using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Domain.Payments.Services;

public sealed record PaymentProviderInitiationRequest(
    string MerchantReference,
    decimal Amount,
    string CurrencyCode,
    PaymentIntent PaymentIntent,
    Guid StakeholderId,
    Guid TenantId,
    Guid CountryId);
