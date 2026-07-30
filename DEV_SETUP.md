# Local development setup

## Supabase Storage client

The workspace uses the already-created Supabase project only for Storage. The
application continues to use its ASP.NET Core JWTs and EF Core/PostgreSQL database.

1. Sign in to the [Supabase dashboard](https://supabase.com/dashboard), select the
   correct organization, and open the existing Event Management System project.
   Do not click **New project** for this setup.
2. Click **Connect** in the project toolbar, then choose **App Frameworks**.
3. Copy **Project URL** and **Publishable key**. A legacy project may instead show
   its browser key under **Settings → API Keys → Legacy API Keys → anon**.
   Never copy a secret or `service_role` key into this frontend.
4. The URL is also available at **Integrations → Data API → Project URL**. Keys
   are also available at **Settings → API Keys**; use a publishable key (or the
   legacy `anon` key), never a secret or legacy `service_role` key.
5. Copy `.env.example` to `.env` and populate these browser variables:

   ```dotenv
   VITE_SUPABASE_URL=https://your-project-ref.supabase.co
   VITE_SUPABASE_ANON_KEY=your-publishable-or-anon-key
   ```

`.env` is ignored by Git and `.env.example` intentionally contains empty values.
The publishable/anon key is designed to be exposed in a browser; its permissions
must be constrained with Storage policies. See [SUPABASE_SETUP.md](SUPABASE_SETUP.md)
for the two bucket definitions and policies.

An offline Vitest smoke test verifies client construction. After filling `.env`
and creating the buckets, run the live Storage check:

```bash
npm test -- src/tests/api/supabaseClient.test.ts
npm run supabase:smoke
```

Expected live output:

```text
✓ Connected to event-images
✓ Connected to profile-images
Supabase Storage client initialized and both buckets are reachable.
```

## Mailtrap email sandbox

Create or sign in to a Mailtrap account, then open **Email Testing → Sandboxes →
My Sandbox → Integration → SMTP**. Copy that sandbox's host, port, username, and
password. Mailtrap's current walkthrough is available in its
[Sandbox SMTP Integration documentation](https://docs.mailtrap.io/email-sandbox/setup/sandbox-smtp-integration).

From `backend/EventManagement.Api`, store the values with .NET User Secrets using
the commands in [SECURITY.md](SECURITY.md#smtp-credentials). Do not paste them into
an `appsettings` file. Start the API in Development and exercise event registration
or an Admin application decision; messages addressed to the test Student email
will appear only in the Mailtrap sandbox inbox. The API intentionally rejects any
non-Mailtrap SMTP host in Development.

The reminder worker checks hourly by default and sends once when a registered
event is within 24 hours. The non-secret cadence can be changed with
`Email:Reminders:LeadTimeHours` and `Email:Reminders:CheckIntervalMinutes`.

## Google sign-in

1. In [Google Cloud Console](https://console.cloud.google.com/apis/credentials),
   create or select a development project and configure its OAuth consent screen.
2. Create an **OAuth client ID** with application type **Web application**.
3. Add `http://localhost:5173` under **Authorized JavaScript origins**. This app uses
   the Google Identity Services JavaScript callback and does not need a redirect URI.
4. Put the web client ID in the frontend's ignored `.env`:

   ```dotenv
   VITE_GOOGLE_CLIENT_ID=your-web-client-id.apps.googleusercontent.com
   ```

5. Configure the same audience for backend verification from
   `backend/EventManagement.Api`:

   ```bash
   dotnet user-secrets set "Google:ClientId" "your-web-client-id.apps.googleusercontent.com"
   ```

Restart Vite after changing `.env`. Do not put a Google client secret in the
frontend. The current ID-token flow does not need one; see [SECURITY.md](SECURITY.md#google-sign-in-configuration)
for the storage rule if a future server-side authorization-code flow introduces it.

## npm audit resolution

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

After that update, the advisory feed exposed a newer issue affecting React Router
RSC mode. This application does not use SSR, RSC mode, server actions, or React
Router's server runtime, so that code path is not reachable here. Nevertheless,
`npm audit` correctly continues to report the installed direct/transitive pair:

```text
# npm audit report

react-router  7.12.0 - 8.2.0
Severity: high
React Router: RSC Mode CSRF Bypass Allows Action Execution Before 400 Response - https://github.com/advisories/GHSA-qwww-vcr4-c8h2
fix available via `npm audit fix --force`
Will install react-router-dom@7.11.0, which is a breaking change
node_modules/react-router
  react-router-dom  >=7.12.0-pre.0
  Depends on vulnerable versions of react-router
  node_modules/react-router-dom

2 high severity vulnerabilities

To address all issues (including breaking changes), run:
  npm audit fix --force
```

At the time of this maintenance pass, `react-router-dom@7.18.2` is the latest
published DOM package and it pins `react-router@7.18.2`; the fixed core router is
8.3.x. The actual forced dry run proposed downgrading both packages to `7.11.0`,
which would reintroduce the older redirect/XSS advisory range resolved above:

```text
npm warn using --force Recommended protections disabled.
npm warn audit Updating react-router-dom to 7.11.0, which is a SemVer major change.
change react-router-dom 7.18.2 => 7.11.0
change react-router 7.18.2 => 7.11.0
```

Forcing that downgrade or overriding the transitive router with incompatible
8.3.x code was intentionally not done. Re-run `npm audit` and update both packages
together when a compatible patched `react-router-dom` is released. Until then,
the remaining advisory is accepted as a documented, unreachable RSC-only risk in
this client-rendered Vite application; the earlier reachable findings are fixed.

## Development accounts

Starting the API in the `Development` environment idempotently seeds:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@dev.local` | `Dev-Admin-Password-123!` |
| Organizer | `organizer@dev.local` | `Dev-Organizer-Password-123!` |

Passwords use the application's `IPasswordHasher`. The Organizer starts with the
normal Student role and is promoted through `UserService.UpdateRoleAsync`, the same
role mutation path used by an Admin. Both `Program.cs` and the seed method enforce
`Development`, and email lookups make repeated starts safe. Credentials are logged
only when each user is first created and only in Development; no seed credential
message is emitted on later starts or in Production.
