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

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;

public sealed class GoogleSignUpHandler(
    IAuthenticationIdentityService identityService,
    IGoogleAuthenticationFlowService googleAuthenticationFlowService,
    AuthenticationSessionIssuer sessionIssuer,
    IEventPublisher eventPublisher,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Stakeholder> stakeholderRepository,
    RegistrationCountryValidator registrationCountryValidator,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GoogleSignUpResult> HandleAsync(GoogleSignUpCommand request, CancellationToken cancellationToken)
    {
        var requestedAtUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = requestedAtUtc.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignUpStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var flowResult = await googleAuthenticationFlowService.TakeAsync(request.FlowToken, cancellationToken);
        if (flowResult.Status != GoogleAuthenticationFlowStatus.Success)
        {
            return new GoogleSignUpResult(MapFlowStatus(flowResult.Status));
        }

        var flow = flowResult.Flow!;
        if (flow.State != GoogleAuthenticationFlowState.RegistrationRequired
            || (flow.TenantId != Guid.Empty && request.ActorContext.TenantId != flow.TenantId)
            || string.IsNullOrWhiteSpace(flow.Subject)
            || string.IsNullOrWhiteSpace(flow.Email))
        {
            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            return new GoogleSignUpResult(GoogleSignUpStatus.GoogleFlowInvalid);
        }

        var countryValidation = await registrationCountryValidator.ValidateAsync(
            request.CountryId,
            request.IpAddress,
            cancellationToken);
        if (countryValidation == RegistrationCountryValidationResult.CountryNotFound)
        {
            await googleAuthenticationFlowService.RestoreAsync(flow, cancellationToken);
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new GoogleSignUpResult(
                GoogleSignUpStatus.ValidationFailed,
                ValidationErrors: new Dictionary<string, string[]>
                {
                    [nameof(GoogleSignUpCommand.CountryId)] = ["The selected country is invalid."]
                });
        }

        if (countryValidation == RegistrationCountryValidationResult.Mismatch)
        {
            await googleAuthenticationFlowService.RestoreAsync(flow, cancellationToken);
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.CountryMismatch);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.CountryMismatch));
            return new GoogleSignUpResult(GoogleSignUpStatus.CountryMismatch);
        }

        if (await identityService.FindByEmailAsync(flow.Email) is not null)
        {
            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
            return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateEmail);
        }

        var googleIdentity = new GoogleIdentityTokenPayload(
            flow.Subject,
            flow.Email,
            DisplayName: null,
            flow.EmailVerified,
            flow.HostedDomain);
        var authoritativeEmail = GoogleEmailAuthority.IsAuthoritative(googleIdentity);
        var user = AppUser.Create(flow.Email);
        if (authoritativeEmail)
        {
            user.MarkEmailVerified();
        }

        var tenantId = request.ActorContext.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to sign up.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Authentication.GoogleSignUpFailed,
                    ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
                await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
                return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateEmail);
            }

            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            await googleAuthenticationFlowService.RestoreAsync(flow, cancellationToken);
            return new GoogleSignUpResult(GoogleSignUpStatus.ValidationFailed, ValidationErrors: createResult.ToValidationDictionary());
        }

        var addLoginResult = await identityService.AddLoginAsync(
            user,
            ExternalLoginProviders.Google,
            flow.Subject,
            ExternalLoginProviders.Google);
        if (!addLoginResult.Succeeded)
        {
            if (addLoginResult.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.LoginAlreadyAssociated)))
            {
                customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateGoogleAccount);
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Authentication.GoogleSignUpFailed,
                    ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateGoogleAccount));
                await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
                return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateGoogleAccount);
            }

            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            await googleAuthenticationFlowService.RestoreAsync(flow, cancellationToken);
            return new GoogleSignUpResult(GoogleSignUpStatus.ValidationFailed, ValidationErrors: addLoginResult.ToValidationDictionary());
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

        AuthenticationTokens? tokens = null;
        if (authoritativeEmail)
        {
            tokens = await sessionIssuer.IssueAsync(
                user,
                stakeholder,
                request.IpAddress,
                request.UserAgent,
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignUpCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return authoritativeEmail
            ? new GoogleSignUpResult(GoogleSignUpStatus.Success, flow.Email, Tokens: tokens)
            : new GoogleSignUpResult(
                GoogleSignUpStatus.EmailVerificationRequired,
                flow.Email,
                RetryAtUtc: expiresAtUtc);
    }

    private static GoogleSignUpStatus MapFlowStatus(GoogleAuthenticationFlowStatus status) => status switch
    {
        GoogleAuthenticationFlowStatus.Expired => GoogleSignUpStatus.GoogleFlowExpired,
        GoogleAuthenticationFlowStatus.Consumed => GoogleSignUpStatus.GoogleFlowConsumed,
        _ => GoogleSignUpStatus.GoogleFlowInvalid
    };
}






