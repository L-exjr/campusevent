# Authorization model

Authorization is layered and evaluated by the API in this order: platform Admin, event owner, event-team capability, then denial. The historical Student/Organizer distinction remains only a coarse compatibility label and is not the durable event-management boundary.

- An active non-Admin account may create an event and becomes its owner.
- A platform Admin has full event capability wherever the endpoint admits Admin.
- The event owner (`EventEntity.OrganizerId`) has full access and cannot be removed from the event or assigned a team role.
- A `Team Admin` has all event capabilities. A `Member` may view attendees, check in, edit, and manage operations. `Check-in Staff` may only view attendees and check in. Neither `Member` nor `Check-in Staff` may view revenue, manage the team, or delete the event.
- Attendee resources—registrations, payment status, tickets, certificates, votes—are scoped to the authenticated account’s database record.
- Assigned booking requests are scoped by `AssignedOrganizerId`.
- Profile, organizer-directory settings, and staged images are scoped to the authenticated user ID.

Controller role attributes are coarse authentication gates. Services enforce the resource boundary and must remain the source of truth. Team membership never changes `User.Role`; capabilities are scoped to one event and are returned by `GET /api/events/{eventId}/access`.

## Organizer verification

Organizer verification is a trust signal, not a capability grant. Active non-Admin users may request verification from their profile. The status is `Unverified`, `Pending`, or `Verified`; only Admins may approve or reject requests. Approval displays a badge on the organizer's public directory card and profile, while rejection returns the account to `Unverified`.

Verification never changes `User.Role`, never changes an existing session's permissions, and is not consulted by event creation or event-management authorization. It is an independent trust signal layered outside the Admin → owner → event-team authorization chain.
