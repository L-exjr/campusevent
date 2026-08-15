# Production deployment

The production topology is a Railway ASP.NET Core service plus Railway
PostgreSQL, with the Vite frontend hosted by Vercel. GitHub Actions is the only
production deployment trigger. Disconnect Railway/Vercel production Git
autodeploys so a push cannot bypass the test gates or cause a duplicate deploy.

## Railway backend and PostgreSQL

### Provisioning

1. Create a Railway project and a `production` environment.
2. Add PostgreSQL from **New → Database → PostgreSQL**. Railway exposes
   `PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD`, `PGDATABASE`, and `DATABASE_URL`.
   `DATABASE_URL` uses the standard URI shape
   `postgresql://<user>:<password>@<host>:<port>/<database>`.
3. Add an empty service for the API and generate its public HTTPS domain.
4. Generate a project token scoped to the production environment. Add it to
   GitHub Actions as `RAILWAY_TOKEN`; add the API service name as
   `RAILWAY_SERVICE`.

Use Railway reference variables to keep the API-to-database connection on the
project's private network. `ConnectionStrings__DefaultConnection` must be an
Npgsql keyword connection string assembled from the Postgres service variables:

```text
Host=<PGHOST>;Port=<PGPORT>;Database=<PGDATABASE>;Username=<PGUSER>;Password=<PGPASSWORD>;SSL Mode=Require
```

In Railway, each placeholder should be a reference to the corresponding
PostgreSQL service variable, not a copied credential. Do not pass `DATABASE_URL`
directly as `ConnectionStrings__DefaultConnection`: Npgsql's documented format
is the keyword/value form above.

### Required Railway variables

Configure these on the API service. Names are shown; secret values are never
stored in this repository.

| Variable | Required use |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Must be `Production`. The Docker image also defaults to Production as a safety net. |
| `ConnectionStrings__DefaultConnection` | Npgsql connection string built from Railway Postgres reference variables. |
| `Database__ApplyMigrations` | Enables the guarded startup migration step. |
| `Jwt__SigningKey` | Unique production HMAC key of at least 32 characters. |
| `Jwt__Issuer` | Production JWT issuer. |
| `Jwt__Audience` | Production frontend audience. |
| `Jwt__ExpiryMinutes` | JWT lifetime; the application clamps it to 60–90 minutes. |
| `Cors__AllowedOrigins__0` | Exact Vercel production origin, including `https://` and no path. |
| `Frontend__BaseUrl` | Exact Vercel production origin used in password-reset links. |
| `Google__ClientId` | Google OAuth web client ID; must match the Vercel client ID. |
| `Google__ClientSecret` | Store here if Google issued one; reserved for a future authorization-code flow and unused by the current ID-token flow. |
| `DemoData__Enabled` | Optional KNUST demonstration dataset switch. Set it to `true` and redeploy/restart Railway to seed idempotently, verify the rows, then set it back to `false`. Upcoming demo events are published; historical records remain drafts. |
| `DemoData__Password` | Optional 12+ character password for the deliberately created demo accounts; do not reuse a real password. |
| `SUPABASE_URL` | Supabase project URL used only by the backend upload proxy. |
| `SUPABASE_SERVICE_ROLE_KEY` | Server-only Supabase service-role key. Never use a `VITE_*` name or expose it to the browser. |
| `PAYSTACK_SECRET_KEY` | Server-only Paystack key. Production requires `sk_live_`; staging requires its own `sk_test_` key. |
| `PAYMENTS_PENDING_MINUTES` | Optional payment-reservation lifetime; defaults to 15 and is clamped to 5–60 minutes. |
| `Payments__OrganizerSubaccountsEnabled` | Keep `false` until every paid organizer is routed to a verified Paystack subaccount. Paid-event creation is rejected while false. |
| `Payments__PaystackGhanaProcessingFeeBasisPoints` | Fee disclosure used by the UI; tracked default is `195` (1.95%). Re-verify with Paystack before each production release. |
| `Payments__PlatformFeeBasisPoints` | Project platform fee; tracked default is `0`. Changing this is a business-policy decision. |
| `Payments__SettlementSchedule` | Organizer-facing settlement disclosure; tracked default is `AutomaticNextWorkingDay`. Confirm that value against the production Paystack account before launch. |
| `QR_SIGNING_KEY` | Stable random secret of at least 32 characters. Rotation invalidates every outstanding QR ticket and requires a re-issue plan. |
| `CERTIFICATES_BUCKET` | Private Supabase bucket for attendee certificate PDFs; use `certificates`. |
| `CERTIFICATE_SIGNED_URL_MINUTES` | Signed certificate URL lifetime; use 60. |
| `CERTIFICATE_TEMPLATE_VERSION` | Positive template version embedded in certificate object keys; increment only when intentionally regenerating under a new version. |
| `EMAIL_PROVIDER` | Set `Gmail` in production; keep `Mailtrap` in local/dev unless intentionally testing Gmail. |
| `GMAIL_SMTP_HOST` / `GMAIL_SMTP_PORT` | Gmail SMTP endpoint; `smtp.gmail.com` and `587`. |
| `GMAIL_SMTP_USERNAME` | Gmail account used for SMTP authentication. |
| `GMAIL_APP_PASSWORD` | Google App Password created after enabling 2-Step Verification; never use the account password. |
| `GMAIL_SENDER_EMAIL` / `GMAIL_SENDER_NAME` | Approved From address and display name. |
| `GMAIL_DAILY_WARNING_THRESHOLD` | Per-process accepted-send warning threshold; defaults to 400. |
| `MAILTRAP_API_TOKEN` | Required only when `EMAIL_PROVIDER=Mailtrap`. Store it only in Railway variables. |
| `MAILTRAP_SENDER_EMAIL` / `MAILTRAP_SENDER_NAME` | Mailtrap sender configuration, required only for the Mailtrap provider. |

Optional production tuning uses normal ASP.NET Core double-underscore keys. The
tracked defaults are safe starting points: `AuthRateLimiting__Ip__Login__PermitLimit=30`,
`AuthRateLimiting__Account__Login__PermitLimit=8`,
`Images__UploadRateLimit__PermitLimit=10`, `Email__Outbox__BatchSize=50`,
`Email__Outbox__PollIntervalSeconds=15`, and
`Images__Cleanup__PendingRetentionHours=24`,
`DataRetention__BookingRequests__ClosedRetentionDays=90`, and
`DataRetention__AdminAuditLogs__RetentionDays=365`. Rate-limit counters, email claims,
and image cleanup claims are stored in PostgreSQL, so these controls remain
consistent when the Railway service has multiple replicas.

Railway injects `PORT`; do not create or override it. The container enables
ASP.NET Core forwarded-header processing because Railway terminates TLS before
forwarding traffic to Kestrel. Additional allowed origins use sequential keys
such as `Cors__AllowedOrigins__1`. Never use `AllowAnyOrigin()`; besides being
unnecessary here, wildcard origins cannot be combined with credentialed CORS if
authentication later moves to cookies.

Leave `BootstrapAdmin__Email`, `BootstrapAdmin__Password`, and
`BootstrapAdmin__Name` unset for a fresh production database. The fixed
Admin/Organizer seed runs only when `ASPNETCORE_ENVIRONMENT=Development`, so it
cannot run with the required Production setting.

### Container and migrations

Railway uses the repository's [Dockerfile](Dockerfile), selected explicitly by
[railway.json](railway.json). A Dockerfile is preferred over buildpack detection
because it pins the .NET 10 SDK/runtime, publishes only the API, includes email
templates deterministically, runs as the .NET image's non-root user, and binds
to Railway's injected `PORT`.

The stages are:

1. `build`: restore the API project and publish a Release framework-dependent
   application.
2. `runtime`: copy only published output into the smaller ASP.NET Core image,
   select Production, run as non-root, and launch Kestrel on `PORT`.

`Database__ApplyMigrations` makes `Program.cs` call `Database.MigrateAsync()`
before middleware, routes, health checks, or seed logic become available. A
failed migration terminates startup, `/health` never returns 200, and Railway
marks the deployment failed instead of routing traffic to a mismatched schema.
EF's PostgreSQL migration lock serializes concurrent migration attempts. At this
project's scale, keep the API at one replica while migrating. Every migration
must still be reviewed as additive before merge; this mechanism never drops or
recreates the database by itself.

The GitHub Actions Railway CLI command remains attached until Railway completes
the deployment and healthcheck. Do not also enable Railway GitHub autodeploys.

## Vercel frontend

Create a Vercel project from this repository without enabling production Git
autodeploys. The tracked [vercel.json](vercel.json) selects Vite, runs
`npm run build`, publishes `dist`, and rewrites SPA routes to `index.html`.

Configure these variables for Vercel's **Production** environment:

| Variable | Required use |
| --- | --- |
| `VITE_API_BASE_URL` | Railway public HTTPS API URL ending in `/api`. |
| `VITE_USE_MOCK_API` | Production must not enable mock mode. |
| `VITE_GOOGLE_CLIENT_ID` | Google OAuth web client ID. |

Do not mark `VITE_*` variables as **Sensitive** in Vercel. Vercel masks
sensitive build values as `[SENSITIVE]`, while Vite must embed these
browser-visible settings in the production bundle. Only browser-safe,
publishable values belong in `VITE_*` variables.

Vite embeds these values at build time. `httpClient.ts` uses localhost only while
`import.meta.env.DEV` is true and throws if a production runtime lacks
`VITE_API_BASE_URL`, preventing a deployed app from silently calling localhost.
Add the final Vercel origin to the Google client's authorized JavaScript origins
and to Railway's explicit CORS list.

The production GitHub job runs `vercel pull --environment=production`,
`vercel build --prod`, and `vercel deploy --prebuilt --prod`. Store
`VERCEL_TOKEN`, `VERCEL_ORG_ID`, and `VERCEL_PROJECT_ID` only in GitHub Actions
Secrets.

### Preview and staging isolation

Manual Vercel Preview deployments use the separate Railway `staging` environment
and its separate PostgreSQL database. Vercel Preview scope must set
`VITE_API_BASE_URL=https://ems-api-staging.up.railway.app/api`; Production scope
continues to use the production API. Each preview's exact origin must be added to
staging CORS and `Frontend__BaseUrl` before testing it. Never point previews at
production and never add wildcard Vercel preview origins.

Automatic PR/feature-branch deployment remains disabled, so a preview is an
intentional release-candidate action after local checks. Keep
`DemoData__Enabled=false` except for a deliberate, temporary staging seed, and
remove `DemoData__Password` immediately after the test. Staging paid-lifecycle
testing requires a dedicated `sk_test_` Paystack key; never copy the production
live key into staging.

## First deployment and verification

1. Merge only additive, reviewed EF migrations into `main`. The email outbox,
   authentication, booking, image lifecycle, payments, certificates, voting,
   event format, and sales-window migrations must all be present in the reviewed
   deployment artifact. Never generate a migration during deployment.
2. Confirm both GitHub test jobs pass and both deploy jobs succeed.
3. In Railway logs, confirm `EF Core database migrations are up to date`, the
   Production environment, and a successful `/health` deployment check.
4. Query production PostgreSQL before registration:

   ```sql
   SELECT COUNT(*) FROM "Users";
   SELECT "Email" FROM "Users"
   WHERE "Email" IN ('admin@dev.local', 'organizer@dev.local');
   ```

   A fresh database should return zero users and no development seed rows.
5. Register a real Student, log in, and open a JWT-protected Student route from
   the Vercel origin. Confirm the browser has no CORS errors and the API returns
   the expected authenticated response.
6. Upload one event/profile image and verify the browser sends multipart data to
   `/api/uploads/*`, the authenticated API writes it to Supabase, and the returned
   public URL is readable. Remove all Supabase `anon` INSERT/UPDATE/DELETE storage
   policies after the backend service-role configuration is deployed.
7. Request a password reset or trigger another notification. Confirm that the
   provider selected by `EMAIL_PROVIDER` accepts the message, the outbox marks it
   delivered, and no credential or raw provider response appears in logs. For
   Mailtrap, use a recipient permitted by the selected domain/account. For Gmail,
   verify the App Password and sender account before testing.
8. Test Google sign-in using the production Vercel origin.

These live checks require the platform projects, domains, and credentials; they
cannot be completed from a source-only checkout.

## Rollback

- **Railway:** open the API service's Deployments view, select the last known-good
  deployment, and choose **Rollback/Redeploy**. Do not automatically roll back a
  database migration. Prefer a forward corrective migration; use a reviewed EF
  `database update <target>` only when its `Down()` is proven data-safe.
- **Vercel:** open the project Deployments view, select the last known-good
  production deployment, and **Promote to Production**. Then revert the bad Git
  commit so the next main deployment remains consistent.

## Capacity and cost controls

Railway plan limits and pricing change independently of this repository. Review
the current plan before launch, configure usage alerts or hard limits where
available, monitor PostgreSQL storage, and test database restores. Enabling a
sleeping/cold-start option can add latency and must be tested against the API
healthcheck and the frontend's loading behavior.

Current platform references: [Railway PostgreSQL](https://docs.railway.com/databases/postgresql),
[Railway Dockerfiles](https://docs.railway.com/builds/dockerfiles),
[Railway healthchecks](https://docs.railway.com/deployments/healthchecks),
[Railway plans](https://docs.railway.com/pricing/plans), and
[Vercel GitHub Actions deployments](https://vercel.com/docs/git/vercel-for-github).
