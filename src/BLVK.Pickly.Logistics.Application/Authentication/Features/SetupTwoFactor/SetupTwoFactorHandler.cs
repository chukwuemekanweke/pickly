using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SetupTwoFactor;

public sealed class SetupTwoFactorHandler(
    TwoFactorActorResolver actorResolver,
    IAuthenticationIdentityService identityService,
    IOptions<TwoFactorAuthenticationOptions> options)
{
    public async Task<SetupTwoFactorResult> HandleAsync(
        SetupTwoFactorCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveAsync(command.ActorContext, cancellationToken);
        if (actor is null)
            return new SetupTwoFactorResult(SetupTwoFactorStatus.NotAuthenticated);
        if (await identityService.GetTwoFactorEnabledAsync(actor.User))
            return new SetupTwoFactorResult(SetupTwoFactorStatus.AlreadyEnabled);

        var key = await identityService.GetAuthenticatorKeyAsync(actor.User);
        if (string.IsNullOrWhiteSpace(key))
        {
            var reset = await identityService.ResetAuthenticatorKeyAsync(actor.User);
            if (!reset.Succeeded)
                return new SetupTwoFactorResult(SetupTwoFactorStatus.Failed);
            key = await identityService.GetAuthenticatorKeyAsync(actor.User);
        }

        if (string.IsNullOrWhiteSpace(key))
            return new SetupTwoFactorResult(SetupTwoFactorStatus.Failed);

        var issuer = options.Value.Issuer;
        var account = actor.User.Email ?? actor.User.UserName ?? actor.User.Id.ToString();
        var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}" +
            $"?secret={Uri.EscapeDataString(key)}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
        return new SetupTwoFactorResult(SetupTwoFactorStatus.Success, key, uri);
    }
}
