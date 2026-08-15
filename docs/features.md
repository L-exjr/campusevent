# Features

## Events and organizer tools

Any active ordinary account can create an event. The creator becomes `OrganizerId`; subsequent management is capability-based: platform Admins and the event owner have full access, while invited event-team members receive only the capabilities attached to their team role. Events support physical, virtual, and hybrid details, publication state, registration/ticketing switches, sales windows, attendee CSV export, attendance management, and aggregate organizer analytics.

### Event teams

An owner or platform Admin can add an existing active ordinary account to an event team, change its role, or remove it. `Team Admin` has full event access; `Member` can view attendees, check in guests, edit the event, and manage operations but cannot see revenue, manage the team, or delete the event; `Check-in Staff` can only view attendees and check them in. The management UI obtains the server-calculated capability set from `GET /api/events/{eventId}/access` rather than inferring permissions from a role label.

## Ticket tiers, registration, and coupons

Each event has one or more named ticket tiers with independent GHS price and capacity. Existing events are migrated to a single `General` tier preserving their previous price and capacity. Free registrations are created directly. Paid registration creates a pending provider-specific payment order that reserves tier capacity. A verified webhook creates the registration idempotently.

Organizers can create globally applicable or event-scoped percentage coupons with an optional usage limit and expiry. The API normalizes and validates the code, locks the coupon while reserving usage, and calculates `OriginalAmountMinor`, `DiscountAmountMinor`, and final `AmountMinor`. The client never supplies a discount amount.

## Tickets and check-in

A registration has a signed QR token and a short `EMS-…` ticket code. Event owners and Admins may scan the token or type the code. Both paths lock the registration, enforce event ownership, and prevent double check-in. Attendees can retrieve only their own ticket.

## Voting

Event owners configure campaign dates, publication, categories, nominees, and free or paid voting. They may expose live totals publicly; otherwise totals remain private until close. Paid orders created before close remain valid after close until their own expiry. An expired order is rejected even if the provider later reports success.

## Organizer analytics

The organizer dashboard aggregates existing registrations and verified payment orders. It reports daily registration counts, verified ticket revenue in GHS, attendance totals, and attendance rate. No behavioral tracking is introduced.

## Organizer directory and commissioning

Users who own events may opt into the public organizer directory and maintain bio, banner, links, and specialties. Public commissioning requests may select a visible organizer or enter Admin triage. Their structured briefs capture category, budget range, dates and flexibility, attendance, requested platform tools, references, and the event description.

Commissioning remains single-assignment: an Admin assigns one organizer, and only that organizer may submit the request's single GHS quote with a proposed fee, timeline, and message. The lifecycle is recorded as status-history entries across `Submitted`, `UnderReview`, `SentToOrganizer`, `Quoted`, `Accepted`, `Declined`, `Converted`, and `Closed`. Anonymous requesters receive a one-time cryptographically random tracking token; the API stores only its SHA-256 hash and requires the raw token to retrieve the privacy-reduced tracking view. Accepting the quote preserves the existing conversion flow by creating an unpublished event draft for the assigned organizer; publishing that draft advances the request to `Converted`.

## Demonstration catalog

When `DemoData__Enabled=true`, the idempotent initializer creates 38 draft catalog records spanning all 19 event categories (two per category). The records are distributed across six explicitly fictional demo organizers: two `Verified` and directory-visible, two `Pending` (one visible and one hidden), and two `Unverified` and hidden. Verified and Pending profiles include demo biographies, placeholder banners, social links, and specialties derived from their assigned catalog categories. These states exist solely to demonstrate directory filters and badges; they are not endorsements or claims of affiliation.

## Certificates, email, and administration

Attended registrations can generate private signed certificate downloads after the event. Transactional email is queued in a database outbox. Admin tools cover users, event ownership transfer, reports, failed email/image retries, and immutable audit logs.
