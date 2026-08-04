import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { apiEvent } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

describe('EventsPage pagination', () => {
  it('requests only the selected server page', async () => {
    const requestedPages: string[] = []
    server.use(http.get('http://localhost:5080/api/events', ({ request }) => {
      const page = new URL(request.url).searchParams.get('page') ?? '1'
      requestedPages.push(page)
      return HttpResponse.json({
        items: [apiEvent({ id: `event-${page}`, title: `Event page ${page}` })],
        page: Number(page),
        pageSize: 12,
        totalCount: 13,
        totalPages: 2,
      })
    }))
    const user = userEvent.setup()
    renderWithAuth(<App />, { initialEntries: ['/events'] })

    expect(await screen.findByText('Event page 1')).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(await screen.findByText('Event page 2')).toBeVisible()
    await waitFor(() => expect(requestedPages).toEqual(['1', '2']))
    expect(screen.queryByText('Event page 1')).not.toBeInTheDocument()
  })
})
