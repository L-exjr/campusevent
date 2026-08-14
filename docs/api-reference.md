# API reference

Auth labels: **Public**, **User** (authenticated ordinary account), **Owner/Admin** (service-enforced event ownership), and **Admin**.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET/POST | `/api/auth/csrf`, `/register`, `/login`, `/forgot-password`, `/reset-password`, `/google`, `/session`, `/logout` | Public/session | Authentication and session lifecycle |
| GET | `/api/events` / `/api/events/{id}` | Public | Browse published events |
| GET | `/api/events/mine` | User | Owned events |
| GET | `/api/events/all` | Admin | All events |
| POST | `/api/events` | User | Create owned event and tiers |
| GET/PUT/DELETE | `/api/events/{id}/management`, `/api/events/{id}` | Owner/Admin | Manage event |
| PUT | `/api/events/{id}/organizer` | Admin | Transfer ownership |
| POST/GET | `/api/events/{id}/register`, `/registration-status` | User | Free registration/status |
| GET | `/api/events/{id}/registrants` | Owner/Admin | Paginated attendees |
| GET | `/api/events/{id}/registrants/export` | Owner/Admin | Attendee CSV |
| PUT | `/api/events/{id}/attendance` | Owner/Admin | Bulk attendance |
| GET | `/api/events/analytics/mine` | User | Organizer aggregates |
| POST | `/api/payments/events/{id}/initialize` | User | Initialize tier/coupon checkout |
| GET | `/api/payments/{reference}` | User/owner of order | Payment status |
| POST | `/api/payments/webhooks/paystack` | Signed provider | Paystack notification |
| POST | `/api/payments/webhooks/flutterwave` | Signed provider | Flutterwave notification |
| GET/POST/PUT | `/api/coupons`, `/api/coupons/{id}` | User/owner | Manage coupons |
| GET | `/api/tickets/{registrationId}` | Attendee | Signed QR token and short code |
| POST | `/api/events/{id}/check-in` / `/manual` | Owner/Admin | QR or code check-in |
| GET/PUT | `/api/events/{id}/voting` | Public / Owner/Admin | View/manage voting campaign |
| POST | `/api/voting/categories/{id}/votes` | User | Cast free vote |
| POST/GET | `/api/voting/categories/{id}/payments/initialize`, `/api/voting/payments/{reference}` | User | Paid voting checkout/status |
| POST | `/api/certificates/registrations/{id}` | Attendee | Generate/retrieve certificate |
| POST | `/api/booking-requests` | Public | Submit organizer request |
| GET/PUT | `/api/booking-requests…` | Assigned user/Admin | Queue, assignment, response, status |
| GET/PUT | `/api/organizers…` | Public/self | Directory and self settings |
| POST | `/api/uploads/profile-image`, `/organizer-banner` | User | Self-owned staged image |
| POST | `/api/uploads/event-image?eventId={id}` | Owner/Admin | Event cover upload |
| GET/PUT | `/api/users…` | Admin except self profile | User administration/profile |
| GET | `/api/students/{id}/registrations` | Self | Registration history |
| GET | `/api/reports…` | Admin | Summary/event/organizer reports |
| GET/PUT | `/api/email-outbox…`, `/api/image-cleanup…` | Admin | Failure recovery |
| GET | `/api/admin-audit-logs`, `/export` | Admin | Audit log/query export |

Organizer-application endpoints remain present for legacy compatibility; see [Authorization model](auth-model.md).
