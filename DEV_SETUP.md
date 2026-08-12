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

The current Google Identity Services flow verifies an ID token and does not use a
client secret or redirect URI. Restart Vite after changing `.env.local`.

## Paystack and signed tickets

The API needs test secrets to exercise paid checkout and QR tickets:

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Payments:Paystack:SecretKey" "sk_test_replace-me"
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
