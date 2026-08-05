import { mockApi } from '../../api/mockApi'

describe('mockApi operational workflows', () => {
  it('supports assignment and organizer acceptance with a generated draft', async () => {
    await mockApi.login('admin@cevents.com', 'demo123')
    const queue = await mockApi.getBookingRequests()
    const submitted = queue.items.find((request) => request.status === 'submitted')
    expect(submitted).toBeDefined()
    const assigned = await mockApi.assignBookingRequest(
      submitted!.id,
      'user-organizer-1',
    )
    expect(assigned.status).toBe('sentToOrganizer')

    await mockApi.logout()
    await mockApi.login('organizer@cevents.com', 'demo123')
    const organizerQueue = await mockApi.getAssignedBookingRequests()
    expect(organizerQueue.items.some((request) => request.id === submitted!.id)).toBe(true)
    const accepted = await mockApi.respondToBookingRequest(
      submitted!.id,
      true,
      'Happy to organize this event.',
    )

    expect(accepted.status).toBe('accepted')
    expect(accepted.draftEventId).not.toBeNull()
    expect((await mockApi.getManagementEvent(accepted.draftEventId!)).isPublished).toBe(false)
  }, 10_000)

  it('retries seeded dead letters and records the admin audit entry', async () => {
    await mockApi.login('admin@cevents.com', 'demo123')
    const failed = await mockApi.getFailedEmails()
    expect(failed.items).toHaveLength(1)

    await mockApi.retryFailedEmail(failed.items[0].id)

    expect((await mockApi.getFailedEmails()).items).toHaveLength(0)
    const audit = await mockApi.getAdminAuditLogs('EmailDeadLetterRetried')
    expect(audit.items).toHaveLength(1)
    expect(audit.items[0].targetId).toBe(failed.items[0].id)
  }, 10_000)
})
