---
type: domain
title: Authentication Domain
description: Sign-up/sign-in, TOTP MFA and recovery codes, sessions and refresh tokens, Google sign-in, password reset, session revocation, and the ASP.NET Core Identity base
resource: okf://knowledge/domains/authentication
tags:
    - context
    - domain
    - authentication
    - identity
    - sessions
governance: context
code_refs:
    - src/BLVK.Pickly.Logistics.Application/Authentication/**
    - src/BLVK.Pickly.Logistics.Domain/Authentication/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Authentication/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/LoginActivityReadModelRepository.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/ActiveSessionReadModelRepository.cs
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Authentication/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Stakeholders/LoginActivity/**
    - src/BLVK.Pickly.Logistics.Consumer/Authentication/**
    - src/BLVK.Pickly.Logistics.Jobs/Authentication/**
    - src/BLVK.Pickly.Logistics.Contracts/Commands/Authentication/**
    - tests/unit/BLVK.Pickly.Logistics.Application.UnitTests/Authentication/**
    - tests/unit/BLVK.Pickly.Logistics.WebAPI.UnitTests/Features/Stakeholders/LoginActivity/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/Authentication/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/Stakeholders/LoginActivity/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Domain/Authentication/Entities/AuthenticationSession.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Scope

Sign-up (with email OTP confirmation), sign-in (including Google sign-in), session lifecycle (list/refresh/logout/revoke-other/revoke-single sessions), password change/reset, email-existence checks,
authenticator-app MFA and recovery codes, login-activity recording/history and IP-address-location enrichment, and active-session/device management.

## Foundation

ASP.NET Core Identity is the authentication base (see [[dotnet-style]]) — do not reintroduce custom password hashing, token issuance, or credential-check flows when Identity already covers the need.
Sessions and refresh tokens are modeled as first-class domain entities (`AuthenticationSession`, `AuthenticationRefreshToken`) alongside Identity rather than replacing Identity's user/credential
model.

## Authenticator MFA boundary

ASP.NET Core Identity owns the authenticator key, TOTP verification, two-factor enabled flag, and
hashed single-use recovery codes. The application does not maintain a parallel MFA user store or
implement TOTP cryptography.

Password and Google first factors converge on a five-minute, five-attempt `TwoFactorChallenge`
stored server-side through Redis-backed `IJsonCache`. The client receives only a cryptographically
random opaque token. A challenge captures the user, stakeholder, tenant, original password/Google
method, actor context, expiry, and original IP/user-agent needed for final session creation. Cache
take-and-remove plus terminal markers detects subsequent replay. The preceding Google flow is
consumed after the MFA challenge assumes continuation ownership.

For MFA-enabled users, first-factor success does not create an `AuthenticationSession`, issue tokens,
or publish `UserSignInSuccessful`; it returns `two_factor_required`.
`CompleteTwoFactorChallengeHandler` rechecks tenant, account, lockout, email confirmation, and MFA
state, verifies an Identity authenticator token or redeems an Identity recovery code, and only then
calls `AuthenticationSessionIssuer` and publishes normal sign-in success. Invalid proofs increment
Identity failed access and consume one challenge attempt.

MFA management uses the active-session policy. Setup reads/creates Identity's authenticator key without
replacing an in-progress key. Enrollment, recovery-code regeneration, and disable preserve the
initiating session and its refresh-token lifecycle while revoking every other active application
session; disable also resets the authenticator key. The initiating session id comes from the
authorized access token rather than the request body. Recovery codes are returned only when generated
and are never copied into application persistence or cache. Recovery-code input accepts Identity's
canonical `XXXXX-XXXXX` form as well as compact, spaced, or case-varied equivalents; invalid MFA
proofs return the stable `invalid_two_factor_code` problem code. Refresh-token rotation remains the
existing transparent session flow and never requests MFA again.

## Google authentication continuation

Google authentication is a single entry point backed by a 5-10 minute Redis `IJsonCache` flow.
The flow binds the Google ID token to a cryptographically random nonce and stores the validated
`sub`, email, `email_verified`, hosted domain, tenant, continuation state and expiry server-side.
Flow data uses the existing cache take-and-remove operation before use, and terminal flows retain a
short consumed marker for replay detection.

`GoogleSignInHandler` returns one of four primary outcomes: a normal `AuthenticationSession` and
tokens for an existing `Google` external login with MFA disabled, `two_factor_required` for an
MFA-enabled linked account, `link_required` for a matching password account, or
`registration_required` for a new account. Matching email never links automatically.
`LinkGoogleAccountHandler` requires the existing password and uses Identity
failed-access/reset/lockout APIs before `AddLoginAsync`. Google registration consumes only validated
flow identity; Gmail and Workspace (`hd`) email is authoritative, while external-mailbox Google
accounts continue through normal email confirmation.
Because `AppUser` and active `Stakeholder` are one-to-one, authentication resolves a stakeholder by
`AppUserId`; Google flows then validate the resolved stakeholder's tenant explicitly instead of using
tenant identity as an additional stakeholder lookup key.
For that non-authoritative-email registration path, the `email_verification_required` 403 Problem
Details response includes the server-side flow `email` and the confirmation expiry as `retryAtUtc`;
clients must use this continuation metadata instead of asking the user to re-enter the Google email.

## Active-session and login-activity location display

Both `GET /sessions` (active sessions) and login-activity history expose the relevant IP address plus
coarse `City`/`State`/`Country` fields, but neither endpoint calls a geolocation provider itself.
Location is resolved centrally per unique IP value and periodically refreshed when stale by
`Jobs.Authentication.IpAddressLocationEnrichmentProcessor` (batched, via
`IIpGeolocationService`/`IIpGeolocationProvider`), and persisted on the shared `IpAddress`/
`IpAddressLocation` aggregate (`IsCurrentLocation` marks the live row; see [[persistence]]). Both
read endpoints are dedicated `AppReadDbContext`-backed read-model repositories
(`ILoginActivityReadModelRepository`/`LoginActivityReadModelRepository`,
`IActiveSessionReadModelRepository`/`ActiveSessionReadModelRepository`) that LEFT JOIN against that
shared location data in a single query — never re-resolving or re-calling a provider per request, and
never per-row (no N+1). `ActiveSessionReadModelRepository` joins on `AuthenticationSession.LastIpAddressId`
filtered to `IsCurrentLocation`, so a session's displayed location always reflects the current
resolution for its last-seen IP. `IAuthenticationSessionService.ListActiveAsync` (the aggregate/
specification-backed listing used by session revocation) is untouched by this and stays separate from
the read-model path used for display.
An IP that fails to resolve (private/loopback/local, or an unresponsive provider) degrades to
`null` City/State/Country on the affected sessions/activities; it never fails the request.
The user-facing login-activity history returns successful initial sign-ins only. Token-refresh
activity may remain persisted for internal audit and telemetry purposes, but the read-model query
filters it before ordering and cursor pagination.

## Cross-cutting

- Sign-up, email confirmation, and access-token refresh publish `Contracts` events (`UserCreated`, `UserEmailConfirmed`, `UserAccessTokenRefreshed`, `UserSignInSuccessful`, `UserSignInFailed`) through
  the outbox — see [[async-processing]]. `Consumer` handlers react to these (e.g. IP-location enrichment, welcome email).
- Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions.

## Feature-flagged MVP registration

`Microsoft.FeatureManagement` exposes the scoped `IsMVP` policy to both WebAPI registration flows
and the Consumer email-OTP sender. `RegistrationProfileResolver` is the shared application boundary
for password and Google registration. MVP mode allows missing profile fields, generates separate
`pikly-` GUID-based names, uses `SupportedLanguages.Default`, resolves Nigeria through the persisted
`Country.ShortCode == "NG"` record, and skips the normal IP-country mismatch check. Standard mode
retains required profile fields and existing country validation.

Email confirmation generation uses five numeric digits in MVP mode and six in standard mode.
Verification compares against the cached issued code. The HTTP validator uses the current policy when
no code is active and the cached code's actual length when one exists, so an active code survives a
feature-flag change without weakening expiry, attempt, consumption, or resend controls. No
registration schema or domain-invariant changes are required.
