import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

const apiUrl = 'http://localhost:5080/api'

describe('AdminDashboardPage', () => {
  it('loads one event-report page at a time and navigates to the next page', async () => {
    const requestedPages: string[] = []
    server.use(
      http.get(`${apiUrl}/reports/events`, ({ request }) => {
        const page = new URL(request.url).searchParams.get('page') ?? '1'
        requestedPages.push(page)
        return HttpResponse.json({
          items: [{
            eventId: `event-${page}`,
            eventTitle: `Report page ${page}`,
            organizerId: users.organizer.id,
            organizerName: users.organizer.name,
            registrationCount: 4,
            attendanceCount: 3,
            attendanceRate: 75,
          }],
          page: Number(page),
          pageSize: 20,
          totalCount: 21,
          totalPages: 2,
        })
      }),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, { user: users.admin, initialEntries: ['/admin'] })

    expect(await screen.findByText('Report page 1')).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(await screen.findByText('Report page 2')).toBeVisible()
    await waitFor(() => expect(requestedPages).toEqual(['1', '2']))
    expect(screen.queryByText('Report page 1')).not.toBeInTheDocument()
  })
})
