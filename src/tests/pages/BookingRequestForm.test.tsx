import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import BookingRequestForm from '../../pages/BookingRequestForm'
import { server } from '../mocks/server'
import { renderWithAuth } from '../testUtils'

describe('BookingRequestForm', () => {
  it('shows inline guidance for the required request details', async () => {
    const user = userEvent.setup()
    renderWithAuth(<BookingRequestForm />)

    await user.click(screen.getByRole('button', { name: 'Review request' }))

    expect(screen.getByLabelText('Organization name')).toBeInvalid()
    expect(screen.getByLabelText('Preferred date')).toBeInvalid()
    expect(screen.getByLabelText('Preferred start time')).toBeInvalid()
    expect(screen.getByText('Enter the organization name.')).toBeVisible()
    expect(screen.getByText('Choose a preferred date.')).toBeVisible()
  }, 10_000)

  it('reviews before sending the existing booking-request contract', async () => {
    const user = userEvent.setup()
    const payloads: Array<Record<string, unknown>> = []
    server.use(
      http.post('http://localhost:5080/api/booking-requests', async ({ request }) => {
        payloads.push(await request.json() as Record<string, unknown>)
        return HttpResponse.json({ message: 'Request received.' }, { status: 202 })
      }),
    )
    renderWithAuth(<BookingRequestForm />)

    await user.type(screen.getByLabelText('Organization name'), '  Campus Robotics Club  ')
    await user.type(screen.getByLabelText('Contact name'), 'Ama Mensah')
    await user.type(screen.getByLabelText('Email address'), 'ama@example.edu')
    await user.type(screen.getByLabelText('Phone number'), '+233200000000')
    await user.type(screen.getByLabelText('Event type or purpose'), 'Robotics showcase')
    await user.type(screen.getByLabelText('Preferred date'), '2026-09-18')
    await user.type(screen.getByLabelText('Preferred start time'), '14:30')
    await user.clear(screen.getByLabelText('Estimated attendance'))
    await user.type(screen.getByLabelText('Estimated attendance'), '120')
    await user.type(screen.getByLabelText('What support do you need?'), 'We need support coordinating our annual student robotics showcase.')
    await user.click(screen.getByRole('button', { name: 'Review request' }))

    expect(screen.getByRole('heading', { name: 'Review your request' })).toBeVisible()
    expect(screen.getByText('Campus Robotics Club')).toBeVisible()
    expect(payloads).toHaveLength(0)

    await user.click(screen.getByRole('button', { name: 'Send request' }))
    await waitFor(() => expect(payloads).toHaveLength(1))
    expect(payloads[0]).toEqual(expect.objectContaining({
      organizationName: 'Campus Robotics Club',
      eventType: 'Robotics showcase',
      estimatedAttendance: 120,
      website: '',
    }))
    expect(new Date(String(payloads[0].proposedDate)).toISOString()).toBe('2026-09-18T14:30:00.000Z')
  }, 10_000)
})
