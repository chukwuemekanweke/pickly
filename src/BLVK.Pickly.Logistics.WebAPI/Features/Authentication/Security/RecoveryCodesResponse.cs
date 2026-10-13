namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Security;

public sealed record RecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
