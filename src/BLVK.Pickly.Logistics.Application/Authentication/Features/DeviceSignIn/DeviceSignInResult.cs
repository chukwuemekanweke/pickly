using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.DeviceSignIn;

public sealed record DeviceSignInResult(AuthenticationTokens Tokens);
