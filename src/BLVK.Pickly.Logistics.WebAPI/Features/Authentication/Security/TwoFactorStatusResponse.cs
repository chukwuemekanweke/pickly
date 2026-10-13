namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Security;

public sealed record TwoFactorStatusResponse(bool Enabled, int RecoveryCodesRemaining);
