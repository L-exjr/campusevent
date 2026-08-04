import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { paginated, users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

const apiUrl = 'http://localhost:5080/api'

function request(overrides: Record<string, unknown> = {}) {
  return {
    id: 'booking-admin-1',
    organizationName: 'Campus Society',
    contactName: 'Casey Contact',
    email: 'casey@example.test',
    phone: '+233 20 000 0000',
    eventType: 'Workshop',
    proposedDate: '2030-08-20T14:00:00Z',
    alternativeDates: null,
    flexibilityNote: null,
    estimatedAttendance: 50,
    preferredOrganizer: null,
    description: 'A sufficiently detailed booking request for the admin queue.',
    status: 'SentToOrganizer',
    assignedOrganizerId: 'old-organizer',
    assignedOrganizerName: 'Old Organizer',
    organizerResponseNote: null,
    draftEventId: null,
    submittedAt: '2026-08-01T12:00:00Z',
    updatedAt: '2026-08-01T12:00:00Z',
    ...overrides,
  }
}

function apiUser() {
  return {
    id: users.organizer.id,
    name: users.organizer.name,
    email: users.organizer.email,
    role: 'Organizer',
    isActive: true,
    createdAt: users.organizer.joinedAt,
    imageUrl: null,
  }
}

describe('AdminBookingQueue', () => {
  it('reassigns an already assigned request', async () => {
    let assignmentBody: unknown
    server.use(
      http.get(`${apiUrl}/booking-requests`, () => HttpResponse.json(paginated([request()]))),
      http.get(`${apiUrl}/users`, () => HttpResponse.json(paginated([apiUser()]))),
      http.put(`${apiUrl}/booking-requests/:id/assign`, async ({ request: httpRequest }) => {
        assignmentBody = await httpRequest.json()
        return HttpResponse.json(request({
          assignedOrganizerId: users.organizer.id,
          assignedOrganizerName: users.organizer.name,
        }))
      }),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, { user: users.admin, initialEntries: ['/admin/booking-requests'] })

    await user.selectOptions(
      await screen.findByLabelText('Organizer for Campus Society'),
      users.organizer.id,
    )
    await user.click(screen.getByRole('button', { name: 'Reassign' }))

    await waitFor(() => expect(assignmentBody).toEqual({ organizerId: users.organizer.id }))
    expect(screen.getByText('Request reassigned to the selected Organizer.')).toBeVisible()
  })

  it('closes a recoverable request', async () => {
    server.use(
      http.get(`${apiUrl}/booking-requests`, () => HttpResponse.json(paginated([request()]))),
      http.get(`${apiUrl}/users`, () => HttpResponse.json(paginated([apiUser()]))),
      http.put(`${apiUrl}/booking-requests/:id/status`, () =>
        HttpResponse.json(request({ status: 'Closed' }))),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, { user: users.admin, initialEntries: ['/admin/booking-requests'] })

    await user.click(await screen.findByRole('button', { name: 'Close request' }))

    expect(await screen.findByText('Request closed.')).toBeVisible()
    expect(screen.queryByRole('button', { name: 'Close request' })).not.toBeInTheDocument()
  })

  it('keeps recovery actions available after an API error', async () => {
    server.use(
      http.get(`${apiUrl}/booking-requests`, () => HttpResponse.json(paginated([request()]))),
      http.get(`${apiUrl}/users`, () => HttpResponse.json(paginated([apiUser()]))),
      http.put(`${apiUrl}/booking-requests/:id/status`, () =>
        HttpResponse.json({ error: 'The request changed. Reload and try again.' }, { status: 409 })),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, { user: users.admin, initialEntries: ['/admin/booking-requests'] })

    await user.click(await screen.findByRole('button', { name: 'Close request' }))

    expect(await screen.findByText('The request changed. Reload and try again.')).toBeVisible()
    expect(screen.getByRole('button', { name: 'Close request' })).toBeEnabled()
  })
})
