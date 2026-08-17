import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { apiEvent, paginated, users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

const apiUrl = 'http://localhost:5080/api'

describe('AdminEventsPage', () => {
  it('transfers ownership to a searched active organizer with the current version', async () => {
    const newOrganizerId = 'organizer-new'
    let transferBody: unknown
    server.use(
      http.get(`${apiUrl}/events/all`, () => HttpResponse.json(paginated([apiEvent()]))),
      http.get(`${apiUrl}/users`, () => HttpResponse.json(paginated([{
        id: newOrganizerId,
        name: 'Nora New Owner',
        email: 'nora@example.test',
        role: 'Organizer',
        isActive: true,
        createdAt: '2026-01-01T12:00:00Z',
        imageUrl: null,
      }]))),
      http.put(`${apiUrl}/events/:id/organizer`, async ({ request }) => {
        transferBody = await request.json()
        return HttpResponse.json(apiEvent({
          organizerId: newOrganizerId,
          organizerName: 'Nora New Owner',
          version: 2,
        }))
      }),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, { user: users.admin, initialEntries: ['/admin/events'] })

    await user.click(await screen.findByRole('button', { name: 'Transfer' }))
    await user.selectOptions(await screen.findByLabelText('New owner'), newOrganizerId)
    await user.click(screen.getByRole('button', { name: 'Transfer ownership' }))

    await waitFor(() => expect(transferBody).toEqual({
      organizerId: newOrganizerId,
      version: 1,
    }))
    expect(await screen.findByText('Event ownership transferred successfully.')).toBeVisible()
  }, 10_000)
})
