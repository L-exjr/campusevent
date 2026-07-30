# Testing the API

The solution has two independent xUnit test projects:

- `EventManagement.Api.UnitTests/` contains fast service/controller tests. The
  ownership policy is tested as a role/owner matrix, and Moq verifies that HTTP
  controllers pass the authenticated actor identity and role to the service layer.
- `EventManagement.Api.IntegrationTests/` sends real HTTP requests through
  `WebApplicationFactory<Program>` and uses the Npgsql EF Core provider against
  PostgreSQL. It covers JWT validation, role authorization, organizer application
  review, event ownership, duplicate/capacity registration, concurrency, and
  attendance integrity.

## Run locally

From `backend/`:

```bash
dotnet restore EventManagement.slnx
dotnet test EventManagement.slnx
```

The integration fixture first looks for local `initdb` and `postgres` executables.
When they are available, it creates a disposable PostgreSQL cluster under the OS
temporary directory, applies migrations, and deletes the cluster after the run.

Alternatively, point the suite at a dedicated PostgreSQL test database:

```bash
export EMS_TEST_POSTGRES='Host=localhost;Port=5432;Database=event_management_tests;Username=postgres;Password=postgres'
dotnet test EventManagement.Api.IntegrationTests/EventManagement.Api.IntegrationTests.csproj
```

`EMS_TEST_POSTGRES` must never reference a production or shared development
database. The suite truncates all application tables between tests. Database
pooling is disabled so teardown and per-test isolation are deterministic.

`appsettings.Testing.json` contains only non-secret test defaults. The fixture
provides a random JWT signing key and the test database connection through
environment variables for the lifetime of the test host.

To run only the fast unit tests:

```bash
dotnet test EventManagement.Api.UnitTests/EventManagement.Api.UnitTests.csproj
```
