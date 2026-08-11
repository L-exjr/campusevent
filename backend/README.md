# Event Management API

ASP.NET Core 10 Web API using Entity Framework Core 10, PostgreSQL through Npgsql, PBKDF2 password hashing, and short-lived JWT bearer authentication.

## Security rules

- Public registration always creates a `Student`. The register contract has no role field.
- Passwords are stored in a versioned `PBKDF2-HMAC-SHA256` format with a unique 128-bit random salt, a 256-bit derived key, and 600,000 iterations. Existing `v1` hashes are upgraded after the next successful login.
- Password verification uses a constant-time hash comparison. Unknown accounts still perform one full PBKDF2 derivation to reduce email-enumeration timing differences.
- A Student becomes an `Organizer` only after Admin approval of an application or direct Admin promotion.
- Organizer ownership is checked again in `EventService` for every update, delete, registrant, and attendance operation.
- Active account state and the current database role are checked for every authenticated request. A deactivated user or stale role token receives `401`.
- JWTs expire after 75 minutes and carry the user's current session version. Password resets increment that version, immediately rejecting previously issued access tokens. Refresh tokens are intentionally not implemented; the client returns to login whenever an access token expires or the API rejects a stale session or role.
- Event registration uses a serializable database transaction for capacity enforcement and a unique `(EventId, StudentId)` database index for duplicate prevention.
- Registration confirmations, password resets, and organizer-application decisions are committed to the PostgreSQL email outbox in the same transaction as their domain changes. Message-specific handlers revalidate time-sensitive state, the dispatcher retries provider failures, and stored payloads are cleared after terminal delivery status.
- Event updates require the `version` returned by the latest event response. Concurrent stale edits receive `409` rather than silently overwriting newer changes.
- A filtered unique index prevents more than one pending organizer application per Student.
- Paid events cannot use the free-registration endpoint. Registration is created only after a signed Paystack webhook is independently verified for reference, amount, and currency.
- Signed QR tickets expire after the event and can be checked in only once by the owning Organizer or an Admin.
- Free votes are protected by a database unique constraint; paid vote quantities are recorded only after an idempotent verified webhook.
- Certificates require confirmed attendance and a past event date, are stored once in a private Supabase bucket, and are returned through short-lived signed URLs.

All API failures use:

```json
{ "error": "Human-readable explanation." }
```

## Configuration

Production secrets are not committed. Supply configuration through environment variables or your deployment secret store:

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=event_management;Username=postgres;Password=your-password'
export Jwt__SigningKey='replace-with-a-new-random-signing-key'
export Jwt__Issuer='EventManagement.Api'
export Jwt__Audience='EventManagement.Frontend'
export BootstrapAdmin__Email='admin@cevents.com'
export BootstrapAdmin__Password='a-strong-initial-password'
export BootstrapAdmin__Name='System Administrator'
export PAYSTACK_SECRET_KEY='set-in-the-deployment-secret-store'
export QR_SIGNING_KEY='replace-with-a-stable-random-key-of-at-least-32-characters'
export EMAIL_PROVIDER='Gmail'
export GMAIL_SMTP_HOST='smtp.gmail.com'
export GMAIL_SMTP_PORT='587'
export GMAIL_SMTP_USERNAME='account@gmail.com'
export GMAIL_APP_PASSWORD='google-app-password-not-account-password'
export GMAIL_SENDER_EMAIL='account@gmail.com'
export GMAIL_DAILY_WARNING_THRESHOLD='400'
export SUPABASE_URL='https://your-project.supabase.co'
export SUPABASE_SERVICE_ROLE_KEY='set-in-the-deployment-secret-store'
export CERTIFICATES_BUCKET='certificates'
export CERTIFICATE_SIGNED_URL_MINUTES='60'
export CERTIFICATE_TEMPLATE_VERSION='1'
```

Gmail SMTP requires 2-Step Verification and a Google App Password; never use the
account password. Standard consumer Gmail accounts are commonly constrained to
about 500 outgoing messages per day. Each accepted Gmail send emits a structured
`Gmail daily send count` log and warns at `GMAIL_DAILY_WARNING_THRESHOLD` (400 by
default), then every 25 messages. This lightweight counter is per process and resets
on restart, so replace it with a shared metric before running multiple replicas.
If sustained volume approaches the account limit, move to Google Workspace with
its higher applicable limits or a transactional email provider with delivery and
bounce monitoring.

For local development, keep the signing key out of tracked settings and store it
with .NET User Secrets:

```bash
cd backend/EventManagement.Api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -hex 64)" >/dev/null
```

See `../SECURITY.md` for rotation, deployment, and git-history cleanup guidance.

The bootstrap Admin is created only when both bootstrap email and password are configured and no account already uses that email. Remove the bootstrap password from the environment after the first successful startup.

For local development, copy `appsettings.Development.example.json` if local
non-secret overrides are needed. The real development signing key belongs in
.NET User Secrets and the local `appsettings.Development.json` is ignored by git.

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
| GET | `/api/organizer-applications?status=Pending&search=&page=1&pageSize=20` | Admin | Paginated, searchable application queue |
| PUT | `/api/organizer-applications/{id}/approve` | Admin | Approve and promote the applicant |
| PUT | `/api/organizer-applications/{id}/reject` | Admin | Reject with an optional reason |
| GET | `/api/users?search=&role=&isActive=&page=1&pageSize=20` | Admin | Paginated user management list |
| PUT | `/api/users/{id}/role` | Admin | Promote to Organizer or demote to Student |
| PUT | `/api/users/{id}/deactivate` | Admin | Soft-disable an account |
| GET | `/api/events?search=&category=&from=&to=&page=1&pageSize=20` | Public | Paginated filtered event list |
| GET | `/api/events/{id}` | Public | Event detail |
| GET | `/api/events/mine?page=1&pageSize=20` | Organizer, Admin | Events created by the current user |
| GET | `/api/events/all?search=&category=&page=1&pageSize=20` | Admin | Paginated event-management list including drafts |
| POST | `/api/events` | Organizer, Admin | Create an event |
| PUT | `/api/events/{id}` | Owner Organizer, Admin | Update an event |
| DELETE | `/api/events/{id}` | Owner Organizer, Admin | Delete an event and its registrations |
| POST | `/api/events/{id}/register` | Student | Register with duplicate/capacity enforcement |
| POST | `/api/payments/events/{id}/initialize` | Student | Initialize Paystack checkout for a paid event |
| GET | `/api/payments/{reference}` | Same Student only | Read server-verified payment status |
| POST | `/api/payments/webhooks/paystack` | Paystack webhook | Verify booking or voting payment and apply it idempotently |
| GET | `/api/tickets/{registrationId}` | Same Student only | Create a signed QR ticket token |
| POST | `/api/events/{id}/check-in` | Owner Organizer, Admin | Validate a signed ticket and record one-time attendance |
| POST | `/api/certificates/registrations/{registrationId}` | Same Student only | Generate once and return a short-lived certificate URL |
| GET | `/api/events/{id}/voting` | Public; manager sees live totals | View an event voting campaign |
| PUT | `/api/events/{id}/voting` | Owner Organizer, Admin | Configure voting dates, categories, nominees, and prices |
| POST | `/api/voting/categories/{id}/votes` | Student | Cast one database-enforced free vote |
| POST | `/api/voting/categories/{id}/payments/initialize` | Student | Initialize a 1–100 quantity paid-vote checkout |
| GET | `/api/events/{id}/registration-status` | Student | Check registration without scanning the Student's history |
| GET | `/api/events/{id}/registrants?search=&attended=&page=1&pageSize=20` | Owner Organizer, Admin | View paginated, filtered registrants |
| PUT | `/api/events/{id}/attendance` | Owner Organizer, Admin | Bulk-update attendance |
| GET | `/api/students/{id}/registrations?page=1&pageSize=20` | Same Student only | View paginated own registrations |
| GET | `/api/booking-requests?status=&page=1&pageSize=20` | Admin | Paginated booking-request queue |
| GET | `/api/booking-requests/assigned?status=&page=1&pageSize=20` | Organizer | Paginated assigned booking requests |
| GET | `/api/reports/summary` | Admin | Totals and overall attendance rate |
| GET | `/api/reports/events?page=1&pageSize=20` | Admin | Paginated per-event registration and attendance aggregates |
| GET | `/api/reports/events/{id}` | Admin | One event's registration and attendance aggregate |
| GET | `/api/reports/organizers?page=1&pageSize=20` | Admin | Paginated activity grouped by Organizer |

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

The JWT contains `sub` and `userId` claims for the user ID, a `role` claim used by API authorization, and a `sessionVersion` claim checked against the database on every authenticated request.

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
  "category": "Technology",
  "version": 3
}
```

Omit `version` when creating an event. For updates, send the value from the
latest event response; every successful update increments it.

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

## Deployment notes

Gmail SMTP requires 2FA and a Google App Password; a standard Gmail account is a scaling risk at roughly 500 messages per day. Keep Mailtrap selected for local sandbox delivery when needed. The private `certificates` bucket must exist before certificate generation is enabled.

Production migrations are intentionally separate from source implementation and must be reviewed before `Database__ApplyMigrations=true` applies them. Native mobile apps and refresh-token issuance remain out of scope.
