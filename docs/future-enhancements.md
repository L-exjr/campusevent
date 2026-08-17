# Future enhancement evaluation

## Institutional single sign-on

**Recommendation:** use OpenID Connect with the institution as an external identity provider while retaining the API-issued application session. Map stable subject identifiers and verified institutional email claims, but keep application roles in Campus Events. Pilot account linking, leavers, recovery and provider outages.

## Calendar export

**Recommendation:** start with downloadable RFC 5545 `.ics` files for registrations and organizer events. Later opt-in Google/Microsoft synchronization would require OAuth consent, encrypted tokens, revocation and duplicate-update rules.

## Notification preferences

**Recommendation:** add per-user, per-channel choices for confirmations, reminders, schedule changes and organizer alerts. Security messages remain mandatory. Record the applicable preference with queued messages.

## Offline-assisted check-in

**Recommendation:** prototype a progressive web application with short-lived, event-scoped attendee manifests and queued scans. The server remains authoritative during reconciliation. Encrypt cached data, minimize attendee fields, expire manifests and support device revocation.

## Suggested sequence

1. Calendar export (`.ics`) — high value, low integration risk.
2. Notification preferences — moderate scope and immediate usability benefit.
3. Institutional SSO pilot — high value but dependent on identity governance.
4. Offline-assisted check-in pilot — useful but has the greatest privacy and reconciliation complexity.
