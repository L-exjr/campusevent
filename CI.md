# Continuous integration

The GitHub Actions workflow in `.github/workflows/ci.yml` runs on every push to
`main` and every pull request targeting `main`. It is CI only: it does not deploy,
publish, or modify any external environment.

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
email or call live storage. Email failure isolation is exercised without an SMTP
provider, and storage tests use local test doubles.

GitHub does not expose repository secrets to workflows triggered from untrusted
forks. The backend job will therefore fail during service startup or secret
validation until a trusted maintainer runs it in a context where the test-only
secrets are available; the workflow will not fall back to an unprotected database
or key. The independent frontend job can still run.
