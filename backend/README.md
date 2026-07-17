# Event Management API

ASP.NET Core 10 Web API using Entity Framework Core 10, PostgreSQL through Npgsql, PBKDF2 password hashing, and short-lived JWT bearer authentication.

## Security rules

- Public registration always creates a `Student`. The register contract has no role field.
- Passwords are stored in a versioned `PBKDF2-HMAC-SHA256` format with a unique 128-bit random salt, a 256-bit derived key, and 600,000 iterations. Existing `v1` hashes are upgraded after the next successful login.
- Password verification uses a constant-time hash comparison. Unknown accounts still perform one full PBKDF2 derivation to reduce email-enumeration timing differences.
- A Student becomes an `Organizer` only after Admin approval of an application or direct Admin promotion.
- Organizer ownership is checked again in `EventService` for every update, delete, registrant, and attendance operation.
- Active account state and the current database role are checked for every authenticated request. A deactivated user or stale role token receives `401`.
- JWTs expire after 75 minutes. Refresh tokens are intentionally not implemented. The client should return to login when a token expires or when a `401` reports a role change. A production follow-up should add rotating, revocable refresh tokens stored in secure HTTP-only cookies.
- Event registration uses a serializable database transaction for capacity enforcement and a unique `(EventId, StudentId)` database index for duplicate prevention.
- A filtered unique index prevents more than one pending organizer application per Student.

All API failures use:

```json
{ "error": "Human-readable explanation." }
```

## Configuration

Production secrets are not committed. Supply configuration through environment variables or your deployment secret store:

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=event_management;Username=postgres;Password=your-password'
export Jwt__Key='replace-with-at-least-32-random-characters'
export Jwt__Issuer='EventManagement.Api'
export Jwt__Audience='EventManagement.Frontend'
export BootstrapAdmin__Email='admin@cevents.com'
export BootstrapAdmin__Password='a-strong-initial-password'
export BootstrapAdmin__Name='System Administrator'
```

The bootstrap Admin is created only when both bootstrap email and password are configured and no account already uses that email. Remove the bootstrap password from the environment after the first successful startup.

For local development, `appsettings.Development.json` contains a passwordless local PostgreSQL example and a development-only JWT key. Do not reuse either in production.

## Database and startup

```bash
cd backend/EventManagement.Api
dotnet restore
dotnet ef database update
dotnet run
```

The Code-First migration is under `EventManagement.Api/Data/Migrations/`.

## Endpoints

Every list response contains `items`, `page`, `pageSize`, `totalCount`, and `totalPages`. Page size is capped at 100.

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Public | Create a Student account and issue a 75-minute JWT |
| POST | `/api/auth/login` | Public | Validate credentials and issue a 75-minute JWT |
| POST | `/api/organizer-applications` | Student | Submit an Organizer application |
| GET | `/api/organizer-applications/mine` | Student | View the current Student's latest application and review status |
| GET | `/api/organizer-applications?status=Pending&page=1&pageSize=20` | Admin | Paginated application queue |
| PUT | `/api/organizer-applications/{id}/approve` | Admin | Approve and promote the applicant |
| PUT | `/api/organizer-applications/{id}/reject` | Admin | Reject with an optional reason |
| GET | `/api/users?search=&role=&isActive=&page=1&pageSize=20` | Admin | Paginated user management list |
| PUT | `/api/users/{id}/role` | Admin | Promote to Organizer or demote to Student |
| PUT | `/api/users/{id}/deactivate` | Admin | Soft-disable an account |
| GET | `/api/events?search=&category=&from=&to=&page=1&pageSize=20` | Public | Paginated filtered event list |
| GET | `/api/events/{id}` | Public | Event detail |
| GET | `/api/events/mine?page=1&pageSize=20` | Organizer, Admin | Events created by the current user |
| POST | `/api/events` | Organizer, Admin | Create an event |
| PUT | `/api/events/{id}` | Owner Organizer, Admin | Update an event |
| DELETE | `/api/events/{id}` | Owner Organizer, Admin | Delete an event and its registrations |
| POST | `/api/events/{id}/register` | Student | Register with duplicate/capacity enforcement |
| GET | `/api/events/{id}/registrants` | Owner Organizer, Admin | View registrants |
| PUT | `/api/events/{id}/attendance` | Owner Organizer, Admin | Bulk-update attendance |
| GET | `/api/students/{id}/registrations` | Same Student only | View own registrations |
| GET | `/api/reports/summary` | Admin | Totals and overall attendance rate |
| GET | `/api/reports/events/{id}` | Admin | Per-event registration and attendance |
| GET | `/api/reports/organizers` | Admin | Activity grouped by Organizer |

Authenticated requests use `Authorization: Bearer <token>`.

## Sample requests and responses

Registering cannot specify a role:

```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "Maya Johnson",
  "email": "maya@example.edu",
  "password": "StrongPass123!"
}
```

Registration returns the same authenticated session shape as login, so the new Student is signed in immediately.

Login:

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "maya@example.edu", "password": "StrongPass123!" }
```

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-07-16T17:15:00+00:00",
  "user": {
    "id": "4e08bf35-6263-4afd-981a-365026d20dd1",
    "name": "Maya Johnson",
    "email": "maya@example.edu",
    "role": "Student",
    "isActive": true,
    "createdAt": "2026-07-16T16:00:00+00:00"
  }
}
```

The JWT contains `sub` and `userId` claims for the user ID plus a `role` claim used by API authorization.

Organizer application:

```json
{
  "reason": "I coordinate the computing society and want to publish our workshops and manage attendance centrally."
}
```

Create or update an event:

```json
{
  "title": "Future of AI Symposium",
  "description": "A practical afternoon exploring responsible AI and emerging research.",
  "date": "2026-09-18T14:00:00Z",
  "location": "Innovation Hall",
  "capacity": 120,
  "category": "Technology"
}
```

Bulk attendance:

```json
{
  "registrations": [
    { "registrationId": "23a44c62-14f7-414a-9a82-c953b9ef653d", "attended": true },
    { "registrationId": "ff943430-16f6-4cba-82f6-9befd5937962", "attended": false }
  ]
}
```

Paginated list shape:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

## Integration tests

The integration suite starts the real API in memory and exercises it against a temporary PostgreSQL cluster. PostgreSQL command-line binaries (`initdb` and `postgres`) must be available on `PATH`:

```bash
dotnet test EventManagement.slnx
```

CI or environments without local PostgreSQL binaries can point the suite at a dedicated, disposable PostgreSQL database. The suite applies migrations and truncates all EMS tables, so never use an application or shared database:

```bash
EMS_TEST_POSTGRES='Host=localhost;Port=5432;Database=event_management_tests;Username=postgres;Password=...' dotnet test EventManagement.slnx
```

## Out of scope

Payments, certificates, QR-code attendance, email notifications, and refresh-token issuance are intentionally not implemented in this version.
