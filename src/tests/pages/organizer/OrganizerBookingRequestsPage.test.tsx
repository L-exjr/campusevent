import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { server } from '../../mocks/server'
import { paginated, users } from '../../mocks/fixtures'
import { renderWithAuth } from '../../testUtils'

const apiUrl = 'http://localhost:5080/api'

function assignedRequest(status = 'Quoted') {
  return {
    id: 'booking-1',
    organizationName: 'Engineering Society',
    contactName: 'Casey Contact',
    email: 'casey@example.test',
    phone: '+233 20 000 0000',
    eventType: 'Leadership workshop',
    requiresTicketing: true,
    requiresVoting: false,
    requiresRegistration: true,
    proposedDate: '2030-08-20T14:00:00Z',
    alternativeDates: null,
    flexibilityNote: null,
    estimatedAttendance: 80,
    preferredOrganizer: null,
    description: 'A detailed booking request assigned to this organizer.',
    status,
    assignedOrganizerId: users.organizer.id,
    assignedOrganizerName: users.organizer.name,
    organizerResponseNote: null,
    draftEventId: null,
    submittedAt: '2026-08-01T12:00:00Z',
    updatedAt: '2026-08-01T12:00:00Z',
    quote: { id: 'quote-1', proposedFeeMinor: 250000, currency: 'GHS', proposedTimeline: 'Four weeks', message: 'Includes planning and delivery.', submittedAt: '2026-08-01T12:00:00Z' },
    statusHistory: [{ id: 'history-1', status: 'Quoted', note: 'Organizer submitted a quote.', createdAt: '2026-08-01T12:00:00Z' }],
  }
}

describe('OrganizerBookingRequestsPage', () => {
  it('accepts an assigned request and shows the draft-event outcome', async () => {
    let responseBody: unknown
    server.use(
      http.get(`${apiUrl}/booking-requests/assigned`, () =>
        HttpResponse.json(paginated([assignedRequest()]))),
      http.put(`${apiUrl}/booking-requests/:id/respond`, async ({ request }) => {
        responseBody = await request.json()
        return HttpResponse.json({
          ...assignedRequest('Accepted'),
          organizerResponseNote: 'Happy to help.',
          draftEventId: 'draft-event-1',
        })
      }),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, {
      user: users.organizer,
      initialEntries: ['/organizer/booking-requests'],
    })

    expect(await screen.findByText('Engineering Society')).toBeVisible()
    await user.type(screen.getByLabelText('Response note (optional)'), 'Happy to help.')
    await user.click(screen.getByRole('button', { name: 'Accept request' }))

    await waitFor(() => expect(responseBody).toEqual({ accept: true, note: 'Happy to help.' }))
    expect(screen.getByText(/unpublished event draft is ready/i)).toBeVisible()
    expect(screen.queryByRole('button', { name: 'Accept request' })).not.toBeInTheDocument()
  })

  it('declines an assigned request', async () => {
    server.use(
      http.get(`${apiUrl}/booking-requests/assigned`, () =>
        HttpResponse.json(paginated([assignedRequest()]))),
      http.put(`${apiUrl}/booking-requests/:id/respond`, () =>
        HttpResponse.json({
          ...assignedRequest('Declined'),
          organizerResponseNote: 'Schedule conflict.',
        })),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, {
      user: users.organizer,
      initialEntries: ['/organizer/booking-requests'],
    })

    await screen.findByText('Engineering Society')
    await user.click(screen.getByRole('button', { name: 'Decline request' }))

    expect(await screen.findByText('Request declined.')).toBeVisible()
    expect(screen.getByText('Schedule conflict.')).toBeVisible()
  })

  it('keeps the request actionable and exposes API response errors', async () => {
    server.use(
      http.get(`${apiUrl}/booking-requests/assigned`, () =>
        HttpResponse.json(paginated([assignedRequest()]))),
      http.put(`${apiUrl}/booking-requests/:id/respond`, () =>
        HttpResponse.json({ error: 'This request is not awaiting an Organizer response.' }, { status: 409 })),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, {
      user: users.organizer,
      initialEntries: ['/organizer/booking-requests'],
    })

    await screen.findByText('Engineering Society')
    await user.click(screen.getByRole('button', { name: 'Accept request' }))

    expect(await screen.findByText('This request is not awaiting an Organizer response.')).toBeVisible()
    expect(screen.getByRole('button', { name: 'Accept request' })).toBeEnabled()
  })
})
