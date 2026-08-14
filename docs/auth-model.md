# Authorization model

The durable capability model is resource ownership, not the historical Student/Organizer distinction.

- An active non-Admin account may create an event and becomes its owner.
- Event update/delete, registrants, CSV export, attendance, check-in, cover upload, voting management, and event-scoped coupon management require `EventEntity.OrganizerId == authenticated user id`.
- Admin is distinct and may manage any event where the endpoint explicitly permits Admin.
- Attendee resources—registrations, payment status, tickets, certificates, votes—are scoped to the authenticated account’s database record.
- Assigned booking requests are scoped by `AssignedOrganizerId`.
- Profile, organizer-directory settings, and staged images are scoped to the authenticated user ID.

Controller role attributes are coarse authentication gates. Services enforce the resource boundary and must remain the source of truth.

## Legacy organizer applications

The Admin applications screen and API still exist, but the student submission page is not routed in the current SPA. Approval still changes the legacy role field even though event capability now comes from ownership. Removal or repurposing as a verification/trust workflow requires a product decision; it should not be treated as the capability grant.
