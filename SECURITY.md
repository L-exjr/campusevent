# Security and secrets

## Configuration strategy

- Tracked `appsettings*.json` files contain only non-secret defaults, empty
  placeholders, and documentation. Never commit signing keys, passwords,
  connection-string passwords, API keys, or tokens.
- For local development, copy
  `backend/EventManagement.Api/appsettings.Development.example.json` to
  `appsettings.Development.json` if local non-secret overrides are needed. The
  destination is ignored by git. Store the JWT signing key with .NET User
  Secrets:

  ```bash
  cd backend/EventManagement.Api
  dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -hex 64)" >/dev/null
  ```

- For deployed environments, inject the key from the platform's secret manager
  as `Jwt__SigningKey`. Environment variables use double underscores for nested
  .NET configuration keys. Do not place the value in deployment manifests or
  checked-in environment files.
- Rotate a signing key immediately if it is printed, logged, committed, or
  disclosed. Rotation invalidates every JWT signed with the previous key. That
  is low-risk before public deployment, but requires coordinated session expiry
  and user reauthentication once real users exist.

## Transactional email credentials

Email uses Mailtrap's HTTPS Sending API. Store `MAILTRAP_API_TOKEN` only in User
Secrets locally or Railway variables when deployed, and rotate it immediately if
it is printed, logged, or committed. Configure `MAILTRAP_SENDER_EMAIL` separately;
the school-project setup uses Mailtrap's `hello@demomailtrap.co` demo sender.

Mailtrap's demo domain needs no custom DNS setup, but it can send only to the
email address registered on the Mailtrap account. A missing token or sender logs
an error and sends nothing. Provider failures are logged without exposing the
token or raw response body.

## Image upload credentials

The browser sends multipart image data only to authenticated `/api/uploads/*`
routes. The backend repeats size, MIME, and file-signature validation before it
writes to Supabase with `SUPABASE_SERVICE_ROLE_KEY`. That key bypasses storage
RLS and must exist only in backend User Secrets or Railway variables—never in a
`VITE_*` variable, frontend bundle, response, or log.

Uploaded objects are placed below an authenticated owner-ID prefix and first
recorded as pending in PostgreSQL. A profile/event save may claim only a pending
object owned by the acting account; expired pending objects and superseded
objects are deleted by a leased cleanup worker. Directly supplying a new
third-party image URL is intentionally rejected.

Once the backend proxy is deployed and verified, remove Supabase storage policies
that grant `anon` INSERT, UPDATE, or DELETE. Buckets can remain public-read when
their returned URLs are intended to appear on public event pages.

## Password reset tokens

Password reset responses are identical for known and unknown email addresses to
prevent account enumeration. Reset links contain a cryptographically random token;
only its SHA-256 hash is stored. Tokens expire after 30 minutes, become invalid after
one successful use, and requesting another link invalidates every prior unused token.
Changing the password also preserves a linked Google identity, if present.

`Frontend:BaseUrl` controls the non-secret origin used to build reset links. Keep it
at `http://localhost:5173` locally and set `Frontend__BaseUrl` to the deployed HTTPS
frontend URL in production.

## Google sign-in configuration

The frontend receives only the Google OAuth web client ID through
`VITE_GOOGLE_CLIENT_ID`; a client ID identifies the app but is not a credential.
The backend must be configured with the same audience:

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Google:ClientId" "your-web-client-id.apps.googleusercontent.com"
```

In production use `Google__ClientId`. Google Identity Services returns an ID token,
which the backend verifies for Google signature, issuer, expiry, verified email, and
the configured audience before issuing the existing application JWT. The stable
Google `sub` claim is persisted. A first Google login is matched to an existing
normalized email so a local account is linked rather than duplicated.

This ID-token flow does not use a Google client secret. If a client secret is created
for a future authorization-code flow, store it only as `Google:ClientSecret` in User
Secrets or `Google__ClientSecret` in the production secret manager. Never use a
`VITE_*` variable for it or commit it.

## Public booking-request abuse controls

`POST /api/booking-requests` uses a fixed-window limit of **5 requests per source IP
per hour**, with no queue. Excess requests receive HTTP 429. The form also includes
an off-screen `website` honeypot: a filled value receives the same neutral HTTP 202
response as a legitimate submission but is not stored. This avoids CAPTCHA tracking,
keys, and user friction at the current scale. Revisit a privacy-reviewed CAPTCHA if
targeted bot traffic starts bypassing the honeypot.

## Trusted reverse proxies

The API accepts Railway's documented `X-Real-IP` and `X-Forwarded-Proto` only from ASP.NET
Core's loopback defaults and Railway's documented internal proxy network
(`100.0.0.0/8`). Do not clear `KnownIPNetworks`/`KnownProxies` or trust arbitrary
forwarding hops: the authentication and public-booking rate limits partition by
the resolved client address. If the deployment platform changes, confirm its
official proxy network before updating
`Infrastructure/ForwardedHeadersConfiguration.cs`.

## Exposed-key history

The former development key must be treated as compromised because removing it
from the latest commit does not remove it from git history. Before making this
repository public, coordinate with every collaborator and purge the old value
using `git filter-repo` or BFG Repo-Cleaner, then force-push the rewritten
branches/tags and have collaborators re-clone. Do not rewrite shared history
without coordination.
