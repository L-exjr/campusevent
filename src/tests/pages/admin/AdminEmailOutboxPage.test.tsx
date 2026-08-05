import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { paginated, users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

const apiUrl = 'http://localhost:5080/api'

describe('AdminEmailOutboxPage', () => {
  it('retries a recoverable failed message', async () => {
    const message = {
      id: 'dead-letter-1',
      kind: 'RegistrationConfirmation',
      aggregateId: 'registration-1',
      attemptCount: 8,
      lifetimeAttemptCount: 8,
      manualRetryCount: 0,
      lastRetriedAt: null,
      lastError: 'Provider unavailable',
      createdAt: '2026-08-04T10:00:00Z',
      canRetry: true,
    }
    let retried = false
    server.use(
      http.get(`${apiUrl}/email-outbox/failed`, () =>
        HttpResponse.json(paginated(retried ? [] : [message]))),
      http.put(`${apiUrl}/email-outbox/:id/retry`, () => {
        retried = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, { user: users.admin, initialEntries: ['/admin/email-outbox'] })

    await user.click(await screen.findByRole('button', { name: 'Retry' }))

    await waitFor(() => expect(retried).toBe(true))
    expect(await screen.findByText('The email was returned to the delivery queue.')).toBeVisible()
  })
})
