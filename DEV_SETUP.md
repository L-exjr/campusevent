# Local development setup

Start with the root [README.md](README.md) for PostgreSQL, API, and frontend
startup. This document covers optional providers and development-only behavior.

## Configuration boundaries

- Put browser-safe values such as `VITE_API_BASE_URL` and
  `VITE_GOOGLE_CLIENT_ID` in the ignored root `.env.local`.
- Put backend secrets in .NET User Secrets from `backend/EventManagement.Api`.
- ASP.NET Core does not load the root Vite `.env.local` file.
- Never give a secret a `VITE_*` prefix; Vite embeds those values in browser code.

The root `.env.example` lists both frontend and deployment environment names, but
it is a reference inventory rather than a file that should be copied wholesale
into one process.

## Supabase Storage

The application uses Supabase only for object storage. Authentication and domain
data remain in the ASP.NET Core API and PostgreSQL.

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Supabase:Url" "https://your-project-ref.supabase.co"
dotnet user-secrets set "Supabase:ServiceRoleKey" "your-service-role-key"
```

The key must be the backend service-role key, not the public anon/publishable key.
See [SUPABASE_SETUP.md](SUPABASE_SETUP.md) before testing uploads or certificates.

## Email providers

The outbox supports Mailtrap's Sending API and Gmail SMTP. Mailtrap is the default
for local sandboxing; production currently selects Gmail through
`EMAIL_PROVIDER=Gmail`.

### Mailtrap

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Email:Provider" "Mailtrap"
dotnet user-secrets set "Email:Api:Token" "your-mailtrap-api-token"
dotnet user-secrets set "Email:Api:SenderEmail" "hello@demomailtrap.co"
dotnet user-secrets set "Email:Api:SenderName" "Campus Events"
```

Mailtrap's demo-domain recipient restrictions and account quotas can change;
confirm them in the Mailtrap dashboard instead of relying on hard-coded limits.

### Gmail SMTP

Use a Google App Password from an account with 2-Step Verification enabled. Never
store the normal account password.

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Email:Provider" "Gmail"
dotnet user-secrets set "Email:Gmail:Username" "account@gmail.com"
dotnet user-secrets set "Email:Gmail:AppPassword" "google-app-password"
dotnet user-secrets set "Email:Gmail:SenderEmail" "account@gmail.com"
dotnet user-secrets set "Email:Gmail:SenderName" "Campus Events"
```

The tracked host and port defaults are `smtp.gmail.com` and `587`. The application
logs a warning as accepted-send volume approaches
`Email:Gmail:DailyWarningThreshold`; that per-process counter is not a replacement
for provider-side monitoring.

## Google sign-in

1. Create a Google OAuth client with application type **Web application**.
2. Add `http://localhost:5173` as an authorized JavaScript origin.
3. Put the public client ID in `.env.local`:

   ```dotenv
   VITE_GOOGLE_CLIENT_ID=your-web-client-id.apps.googleusercontent.com
   ```

4. Configure the same audience in the backend:

   ```bash
   cd backend/EventManagement.Api
   dotnet user-secrets set "Google:ClientId" "your-web-client-id.apps.googleusercontent.com"
   ```

Restart Vite after changing `.env`. Do not put a Google client secret in the
frontend. The current ID-token flow does not need one; see [SECURITY.md](SECURITY.md#google-sign-in-configuration)
for the storage rule if a future server-side authorization-code flow introduces it.

## npm audit resolution

## Optional local secret scan

Before committing, developers with Gitleaks installed can scan staged changes:

```bash
gitleaks git --pre-commit --staged --redact=100
```

The repository-level `.gitleaks.toml` extends the default rules with provider-specific
checks used by CI. Do not bypass a finding unless it has been manually verified and
given a narrow fingerprint allowlist entry.

The initial `npm audit` reported two high findings. They were two dependency
nodes for one vulnerable package family: `react-router-dom@7.11.0` was a direct
runtime dependency and pulled in the transitive `react-router@7.11.0`.

```text
# npm audit report

react-router  6.0.0 - 7.17.0
Severity: high
React Router vulnerable to XSS via Open Redirects - https://github.com/advisories/GHSA-2w69-qvjg-hvjx
React Router SSR XSS in ScrollRestoration - https://github.com/advisories/GHSA-8v8x-cx79-35w7
React Router's vendored turbo-stream v2 allows arbitrary constructor invocation via TYPE_ERROR deserialization leading to Unauth RCE - https://github.com/advisories/GHSA-49rj-9fvp-4h2h
React Router's same-origin redirect with path starting // causes open redirect via protocol-relative URL reinterpretation - https://github.com/advisories/GHSA-2j2x-hqr9-3h42
React Router vulnerable to XSS in unstable RSC redirect handling via javascript: redirect targets - https://github.com/advisories/GHSA-8646-j5j9-6r62
React Router has stored XSS via unescaped Location header in prerendered redirect HTML - https://github.com/advisories/GHSA-f22v-gfqf-p8f3
React Router vulnerable to DoS via unbounded path expansion in __manifest endpoint - https://github.com/advisories/GHSA-8x6r-g9mw-2r78
React Router vulnerable to Denial of Service via reflected user input in single-fetch - https://github.com/advisories/GHSA-rxv8-25v2-qmq8
React Router has CSRF issue in Action/Server Action Request Processing - https://github.com/advisories/GHSA-h5cw-625j-3rxh
React Router: Open redirect via backslash in <Link> and useNavigate (CVE-2025-68470 bypass) - https://github.com/advisories/GHSA-wrjc-x8rr-h8h6
React Router: Open redirect leading to XSS - https://github.com/advisories/GHSA-jjmj-jmhj-qwj2
React Router: RSCErrorHandler Missing Protocol Validation (XSS) - https://github.com/advisories/GHSA-h8fp-f39c-q6mh
React Router: Arbitrary Constructor Injection via deserializeErrors() in React Router SSR Hydration - https://github.com/advisories/GHSA-337j-9hxr-rhxg
React Router: Unauthenticated Denial of Service via Inefficient Route Matching - https://github.com/advisories/GHSA-chx6-hx7r-mcp5
fix available via `npm audit fix`
node_modules/react-router
  react-router-dom  7.0.0-pre.0 - 7.11.0
  Depends on vulnerable versions of react-router
  node_modules/react-router-dom

2 high severity vulnerabilities

To address all issues, run:
  npm audit fix
```

`npm audit fix --dry-run` showed a same-major update from 7.11.0 to 7.18.2,
so the non-forced fix was applied. This closes the client-side redirect/XSS
advisories relevant to this Vite single-page app. No major version was accepted.
The current Google Identity Services flow verifies an ID token and does not use a
client secret or redirect URI. Restart Vite after changing `.env.local`.

## Paystack and signed tickets

The API needs test secrets to exercise paid checkout and QR tickets:

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Payments:Paystack:SecretKey" "sk_test_replace-me"
dotnet user-secrets set "Payments:Flutterwave:SecretKey" "FLWSECK_TEST_replace-me"
dotnet user-secrets set "Payments:Flutterwave:WebhookSecret" "replace-with-sandbox-webhook-secret"
dotnet user-secrets set "Payments:Provider" "Paystack"
dotnet user-secrets set "Tickets:SigningKey" "replace-with-at-least-32-random-characters"
```

Keep the QR signing key stable; rotating it invalidates outstanding tickets.
Paid-event creation is rejected while
`Payments:OrganizerSubaccountsEnabled=false`, which is the tracked safe default.

## Development-only seed accounts

Running the API with `ASPNETCORE_ENVIRONMENT=Development` idempotently creates:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@dev.local` | `Dev-Admin-Password-123!` |
| Organizer | `organizer@dev.local` | `Dev-Organizer-Password-123!` |

The Organizer follows the same promotion service used by the application. The
seed is guarded by the Development environment and does not run in Production.

## Mock frontend mode

Set `VITE_USE_MOCK_API=true` only when running `npm run dev`. Mock mode is removed
from production builds and does not exercise the .NET API, PostgreSQL, provider
credentials, migrations, or webhook verification.

## Dependency audits

Run current audits rather than relying on a captured historical report:

```bash
npm audit
dotnet list backend/EventManagement.slnx package --vulnerable --include-transitive
```

CI fails for new high or critical findings. Its narrowly reviewed React Router
RSC-only exception is encoded in `.github/workflows/ci.yml`; this client-rendered
Vite application does not enable React Server Components. Reassess the exception
whenever React Router is updated, and never use `npm audit fix --force` without
reviewing the proposed dependency changes.
