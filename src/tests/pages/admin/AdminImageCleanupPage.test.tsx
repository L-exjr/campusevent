import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { paginated, users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

const apiUrl = 'http://localhost:5080/api'

describe('AdminImageCleanupPage', () => {
  it('returns a failed image to the cleanup queue', async () => {
    let retried = false
    server.use(
      http.get(`${apiUrl}/image-cleanup/failed`, () => HttpResponse.json(paginated(retried ? [] : [{
        id: 'image-1', bucket: 'event-images', objectKey: 'orphan.webp', kind: 'Event',
        deleteAttemptCount: 8, lifetimeDeleteAttemptCount: 8, manualRetryCount: 0,
        lastRetriedAt: null, lastError: 'Provider unavailable', createdAt: '2026-08-04T10:00:00Z',
      }]))),
      http.put(`${apiUrl}/image-cleanup/:id/retry`, () => {
        retried = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, { user: users.admin, initialEntries: ['/admin/image-cleanup'] })

    await user.click(await screen.findByRole('button', { name: 'Retry' }))

    await waitFor(() => expect(retried).toBe(true))
    expect(await screen.findByText('The image was returned to the cleanup queue.')).toBeVisible()
  })
})
