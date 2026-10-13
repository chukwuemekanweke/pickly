# Authentication and sessions

The backend uses ASP.NET Core Identity for users, passwords, lockout, email confirmation, external-login associations, authenticator-app TOTP, and recovery codes. Successful authentication creates an `AuthenticationSession`, then issues the normal JWT access token and rotating refresh token. Google authentication and MFA extend this lifecycle; neither creates a separate user or session model.

## First factor and authenticated-session boundary

Password or Google verification is only the first factor for an MFA-enabled user. After the existing account, tenant, lockout, and email-confirmation checks succeed, the backend returns `outcome: "two_factor_required"`, an opaque challenge, and its expiry. It does not create an `AuthenticationSession`, issue access or refresh tokens, or publish `UserSignInSuccessful` at that point. Users without MFA continue to receive `outcome: "authenticated"` and the normal token fields.

The challenge is cryptographically random, lasts five minutes by default, permits five attempts, and stores its user, stakeholder, tenant, original authentication method, actor context, expiry, and original IP/user-agent context server-side through `IJsonCache` in Redis. The challenge value contains no client-decodable identity. The existing cache take-and-remove pattern and terminal markers distinguish invalid, expired, and consumed challenges, while invalid proofs restore the challenge only while attempts remain. Google flows are consumed once their state is transferred to the MFA challenge, so password and Google converge on `POST /api/v1/authentication/sessions/two-factor` without keeping the Google flow alive.

Authenticator codes are verified with Identity's authenticator token provider. Recovery codes are generated, counted, and redeemed by Identity and are therefore single-use; plaintext codes are returned only in the enrollment or regeneration response and are not copied to application storage or logs. Successful verification alone invokes `AuthenticationSessionIssuer`, publishes the normal successful-sign-in event, and returns the authenticated token response.

## MFA management

Authenticated callers using the active-session policy manage MFA under `/api/v1/authentication/security/two-factor`: status, setup, enrollment verification, recovery-code regeneration, and disable. Setup returns Identity's existing authenticator key (creating it only when absent) plus an `otpauth://` URI; status never returns the key. Enrollment enables MFA only after a valid six-digit Identity authenticator token and returns newly generated recovery codes once. Regeneration and disable require a current authenticator or recovery-code proof.

Enrollment, recovery-code regeneration, and disable rotate the Identity security stamp and revoke all active application sessions. Disable also resets the authenticator key, so re-enrollment uses a new secret. Refresh-token rotation remains transparent after an MFA-authenticated session is established and never creates or requests an MFA challenge; the existing session, refresh-token, security-stamp, lockout, email, and ownership checks continue to apply.

## Google authentication lifecycle

The BFF starts an attempt with `POST /api/v1/authentication/google/flows`. The response contains an opaque flow token, a cryptographically random nonce, and an expiry. The BFF stores the flow token in an HttpOnly cookie and gives only the nonce to Google Identity Services. Flow data is stored as short-lived JSON in Redis for ten minutes; Google ID tokens are never stored.

The BFF submits the flow token and Google ID token to `POST /api/v1/authentication/sessions/google`. The backend takes the flow through `IJsonCache`, validates the ID token signature, issuer, allowed audience, lifetime, required claims, and nonce, then chooses one outcome:

- `authenticated`: the Google `sub` is already linked and MFA is disabled. The backend creates the normal application session and returns access and refresh tokens.
- `two_factor_required`: the linked account has MFA enabled. The Google flow is consumed and an opaque MFA challenge is returned without application tokens.
- `link_required`: no external login exists, but an Identity user has the validated Google email. Matching email alone never links accounts.
- `registration_required`: neither the Google `sub` nor validated email belongs to an application account.

Continuation and terminal operations use the Redis-backed cache take-and-remove pattern. A consumed marker remains briefly after expiry so replay, expiry, and unknown tokens produce distinct stable codes: `google_flow_consumed`, `google_flow_expired`, and `google_flow_invalid`.

## Existing-account linking

`POST /api/v1/authentication/google-links` accepts the flow token and the existing account password. The server uses only the validated Google identity stored in the `link_required` flow. Password verification uses Identity failed-access and reset APIs, including configured lockout. After proof succeeds, `AddLoginAsync` associates provider `Google` with Google's immutable `sub`; the password remains usable. A normal session is returned when the account email is confirmed.

## Google registration and email authority

`POST /api/v1/authentication/registrations/google` accepts the `registration_required` flow token plus the remaining profile fields. Email and Google `sub` always come from the server-side flow. Gmail/Googlemail identities and Workspace identities with an `hd` claim are treated as authoritative when `email_verified` is true, so registration confirms the email and returns a normal session immediately. A verified Google account backed by an external mailbox is not treated as authoritative: the account is created, the existing email-confirmation process continues, and the API returns `email_verification_required` without tokens. That 403 Problem Details response includes the backend-authoritative `email` and the email-confirmation expiry as `retryAtUtc`, allowing the confirmation screen to continue without asking for the address again.

## Configuration

Google authentication uses `Authentication:Google`:

```json
{
  "Enabled": true,
  "ClientIds": ["web-client-id.apps.googleusercontent.com"],
  "FlowLifetime": "00:10:00"
}
```

Startup fails when Google authentication is enabled without at least one non-empty client ID, or when the flow lifetime is outside five to ten minutes. Multiple client IDs are allowed. No Google client secret is used.

MFA uses `Authentication:TwoFactor` with issuer label, challenge lifetime, challenge attempt count, and recovery-code count settings. The defaults are `BLVK.Pickly.Logistics`, five minutes, five attempts, and ten recovery codes.
