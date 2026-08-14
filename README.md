# Campus Events

[![CI](https://github.com/L-exjr/campusevent/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/L-exjr/campusevent/actions/workflows/ci.yml)

Campus Events is a production-oriented event management system for students,
organizers, and administrators. It combines a React single-page application with
an ASP.NET Core API, PostgreSQL persistence, provider-neutral Paystack/Flutterwave payments, and Supabase
object storage.

## Current documentation

- [Architecture](docs/architecture.md)
- [Features and end-to-end flows](docs/features.md)
- [Authorization model](docs/auth-model.md)
- [Payments](docs/payments.md)
- [API reference](docs/api-reference.md)
- [Development and deployment](docs/development.md)

## Stack

| Layer | Technology |
| --- | --- |
| Frontend | React 19, TypeScript 6, Vite 8, React Router 7, React-Bootstrap 2, Bootstrap 5 |
| Backend | ASP.NET Core 10 Web API, Entity Framework Core 10, Npgsql |
| Database | PostgreSQL 16 in CI; any PostgreSQL version supported by the tracked Npgsql provider locally |
| Storage | Supabase Storage for public event/profile images and private certificates |
| Payments | Configurable Paystack or Flutterwave hosted checkout with provider-specific verification and webhooks |
| Authentication | Short-lived JWT bearer sessions plus Google Identity Services |
| Testing | Vitest, React Testing Library, MSW, xUnit, Moq, and ASP.NET Core integration tests |
| Hosting | Vercel frontend, Railway API and PostgreSQL, GitHub Actions deployment gates |

## Main workflows

- Public visitors browse events and can submit an event-management booking request.
- Students register for free or paid events, use signed QR tickets, vote, download
  attendance-backed certificates, and apply for the Organizer role.
- Organizers manage their events, registrants, attendance, ticket scanning,
  assigned booking requests, and voting campaigns.
- Administrators review Organizer applications, manage users and events, triage
  booking requests, inspect operational queues, and view reports.

Ticketing currently supports one general-admission price and capacity per event.
Ticket tiers such as VIP, Couple, Family, or Table of 10 are not part of the
current API or database model.

## Repository layout

```text
.
├── src/                            React application
│   ├── api/                        API facade, HTTP client, real/mock adapters
│   ├── components/                 Shared layout, forms, feedback, and feature UI
│   ├── context/                    Authentication context
│   ├── pages/                      Public, Student, Organizer, and Admin routes
│   ├── tests/                      Vitest/RTL/MSW tests
│   ├── types/                      Frontend domain types
│   └── utils/                      Permissions and formatting helpers
├── backend/
│   ├── EventManagement.Api/        ASP.NET Core API and EF Core migrations
│   ├── EventManagement.Api.UnitTests/
│   └── EventManagement.Api.IntegrationTests/
├── contracts/                      Shared non-code contracts
├── Dockerfile                      Railway API image
├── railway.json                    Railway build and health-check settings
└── vercel.json                     Vite hosting, SPA rewrite, security headers
```

Frontend pages call only the facade in `src/api/index.ts`. The real adapter maps
ASP.NET contracts to frontend domain types; the optional mock implements the same
interface and is available only in Vite development mode.

## Prerequisites

- Node.js 24 and npm
- .NET 10 SDK
- PostgreSQL and the `dotnet-ef` tool
- Optional provider accounts for Supabase, Paystack, Mailtrap/Gmail, and Google

## Local setup

### 1. Install dependencies

```bash
npm ci
dotnet restore backend/EventManagement.slnx
```

### 2. Configure PostgreSQL and backend secrets

Create an empty local database, then configure the API from its project directory:

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=event_management;Username=postgres;Password=your-password"
dotnet user-secrets set "Jwt:SigningKey" "replace-with-at-least-32-random-characters"
dotnet ef database update
dotnet run
```

The API listens at the URL printed by `dotnet run`; the tracked frontend default
expects `http://localhost:5080/api`. If your API uses another port, set that URL
in the frontend configuration below.

### 3. Configure and run the frontend

Copy `.env.example` to `.env.local`, then keep only browser-visible `VITE_*`
values in `.env.local`:

```dotenv
VITE_API_BASE_URL=http://localhost:5080/api
VITE_USE_MOCK_API=false
VITE_GOOGLE_CLIENT_ID=
```

```bash
npm run dev
```

Open `http://localhost:5173`. Real API sessions are stored in `sessionStorage`;
expired or rejected JWTs clear the frontend session.

For frontend-only development, set `VITE_USE_MOCK_API=true`. Mock mode works only
under `npm run dev`; production builds always use the real API. The three mock
accounts use password `demo123`:

| Role | Email |
| --- | --- |
| Student | `student@cevents.com` |
| Organizer | `organizer@cevents.com` |
| Admin | `admin@cevents.com` |

The real API also seeds documented Development-only accounts; see
[DEV_SETUP.md](DEV_SETUP.md#development-only-seed-accounts).

### 4. Configure Supabase Storage when testing uploads

Supabase stores files only; PostgreSQL remains the application database and the
API remains the authentication authority. Create public `event-images` and
`profile-images` buckets plus a private `certificates` bucket, then store the
project URL and service-role key in backend User Secrets:

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Supabase:Url" "https://your-project-ref.supabase.co"
dotnet user-secrets set "Supabase:ServiceRoleKey" "your-service-role-key"
```

Never place the service-role key in a `VITE_*` variable. Exact bucket restrictions
and access rules are in [SUPABASE_SETUP.md](SUPABASE_SETUP.md).

## Quality checks

```bash
npm run lint
npm test
npm run build
dotnet test backend/EventManagement.slnx
```

Integration tests create a disposable local PostgreSQL cluster when PostgreSQL
binaries are available, or use `EMS_TEST_POSTGRES`. Never point that variable at
a shared or production database because the suite truncates application tables.

## Railway and Vercel production setup

Production uses three platform resources:

1. A Railway PostgreSQL service.
2. A Railway API service built from the tracked `Dockerfile` and monitored at
   `/health`.
3. A Vercel Vite project whose `VITE_API_BASE_URL` points to the Railway public
   API URL ending in `/api`.

On Railway, configure `ASPNETCORE_ENVIRONMENT=Production`, the Npgsql
`ConnectionStrings__DefaultConnection`, JWT/CORS/frontend settings, provider
secrets, and Supabase credentials. The image enables
`Database__ApplyMigrations=true` and applies pending EF migrations before the
health endpoint becomes available.

GitHub Actions is the repository's production deployment path. Disable native
production Git autodeploys in Railway and Vercel, then configure the repository
secrets described in [CI.md](CI.md). The complete Railway variable table,
Supabase configuration, rollout checks, and rollback procedure are in
[DEPLOYMENT.md](DEPLOYMENT.md).

## Configuration rules

- `VITE_*` values are embedded in browser bundles and must never contain secrets.
- Backend secrets belong in .NET User Secrets locally and Railway variables in
  deployed environments.
- `.env.example` is an inventory/template; do not copy its backend-only secret
  placeholders into frontend `.env.local`.
- Keep `VITE_USE_MOCK_API=false` in production.
- Paid-event creation remains disabled until organizer Paystack subaccount
  routing is explicitly enabled and configured.

## Further documentation

- [DEV_SETUP.md](DEV_SETUP.md) — provider configuration and local development details.
- [DEPLOYMENT.md](DEPLOYMENT.md) — Railway/Vercel production deployment.
- [SUPABASE_SETUP.md](SUPABASE_SETUP.md) — Storage buckets and access model.
- [SECURITY.md](SECURITY.md) — secret handling and security controls.
- [CI.md](CI.md) — test and deployment workflow.
- [backend/README.md](backend/README.md) — API behavior and endpoint reference.
- [backend/TESTING.md](backend/TESTING.md) — frontend and backend test suites.
