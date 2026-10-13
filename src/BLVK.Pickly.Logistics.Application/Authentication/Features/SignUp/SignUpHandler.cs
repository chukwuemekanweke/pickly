using BLVK.Pickly.Logistics.Application.Authentication.Constants;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Specifications;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;

public sealed class SignUpHandler(
    IAuthenticationIdentityService identityService,
    IEventPublisher eventPublisher,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Stakeholder> stakeholderRepository,
    RegistrationCountryValidator registrationCountryValidator,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SignUpResult> HandleAsync(SignUpCommand request, CancellationToken cancellationToken)
    {
        var requestedAtUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = requestedAtUtc.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignUpStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var countryValidation = await registrationCountryValidator.ValidateAsync(
            request.CountryId,
            request.IpAddress,
            cancellationToken);
        if (countryValidation == RegistrationCountryValidationResult.CountryNotFound)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new SignUpResult(
                SignUpStatus.ValidationFailed,
                new Dictionary<string, string[]>
                {
                    [nameof(SignUpCommand.CountryId)] = ["The selected country is invalid."]
                });
        }

        if (countryValidation == RegistrationCountryValidationResult.Mismatch)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.CountryMismatch);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.CountryMismatch));
            return new SignUpResult(SignUpStatus.CountryMismatch);
        }

        if (await identityService.FindByEmailAsync(request.Email) is not null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
            return new SignUpResult(SignUpStatus.DuplicateEmail);
        }

        var user = AppUser.Create(request.Email);
        var tenantId = request.ActorContext.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to sign up.");
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Authentication.PasswordSignUpFailed,
                    ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
                return new SignUpResult(SignUpStatus.DuplicateEmail);
            }

            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new SignUpResult(SignUpStatus.ValidationFailed, createResult.ToValidationDictionary());
        }

        var stakeholderType = await stakeholderTypeRepository.FirstOrDefaultAsync(
            new StakeholderTypeByTenantAndKeySpecification(tenantId, StakeholderDefaults.TypeKey),
            cancellationToken);
        if (stakeholderType is null)
        {
            throw new InvalidOperationException(
                $"Stakeholder type '{StakeholderDefaults.TypeKey}' is not configured for tenant '{tenantId}'.");
        }

        var stakeholder = Stakeholder.Create(user.Id, tenantId, request.CountryId, stakeholderType.Id, request.FirstName, request.LastName, request.Language);
        await stakeholderRepository.AddAsync(stakeholder);

        await eventPublisher.PublishAsync(new UserCreated
        {
            StakeholderId = stakeholder.Id,
            TenantId = tenantId,
            FlowId = request.ActorContext.FlowId,
            RequestedAtUtc = requestedAtUtc,
            ExpiresAtUtc = expiresAtUtc
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignUpCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new SignUpResult(
            SignUpStatus.Accepted,
            RetryAtUtc: expiresAtUtc);
    }
}






