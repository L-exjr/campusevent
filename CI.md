# Continuous integration

The GitHub Actions workflow in `.github/workflows/ci.yml` runs on every push to
`main` and every pull request targeting `main`. Test jobs run for both event types;
production deployment jobs run only for a push to `main` after both test jobs pass.

The two jobs run independently and in parallel:

- **Backend tests** starts a health-checked PostgreSQL 16 service, validates the
  required test-only secrets, restores and audits NuGet dependencies, builds the
  .NET 10 solution in Release mode, and runs all unit and integration tests. The
  integration fixture applies migrations and truncates application tables, so
  the connection must point only to the disposable CI database.
- **Frontend tests** installs the lockfile exactly with `npm ci`, audits npm
  dependencies, runs the existing ESLint configuration, executes the Vitest/RTL
  suite, and creates a production Vite build.

Both audit steps fail on high or critical findings. The npm gate permits only the
previously reviewed React Router RSC advisory `GHSA-qwww-vcr4-c8h2` at high
severity; a different advisory or a critical reclassification fails CI. The
NuGet baseline is empty, so any high or critical NuGet advisory fails CI. Invalid
or unavailable audit reports also fail rather than silently passing.

## Required GitHub Actions secrets

Add these under **Repository settings → Secrets and variables → Actions** before
running the workflow:

| Secret | Purpose |
| --- | --- |
| `TEST_DB_PASSWORD` | Password used only by the PostgreSQL CI service container. |
| `TEST_DB_CONNECTION_STRING` | Full Npgsql connection string for that same service, using `Host=localhost;Port=5432;Database=event_management_tests;Username=postgres;Password=<TEST_DB_PASSWORD>`. |
| `TEST_JWT_SIGNING_KEY` | Test-only JWT HMAC key containing at least 32 characters. It must not be reused in Development or Production. |

Use separate, clearly disposable values. Never copy a production database
password, JWT key, Mailtrap credential, or Supabase credential into these
secrets. Mailtrap and Supabase secrets are not required: CI does not send external
email or call live storage. Email failure isolation is exercised without a live API
provider, and storage tests use local test doubles.

GitHub does not expose repository secrets to workflows triggered from untrusted
forks. The backend job will therefore fail during service startup or secret
validation until a trusted maintainer runs it in a context where the test-only
secrets are available; the workflow will not fall back to an unprotected database
or key. The independent frontend job can still run.

## Production deployment

`deploy-backend` declares `needs: [backend-tests, frontend-tests]`, and
`deploy-frontend` additionally needs `deploy-backend`. They cannot start when a
test fails or is cancelled, and the frontend is not promoted before the Railway
API is healthy. Their additional event/ref condition permits only a push to
`refs/heads/main`, never a pull request or feature branch.

Railway and Vercel native production Git integrations must be disabled for this
repository. GitHub Actions is the single production deployment path, avoiding
duplicate deploys and ensuring every release passes the existing gates. Hosted
PR preview deployments are also disabled to honor the no-PR-deploy constraint;
pull requests still receive the frontend production-build check.

### Deployment secrets

Add these under **Repository settings → Secrets and variables → Actions**:

| Secret | Where to obtain it |
| --- | --- |
| `RAILWAY_TOKEN` | Railway project **Settings → Tokens**; create a project token scoped to the production environment. |
| `RAILWAY_SERVICE` | Railway API service name (`ems-api`). This is an identifier rather than a credential, but it is kept with deployment configuration in Actions Secrets. |
| `VERCEL_TOKEN` | Vercel account **Settings → Tokens**; scope it to the owning team’s **All Projects** so the CLI can retrieve project settings during `vercel pull`. |
| `VERCEL_ORG_ID` | Vercel project/team metadata, available after `vercel link` in `.vercel/project.json`. Copy only the value to the GitHub secret; do not commit `.vercel`. |
| `VERCEL_PROJECT_ID` | Vercel project metadata, available after `vercel link` in `.vercel/project.json`. |

The Railway job uses a project-scoped token and `railway up` in attached mode.
The tracked Docker image runs pending EF migrations before exposing `/health`, so
a failed migration prevents Railway from activating the deployment. The Vercel
job pulls Production environment configuration, builds with Vercel CLI, and
uploads one prebuilt production artifact.

Platform application secrets such as database passwords, JWT keys, SMTP
credentials, Supabase variables, and Google OAuth configuration belong in the
Railway/Vercel environment dashboards described in [DEPLOYMENT.md](DEPLOYMENT.md),
not GitHub Actions Secrets unless a workflow directly requires them.

### Manual rollback

- In Railway, select the previous known-good API deployment and choose
  **Rollback/Redeploy**. Database migrations are not automatically reversed;
  prefer a forward corrective migration, since an application rollback does not
  undo schema changes.
- In Vercel, select the previous known-good deployment and choose **Promote to
  Production**, then revert the bad commit on `main` so later deployments do not
  restore it.
