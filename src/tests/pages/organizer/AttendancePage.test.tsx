import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { apiEvent, event, paginated, users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

describe('AttendancePage organizer flow', () => {
  it('shows the owned event registrants and saves marked attendance', async () => {
    let attended = false
    let savedBody: unknown
    server.use(
      http.get('http://localhost:5080/api/events/:id/management', () => HttpResponse.json(apiEvent())),
      http.get('http://localhost:5080/api/events/:id/registrants', () =>
        HttpResponse.json(paginated([{
          registrationId: 'registration-1',
          studentId: users.student.id,
          studentName: users.student.name,
          studentEmail: users.student.email,
          registeredAt: '2026-07-20T12:00:00Z',
          attended,
        }]))),
      http.put('http://localhost:5080/api/events/:id/attendance', async ({ request }) => {
        savedBody = await request.json()
        attended = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const user = userEvent.setup()
    renderWithAuth(<App />, {
      user: users.organizer,
      initialEntries: [`/organizer/events/${event.id}/attendance`],
    })

    expect(await screen.findByText(users.student.name)).toBeVisible()
    await user.click(screen.getByRole('checkbox', { name: `Mark ${users.student.name} present` }))
    await user.click(screen.getByRole('button', { name: 'Save attendance' }))

    await waitFor(() => {
      expect(savedBody).toEqual({
        registrations: [{ registrationId: 'registration-1', attended: true }],
      })
    })
    expect(screen.getByRole('checkbox', { name: `Mark ${users.student.name} present` }))
      .toBeChecked()
    expect(screen.getByText('1 of 1 marked present')).toBeVisible()
  })
})
