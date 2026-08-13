# Testing the API

Use the .NET 10 SDK. The integration suite requires either local PostgreSQL
server binaries (`initdb` and `postgres`) or an explicitly configured disposable
PostgreSQL database.

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

## Frontend tests

The React suite uses Vitest, React Testing Library, `user-event`, jsdom, and MSW.
Tests live in `src/tests/`, mirroring the corresponding `src/` component or page
path. Run them from the repository root:

```bash
npm ci
npm test
```

For watch mode while developing:

```bash
npm run test:watch
```

The shared MSW server is defined in `src/tests/mocks/server.ts`, with reusable
happy-path handlers in `src/tests/mocks/handlers.ts` and API-shaped fixtures in
`src/tests/mocks/fixtures.ts`. Individual tests call `server.use(...)` to override
only the endpoint or failure state relevant to that scenario. Unhandled network
requests fail the test, ensuring page tests continue exercising the real API
adapter instead of silently reaching a backend or manually stubbed `fetch`.

Role-routing and navigation tests use `renderWithAuth` from
`src/tests/testUtils.tsx` to inject a focused mocked `AuthContext`. Integration-style
page tests combine that context with MSW so routing, request serialization,
response mapping, and visible UI updates are exercised together.

Run the complete frontend gate before handing off UI work:

```bash
npm run lint
npm test
npm run build
```

Shared feedback components, authentication validation, mobile navigation, and
major page flows have focused component/page tests. Visual browser checks remain
necessary for responsive layout, backdrop blur, contrast, focus indicators, and
reduced-motion behavior because jsdom does not render those effects.
