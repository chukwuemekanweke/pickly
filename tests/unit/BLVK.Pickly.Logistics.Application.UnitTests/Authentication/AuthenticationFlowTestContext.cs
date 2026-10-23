using BLVK.Pickly.Logistics.Application.Authentication;
using BLVK.Pickly.Logistics.Application.Authentication.Features.ChangePassword;
using BLVK.Pickly.Logistics.Application.Authentication.Features.CheckEmailExistence;
using BLVK.Pickly.Logistics.Application.Authentication.Features.CompletePasswordReset;
using BLVK.Pickly.Logistics.Application.Authentication.Features.CompleteTwoFactorChallenge;
using BLVK.Pickly.Logistics.Application.Authentication.Features.DeviceSignIn;
using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;
using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;
using BLVK.Pickly.Logistics.Application.Authentication.Features.LinkGoogleAccount;
using BLVK.Pickly.Logistics.Application.Authentication.Features.ListSessions;
using BLVK.Pickly.Logistics.Application.Authentication.Features.LogoutSession;
using BLVK.Pickly.Logistics.Application.Authentication.Features.RefreshSession;
using BLVK.Pickly.Logistics.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BLVK.Pickly.Logistics.Application.Authentication.Features.RequestPasswordReset;
using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;
using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUpOtp;
using BLVK.Pickly.Logistics.Application.Authentication.Stakeholders;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Localization;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication;

internal sealed class AuthenticationFlowTestContext
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IGoogleIdentityTokenService GoogleIdentityTokenService { get; } = Substitute.For<IGoogleIdentityTokenService>();
    public IGoogleAuthenticationFlowService GoogleAuthenticationFlowService { get; } = Substitute.For<IGoogleAuthenticationFlowService>();
    public ITwoFactorChallengeService TwoFactorChallengeService { get; } = Substitute.For<ITwoFactorChallengeService>();
    public IRefreshTokenService RefreshTokenService { get; } = Substitute.For<IRefreshTokenService>();
    public IAuthenticationSessionService SessionService { get; } = Substitute.For<IAuthenticationSessionService>();
    public IActiveSessionReadModelRepository ActiveSessionReadModelRepository { get; } = Substitute.For<IActiveSessionReadModelRepository>();
    public IAccessTokenRevocationService AccessTokenRevocationService { get; } = Substitute.For<IAccessTokenRevocationService>();
    public ITwoFactorOtpService TwoFactorOtpService { get; } = Substitute.For<ITwoFactorOtpService>();
    public IAccessTokenService AccessTokenService { get; } = Substitute.For<IAccessTokenService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IRegistrationPolicyProvider RegistrationPolicyProvider { get; } = Substitute.For<IRegistrationPolicyProvider>();
    public IIpGeolocationService IpGeolocationService { get; } = Substitute.For<IIpGeolocationService>();
    public IRepository<Country> CountryRepository { get; } = Substitute.For<IRepository<Country>>();
    public IRepository<DeviceAccount> DeviceAccountRepository { get; } = Substitute.For<IRepository<DeviceAccount>>();
    public IDeviceAccountLock DeviceAccountLock { get; } = Substitute.For<IDeviceAccountLock>();
    public IStakeholderReadModelRepository StakeholderReadModelRepository { get; } = Substitute.For<IStakeholderReadModelRepository>();
    public IRepository<StakeholderType> StakeholderTypeRepository { get; } = Substitute.For<IRepository<StakeholderType>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public StakeholderResolver StakeholderResolver => new(StakeholderRepository);
    public RegistrationCountryValidator RegistrationCountryValidator => new(CountryRepository, IpGeolocationService);
    public RegistrationProfileResolver RegistrationProfileResolver =>
        new(RegistrationPolicyProvider, CountryRepository, RegistrationCountryValidator);
    public AuthenticationSessionIssuer SessionIssuer => new(AccessTokenService, RefreshTokenService, SessionService);
    public PasswordCredentialVerifier PasswordCredentialVerifier => new(IdentityService);
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();

    public AuthenticationFlowTestContext()
    {
        RefreshTokenService.GetExpiry(Arg.Any<TimeSpan?>()).Returns(Clock.GetUtcNow().AddDays(30));
        SessionService.CreateAsync(Arg.Any<Stakeholder>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(call => AuthenticationSession.Create(((Stakeholder)call[0]).Id, Guid.CreateVersion7(),
                (string)call[2], null, null, null, Clock.GetUtcNow(), (DateTimeOffset)call[3]));
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Transaction));
        IdentityService.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        IdentityService.AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        IdentityService.AccessFailedAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        IdentityService.ResetAccessFailedCountAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        CountryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg"));
        CountryRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Country>>(), Arg.Any<CancellationToken>())
            .Returns(Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg"));
        RegistrationPolicyProvider.GetAsync().Returns(RegistrationPolicy.Create(isMvp: false));
    }

    public SignUpHandler CreateSignUpHandler() => new(
        IdentityService,
        EventPublisher,
        StakeholderTypeRepository,
        StakeholderRepository,
        RegistrationProfileResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public CheckEmailExistenceHandler CreateCheckEmailExistenceHandler() => new(IdentityService);
    public GoogleSignUpHandler CreateGoogleSignUpHandler()
    {
        return new GoogleSignUpHandler(
            IdentityService,
            GoogleAuthenticationFlowService,
            SessionIssuer,
            EventPublisher,
            StakeholderTypeRepository,
            StakeholderRepository,
            RegistrationProfileResolver,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);
    }
    public SignUpOtpHandler CreateSignUpOtpHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        AccessTokenService,
        RefreshTokenService,
        SessionService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public RequestEmailConfirmationOtpHandler CreateRequestEmailConfirmationOtpHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        CommandSender,
        StakeholderResolver,
        UnitOfWork,
        Clock);
    public SignInHandler CreateSignInHandler() => new(
        IdentityService,
        PasswordCredentialVerifier,
        SessionIssuer,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock,
        TwoFactorChallengeService);
    public DeviceSignInHandler CreateDeviceSignInHandler() => new(
        IdentityService,
        SessionIssuer,
        DeviceAccountLock,
        DeviceAccountRepository,
        StakeholderRepository,
        StakeholderTypeRepository,
        CountryRepository,
        EventPublisher,
        UnitOfWork,
        Clock);
    public GoogleSignInHandler CreateGoogleSignInHandler()
    {
        return new GoogleSignInHandler(
            IdentityService,
            GoogleIdentityTokenService,
            GoogleAuthenticationFlowService,
            SessionIssuer,
            EventPublisher,
            StakeholderResolver,
            CustomTelemetryContext,
            UnitOfWork,
            Clock,
            TwoFactorChallengeService);
    }
    public CompleteTwoFactorChallengeHandler CreateCompleteTwoFactorChallengeHandler() => new(
        TwoFactorChallengeService,
        IdentityService,
        SessionIssuer,
        StakeholderResolver,
        EventPublisher,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public LinkGoogleAccountHandler CreateLinkGoogleAccountHandler() => new(
        IdentityService,
        GoogleAuthenticationFlowService,
        PasswordCredentialVerifier,
        SessionIssuer,
        StakeholderResolver,
        EventPublisher,
        UnitOfWork,
        Clock);

    public void SetGoogleFlow(
        string token,
        GoogleAuthenticationFlowState state,
        string email,
        string subject,
        Guid? tenantId = null,
        bool emailVerified = true,
        string? hostedDomain = "gmail.com")
    {
        GoogleAuthenticationFlowService.TakeAsync(token, Arg.Any<CancellationToken>())
            .Returns(new GoogleAuthenticationFlowResult(
                GoogleAuthenticationFlowStatus.Success,
                new GoogleAuthenticationFlow(
                    token,
                    "nonce",
                    tenantId ?? Guid.Empty,
                    state,
                    Clock.GetUtcNow().AddMinutes(10),
                    subject,
                    email,
                    emailVerified,
                    hostedDomain)));
    }
    public RefreshSessionHandler CreateRefreshSessionHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        SessionService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public RequestPasswordResetHandler CreateRequestPasswordResetHandler() => new(
        IdentityService,
        CommandSender,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork);
    public CompletePasswordResetHandler CreateCompletePasswordResetHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        SessionService);
    public ChangePasswordHandler CreateChangePasswordHandler() => new(
        IdentityService,
        StakeholderReadModelRepository,
        CustomTelemetryContext,
        UnitOfWork);
    public LogoutSessionHandler CreateLogoutSessionHandler() => new(
        AccessTokenRevocationService,
        CustomTelemetryContext,
        SessionService,
        UnitOfWork);

    public ListSessionsHandler CreateListSessionsHandler() => new(ActiveSessionReadModelRepository, Clock);

    private static ActorContext TestActorContext() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7().ToString("N"),
        Guid.CreateVersion7().ToString("N"));

    public static SignUpCommand CreateSignUpCommand(
        string? email = null,
        string? password = null,
        Guid? countryId = null,
        string? firstName = null,
        string? lastName = null)
    {
        var resolvedPassword = password ?? AuthenticationTestData.StrongPassword();

        return new SignUpCommand(
            email ?? AuthenticationTestData.Email(),
            resolvedPassword,
            resolvedPassword,
            countryId ?? Guid.CreateVersion7(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName(),
            AuthenticationTestData.IpAddress(),
            TestActorContext(),
            SupportedLanguages.English);
    }

    public static SignInCommand CreateSignInCommand(
        string? email = null,
        string? password = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            password ?? AuthenticationTestData.StrongPassword(),
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public static DeviceSignInCommand CreateDeviceSignInCommand(
        string? deviceIdentifier = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            deviceIdentifier ?? Guid.CreateVersion7().ToString("N"),
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public static GoogleSignUpCommand CreateGoogleSignUpCommand(
        string? idToken = null,
        Guid? countryId = null,
        string? firstName = null,
        string? lastName = null) =>
        new(
            idToken ?? "google-id-token",
            countryId ?? Guid.CreateVersion7(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName(),
            AuthenticationTestData.IpAddress(),
            TestActorContext(),
            SupportedLanguages.English);

    public static GoogleSignInCommand CreateGoogleSignInCommand(
        string? idToken = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            idToken ?? "google-id-token",
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public static SignUpOtpCommand CreateSignUpOtpCommand(
        string? email = null,
        string? otp = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            otp ?? AuthenticationTestData.Otp(),
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public static RequestPasswordResetCommand CreateRequestPasswordResetCommand(string? email = null) =>
        new(email ?? AuthenticationTestData.Email(), TestActorContext());

    public static CompletePasswordResetCommand CreateCompletePasswordResetCommand(
        string? email = null,
        string? otp = null,
        string? password = null,
        string? confirmPassword = null)
    {
        var resolvedPassword = password ?? AuthenticationTestData.StrongPassword();

        return new CompletePasswordResetCommand(
            email ?? AuthenticationTestData.Email(),
            otp ?? AuthenticationTestData.Otp(),
            resolvedPassword,
            confirmPassword ?? resolvedPassword,
            TestActorContext());
    }

    public static ChangePasswordCommand CreateChangePasswordCommand(
        string? currentPassword = null,
        string? newPassword = null,
        ActorContext? actorContext = null)
    {
        var resolvedNewPassword = newPassword ?? "N3wP@ssword!";

        return new ChangePasswordCommand(
            currentPassword ?? AuthenticationTestData.StrongPassword(),
            resolvedNewPassword,
            resolvedNewPassword,
            actorContext ?? TestActorContext());
    }

    public static RefreshSessionCommand CreateRefreshSessionCommand(
        string? refreshToken = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            refreshToken ?? "refresh-token",
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public AppUser CreateUser(
        string? email = null,
        string? firstName = null,
        string? lastName = null) =>
        AppUser.Create(
            email ?? AuthenticationTestData.Email(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName());

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}


