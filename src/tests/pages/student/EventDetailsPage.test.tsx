import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { apiEvent, event, paginated, users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

describe('EventDetailsPage student flow', () => {
  it('lets a Student browse, open an event, and register through the API', async () => {
    let registered = false
    server.use(
      http.get('http://localhost:5080/api/events', () =>
        HttpResponse.json(paginated([apiEvent({ registeredCount: registered ? 2 : 1 })]))),
      http.get('http://localhost:5080/api/events/:id', () =>
        HttpResponse.json(apiEvent({ registeredCount: registered ? 2 : 1 }))),
      http.get('http://localhost:5080/api/events/:id/registration-status', () =>
        HttpResponse.json({ isRegistered: registered })),
      http.post('http://localhost:5080/api/events/:id/register', () => {
        registered = true
        return HttpResponse.json({}, { status: 201 })
      }),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, { user: users.student, initialEntries: ['/events'] })

    expect(await screen.findByRole('heading', { name: event.title })).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'View event' }))
    expect(await screen.findByRole('heading', { name: event.title, level: 1 })).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'Register now' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/you’re registered/i)
    expect(screen.getByRole('button', { name: 'Already registered' })).toBeDisabled()
    expect(registered).toBe(true)
  })
})
