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
| `Email__Smtp__Host` | Production SMTP hostname. |
| `Email__Smtp__Port` | Production SMTP port. |
| `Email__Smtp__Username` | Production SMTP username. |
| `Email__Smtp__Password` | Production SMTP password. |
| `Email__Smtp__FromAddress` | Verified sender address. |
| `Email__Smtp__FromName` | Sender display name. |
| `Email__Smtp__EnableSsl` | Enables SMTP TLS. |

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
| `VITE_SUPABASE_URL` | Existing Supabase project URL. |
| `VITE_SUPABASE_ANON_KEY` | Browser-safe publishable/anon key, never `service_role`. |
| `VITE_GOOGLE_CLIENT_ID` | Google OAuth web client ID. |

Vite embeds these values at build time. `httpClient.ts` uses localhost only while
`import.meta.env.DEV` is true and throws if a production runtime lacks
`VITE_API_BASE_URL`, preventing a deployed app from silently calling localhost.
Add the final Vercel origin to the Google client's authorized JavaScript origins
and to Railway's explicit CORS list.

The production GitHub job runs `vercel pull --environment=production`,
`vercel build --prod`, and `vercel deploy --prebuilt --prod`. Store
`VERCEL_TOKEN`, `VERCEL_ORG_ID`, and `VERCEL_PROJECT_ID` only in GitHub Actions
Secrets.

### Preview tradeoff

Hosted PR preview deployments are intentionally disabled. The CD constraint says
deployments must never run for pull requests or feature branches, while Vercel's
native Git integration would deploy independently of this workflow and could
bypass its test gates. Pull requests still run the exact Vite production build
in `frontend-tests`.

If hosted previews are introduced later, create a separate Railway staging API
and database, configure Vercel Preview variables with that API URL, and add its
stable origin to staging CORS. Do not point previews at production and do not add
wildcard Vercel preview origins.

## First deployment and verification

1. Merge only additive, reviewed EF migrations into `main`.
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
6. Upload one event/profile image and verify it is readable from the configured
   Supabase bucket.
7. Request a password reset or trigger another notification and confirm the
   message reaches the configured Mailtrap inbox or production provider.
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

## Railway plan expectations

Railway currently describes Free as experimentation with a small monthly credit
and Hobby as a paid `$5/month` minimum that includes the first `$5` of resource
usage. Current documented per-service limits include 0.5 GB RAM, 1 vCPU, 1 GB
ephemeral storage, and 0.5 GB volume storage on Free; Hobby allows larger compute,
100 GB ephemeral storage, and 5 GB volume storage by default. Usage above included
credit is billed, so configure usage alerts/hard limits and monitor PostgreSQL
volume consumption. App sleeping is an optional cost-control feature; enabling it
introduces wake-up latency/cold starts. Database backups and restore testing remain
an operational responsibility.

Current platform references: [Railway PostgreSQL](https://docs.railway.com/databases/postgresql),
[Railway Dockerfiles](https://docs.railway.com/builds/dockerfiles),
[Railway healthchecks](https://docs.railway.com/deployments/healthchecks),
[Railway plans](https://docs.railway.com/pricing/plans), and
[Vercel GitHub Actions deployments](https://vercel.com/docs/git/vercel-for-github).
