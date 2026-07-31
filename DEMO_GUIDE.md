# Campus Events demonstration guide

## Demo dataset

The optional KNUST dataset is deliberately opt-in. It creates three Organizers,
six Students, six published events, and registrations/attendance for the two
past events. It is idempotent: re-running it does not duplicate those records.

Set these Railway variables for one deploy:

| Variable | Purpose |
| --- | --- |
| `DemoData__Enabled` | Set to `true` to enable the one-time demo seed. |
| `DemoData__Password` | A 12+ character demo-only password shared by the nine demo accounts. |

After Railway confirms the seed in its logs, set `DemoData__Enabled` to `false`.
The records remain, but future deploys no longer run the seed path. Never reuse
a personal or production password for `DemoData__Password`.

## Suggested walkthrough

1. Start on the landing page and browse the KNUST events as a visitor.
2. Sign in as Admin and show the focused operations navigation: reports, users,
   organizer applications, all events, and booking requests.
3. Open **Users** to show the seeded role distribution and the ability to
   promote a Student or deactivate a non-Admin account.
4. Open **All events** to show past, current, and upcoming KNUST activities.
5. Open **Reports** to connect the event data, organizer activity, and
   registration figures.
6. Sign in with a demo Student account to show registration history, or with a
   demo Organizer account to show event management and attendance.

## Likely questions and concise answers

### What problem does the application solve?

It gives a campus one place to publish events, collect registrations, manage
attendance, and control access according to each person's role.

### Which user roles exist?

Visitors browse public events. Students register and can apply to become
Organizers. Organizers manage their own events and attendance. Admins manage
users, applications, all events, booking requests, and reports.

### How is access protected?

The API issues short-lived JWT bearer tokens after login. Every protected API
endpoint checks the token and role; the frontend route guards are for user
experience, while the backend is the security boundary.

### How are passwords handled?

Passwords are never stored in plain text. The ASP.NET Core API hashes them with
PBKDF2 before saving them to PostgreSQL.

### Can someone use Google to sign in?

Yes. Google Identity Services returns an ID token, the backend verifies its
signature, issuer, expiry, verified email, and expected audience, then issues
the same application JWT used by local login.

### Why PostgreSQL and EF Core?

PostgreSQL gives the app reliable relational storage and constraints. EF Core
keeps the model and schema aligned through versioned migrations, which are
applied before the production API becomes healthy.

### How do you prevent duplicate registrations or overselling an event?

The database has a unique event/student registration index. The API locks the
event while it checks capacity and creates a registration, so concurrent
requests cannot overfill it.

### How does the system handle email failures?

Core actions, such as a completed registration, remain successful even if a
notification cannot be sent. The failure is logged for administrators to fix;
password-reset responses stay generic so an attacker cannot discover accounts.

### How does the frontend securely call the backend?

The Vite frontend reads its API base URL from environment configuration, sends
the JWT as a bearer token, and Railway permits only the explicit Vercel origin
through CORS rather than allowing every website.

### How are event and profile images stored?

The frontend uploads them to Supabase Storage using the public project URL and
anon key, then the resulting image URL is saved with the event or profile.
Storage policies remain the enforcement point for what uploads are allowed.

### What happens when an Organizer application is approved?

An Admin reviews it, records the decision, promotes the Student to Organizer
when approved, and the user gains access to the Organizer workspace on their
next authenticated session.

### How is production deployment handled?

The API and PostgreSQL run on Railway; the Vite frontend runs on Vercel. GitHub
Actions requires the backend and frontend checks to pass before the deployment
jobs run on `main`.

### What would you add next?

Rotating refresh tokens in secure cookies, database-backed job scheduling for
reliable reminders, richer analytics, QR-assisted attendance, and a dedicated
staging environment for preview deployments.
