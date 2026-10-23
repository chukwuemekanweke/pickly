using BLVK.Pickly.Logistics.Application.Authentication.Constants;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Authentication.Specifications;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Localization;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Specifications;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Specifications;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.DeviceSignIn;

public sealed class DeviceSignInHandler(
    IAuthenticationIdentityService identityService,
    AuthenticationSessionIssuer sessionIssuer,
    IDeviceAccountLock deviceAccountLock,
    IRepository<DeviceAccount> deviceAccountRepository,
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Country> countryRepository,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const string DefaultCountryShortCode = "NG";

    public async Task<DeviceSignInResult> HandleAsync(
        DeviceSignInCommand request,
        CancellationToken cancellationToken)
    {
        var identifier = DeviceAccount.NormalizeIdentifier(request.DeviceIdentifier);
        var tenantId = request.ActorContext.TenantId
            ?? throw new InvalidOperationException("Tenant id is required for device sign-in.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await deviceAccountLock.AcquireAsync(identifier, cancellationToken);

        var deviceAccount = await deviceAccountRepository.FirstOrDefaultAsync(
            new DeviceAccountByIdentifierSpecification(identifier),
            cancellationToken);

        AppUser user;
        Stakeholder stakeholder;
        if (deviceAccount is null)
        {
            (user, stakeholder) = await CreateAccountAsync(identifier, tenantId, cancellationToken);
            deviceAccount = DeviceAccount.Create(identifier, user.Id, stakeholder.Id);
            await deviceAccountRepository.AddAsync(deviceAccount, cancellationToken);
        }
        else
        {
            user = await identityService.FindByIdAsync(deviceAccount.AppUserId)
                ?? throw new InvalidOperationException(
                    $"Unable to resolve user '{deviceAccount.AppUserId}' for device account '{deviceAccount.Id}'.");
            stakeholder = await stakeholderRepository.GetByIdAsync(deviceAccount.StakeholderId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Unable to resolve stakeholder '{deviceAccount.StakeholderId}' for device account '{deviceAccount.Id}'.");

            if (stakeholder.TenantId != tenantId)
            {
                throw new InvalidOperationException("The device identifier is already registered to another tenant.");
            }
        }

        var tokens = await sessionIssuer.IssueAsync(
            user,
            stakeholder,
            request.IpAddress,
            request.UserAgent,
            cancellationToken);

        await eventPublisher.PublishAsync(new UserSignInSuccessful(request.IpAddress, request.UserAgent)
        {
            StakeholderId = stakeholder.Id,
            FlowId = request.ActorContext.FlowId,
            OccuredAt = timeProvider.GetUtcNow()
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DeviceSignInResult(tokens);
    }

    private async Task<(AppUser User, Stakeholder Stakeholder)> CreateAccountAsync(
        string identifier,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var identifierHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(identifier))).ToLowerInvariant();
        var user = AppUser.Create($"device-{identifierHash}@pickly.test");
        user.MarkEmailVerified();

        var createResult = await identityService.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"Unable to create the device user: {errors}.");
        }

        var stakeholderType = await stakeholderTypeRepository.FirstOrDefaultAsync(
            new StakeholderTypeByTenantAndKeySpecification(tenantId, StakeholderDefaults.TypeKey),
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Stakeholder type '{StakeholderDefaults.TypeKey}' is not configured for tenant '{tenantId}'.");
        var country = await countryRepository.FirstOrDefaultAsync(
            new CountryByShortCodeSpecification(DefaultCountryShortCode),
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Country '{DefaultCountryShortCode}' is not configured.");

        var stakeholder = Stakeholder.Create(
            user.Id,
            tenantId,
            country.Id,
            stakeholderType.Id,
            "Test",
            $"User {identifierHash[..8]}",
            SupportedLanguages.Default);
        stakeholder.MarkVerified();
        await stakeholderRepository.AddAsync(stakeholder, cancellationToken);

        return (user, stakeholder);
    }
}
