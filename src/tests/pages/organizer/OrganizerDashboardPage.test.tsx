import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { apiEvent, paginated, users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

describe('OrganizerDashboardPage upcoming schedule', () => {
  it('requests upcoming events and uses the earliest returned event as next', async () => {
    let upcomingQuery: string | null = null
    server.use(
      http.get('http://localhost:5080/api/events/mine', ({ request }) => {
        upcomingQuery = new URL(request.url).searchParams.get('upcoming')
        return HttpResponse.json(paginated([
          apiEvent({ id: 'next-event', title: 'Next campus event', date: '2030-08-10T10:00:00Z' }),
          apiEvent({ id: 'later-event', title: 'Later campus event', date: '2030-09-10T10:00:00Z' }),
        ]))
      }),
    )

    renderWithAuth(<App />, { user: users.organizer, initialEntries: ['/organizer'] })

    expect(await screen.findByText('Next campus event')).toBeVisible()
    expect(screen.getByText('Next: Next campus event')).toBeVisible()
    expect(upcomingQuery).toBe('true')
  })
})
