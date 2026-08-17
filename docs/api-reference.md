# API reference

## Operational monitoring

`GET /api/operational-metrics` requires an Admin session and returns
process-lifetime counters for payment callbacks, email delivery, image cleanup
and provider quota warnings, plus the process start time.

Auth labels: **Public**, **User** (authenticated ordinary account), **Event capability** (service-enforced owner/Admin/team access), **Owner/Admin**, and **Admin**.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET/POST | `/api/auth/csrf`, `/register`, `/login`, `/forgot-password`, `/reset-password`, `/google`, `/session`, `/logout` | Public/session | Authentication and session lifecycle |
| GET | `/api/events` / `/api/events/{id}` | Public | Browse published events |
| GET | `/api/events/mine` | User | Owned events |
| GET | `/api/events/all` | Admin | All events |
| POST | `/api/events` | User | Create owned event and tiers |
| GET/PUT/DELETE | `/api/events/{id}/management`, `/api/events/{id}` | Owner/Admin | Manage event |
| PUT | `/api/events/{id}/organizer` | Admin | Transfer ownership |
| GET | `/api/events/{id}/access` | User / event participant | Return the server-calculated event capability flags |
| GET | `/api/events/{id}/team` | `ManageTeam` | List the owner and invited team members |
| POST | `/api/events/{id}/team` | `ManageTeam` | Add an existing active ordinary account by email with `Admin`, `Member`, or `CheckInStaff` role |
| PUT | `/api/events/{id}/team/{userId}` | `ManageTeam` | Change an invited member's team role |
| DELETE | `/api/events/{id}/team/{userId}` | `ManageTeam` | Remove an invited member; the owner cannot be removed |
| GET | `/api/events/{id}/revenue` | `ViewRevenue` | Return verified ticket revenue in the event currency |
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
| POST | `/api/booking-requests` | Public | Submit a structured commissioning brief; returns its ID and one-time tracking token |
| GET | `/api/booking-requests/{id}/track?token=…` | Public with token | Read the privacy-reduced status, quote, history, and draft ID; the server matches the token's SHA-256 hash |
| GET | `/api/booking-requests` | Admin | Paginated commissioning queue, optionally filtered by lifecycle status |
| GET | `/api/booking-requests/assigned` | User | Paginated requests assigned to the authenticated organizer |
| PUT | `/api/booking-requests/{id}/assign` | Admin | Assign or reassign the single organizer |
| POST | `/api/booking-requests/{id}/quote` | Assigned user | Submit the request's single GHS quote and move it to `Quoted` |
| PUT | `/api/booking-requests/{id}/respond` | Assigned user | Accept a quoted request and create its private event draft, or decline it |
| PUT | `/api/booking-requests/{id}/status` | Admin | Apply an allowed administrative lifecycle transition |
| GET/PUT | `/api/organizers…` | Public/self | Directory and self settings |
| POST | `/api/uploads/profile-image`, `/organizer-banner` | User | Self-owned staged image |
| POST | `/api/uploads/event-image?eventId={id}` | Owner/Admin | Event cover upload |
| GET/PUT | `/api/users…` | Admin except self profile | User administration/profile |
| GET | `/api/students/{id}/registrations` | Self | Registration history |
| GET | `/api/reports…` | Admin | Summary/event/organizer reports |
| GET/PUT | `/api/email-outbox…`, `/api/image-cleanup…` | Admin | Failure recovery |
| GET | `/api/admin-audit-logs`, `/export` | Admin | Audit log/query export |

Organizer-application endpoints remain present for legacy compatibility; see [Authorization model](auth-model.md).

`BookingRequest` briefs include event category, budget range in minor units, proposed and expected-end dates, scheduling flexibility, estimated attendance, required ticketing/voting/registration tools, reference links, and descriptive context. `BookingRequestQuote` is one-to-one with the request; competing multi-organizer bids are outside the implemented scope. Every transition appends a `BookingRequestStatusHistory` record. The statuses are `Submitted`, `UnderReview`, `SentToOrganizer`, `Quoted`, `Accepted`, `Declined`, `Converted`, and `Closed`.
