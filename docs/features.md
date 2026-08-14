# Features

## Events and organizer tools

Any active ordinary account can create an event. The creator becomes `OrganizerId`; subsequent management is owner-or-Admin. Events support physical, virtual, and hybrid details, publication state, registration/ticketing switches, sales windows, attendee CSV export, attendance management, and aggregate organizer analytics.

## Ticket tiers, registration, and coupons

Each event has one or more named ticket tiers with independent GHS price and capacity. Existing events are migrated to a single `General` tier preserving their previous price and capacity. Free registrations are created directly. Paid registration creates a pending provider-specific payment order that reserves tier capacity. A verified webhook creates the registration idempotently.

Organizers can create globally applicable or event-scoped percentage coupons with an optional usage limit and expiry. The API normalizes and validates the code, locks the coupon while reserving usage, and calculates `OriginalAmountMinor`, `DiscountAmountMinor`, and final `AmountMinor`. The client never supplies a discount amount.

## Tickets and check-in

A registration has a signed QR token and a short `EMS-…` ticket code. Event owners and Admins may scan the token or type the code. Both paths lock the registration, enforce event ownership, and prevent double check-in. Attendees can retrieve only their own ticket.

## Voting

Event owners configure campaign dates, publication, categories, nominees, and free or paid voting. They may expose live totals publicly; otherwise totals remain private until close. Paid orders created before close remain valid after close until their own expiry. An expired order is rejected even if the provider later reports success.

## Organizer analytics

The organizer dashboard aggregates existing registrations and verified payment orders. It reports daily registration counts, verified ticket revenue in GHS, attendance totals, and attendance rate. No behavioral tracking is introduced.

## Organizer directory and booking requests

Users who own events may opt into the public organizer directory and maintain bio, banner, links, and specialties. Public booking requests may select a visible organizer. Admins assign requests; only the assigned user may respond. Acceptance creates a private event draft.

## Certificates, email, and administration

Attended registrations can generate private signed certificate downloads after the event. Transactional email is queued in a database outbox. Admin tools cover users, event ownership transfer, reports, failed email/image retries, and immutable audit logs.
