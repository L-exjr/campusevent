import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import EventCreationWizard from '../../../components/events/create-event/EventCreationWizard'

function futureLocalDate(minutesFromNow = 24 * 60) {
  const value = new Date(Date.now() + minutesFromNow * 60_000)
  const local = new Date(value.getTime() - value.getTimezoneOffset() * 60_000)
    .toISOString()
    .slice(0, 16)
  return { date: local.slice(0, 10), time: local.slice(11, 16), dateTime: local }
}

function completeBasicInformation(minutesFromNow = 24 * 60) {
  const future = futureLocalDate(minutesFromNow)
  fireEvent.change(screen.getByLabelText('Event title'), {
    target: { value: 'Campus technology forum' },
  })
  fireEvent.change(screen.getByLabelText('Description'), {
    target: { value: 'A detailed technology forum for the entire campus community.' },
  })
  fireEvent.change(screen.getByLabelText('Start date'), {
    target: { value: future.date },
  })
  fireEvent.change(screen.getByLabelText('Start time'), {
    target: { value: future.time },
  })
  const end = futureLocalDate(minutesFromNow + 60)
  fireEvent.change(screen.getByLabelText('End date'), {
    target: { value: end.date },
  })
  fireEvent.change(screen.getByLabelText('End time'), {
    target: { value: end.time },
  })
  return future
}

function continueToVenue(minutesFromNow = 24 * 60) {
  const future = completeBasicInformation(minutesFromNow)
  fireEvent.click(screen.getByRole('button', { name: 'Continue' }))
  return future
}

function continueToTools(format: 'physical' | 'virtual' | 'hybrid' = 'physical') {
  const future = continueToVenue()
  fireEvent.click(screen.getByRole('radio', {
    name: format === 'physical' ? /in person/i : new RegExp(format, 'i'),
  }))
  if (format !== 'virtual') {
    fireEvent.change(screen.getByLabelText('Venue address'), {
      target: { value: 'Engineering Auditorium' },
    })
  }
  if (format !== 'physical') {
    fireEvent.change(screen.getByLabelText('Streaming platform'), {
      target: { value: 'googleMeet' },
    })
    fireEvent.change(screen.getByLabelText('Meeting link'), {
      target: { value: 'https://meet.example.test/campus-forum' },
    })
  }
  fireEvent.click(screen.getByRole('button', { name: 'Continue' }))
  return future
}

describe('EventCreationWizard', () => {
  it('validates only the visible step when advancing', () => {
    const onSubmit = vi.fn()
    render(
      <EventCreationWizard onSubmit={onSubmit} onCancel={vi.fn()} />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Continue' }))

    expect(screen.getByLabelText('Event title')).toHaveClass('is-invalid')
    expect(screen.getByLabelText('Description')).toHaveClass('is-invalid')
    expect(screen.getByLabelText('Start date')).toHaveClass('is-invalid')
    expect(screen.queryByLabelText('Venue address')).not.toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('preserves data across backward, forward, and venue-mode navigation', () => {
    render(
      <EventCreationWizard onSubmit={vi.fn()} onCancel={vi.fn()} />,
    )

    continueToVenue()
    fireEvent.click(screen.getByRole('radio', { name: /hybrid/i }))
    fireEvent.change(screen.getByLabelText('Venue address'), {
      target: { value: 'Great Hall' },
    })
    fireEvent.change(screen.getByLabelText('Meeting link'), {
      target: { value: 'https://meet.example.test/preserved' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Back' }))

    expect(screen.getByLabelText('Event title')).toHaveValue('Campus technology forum')
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }))
    expect(screen.getByRole('radio', { name: /hybrid/i })).toBeChecked()
    expect(screen.getByLabelText('Venue address')).toHaveValue('Great Hall')
    expect(screen.getByLabelText('Meeting link')).toHaveValue(
      'https://meet.example.test/preserved',
    )
  })

  it('keeps free registration and paid ticketing mutually exclusive', () => {
    render(
      <EventCreationWizard onSubmit={vi.fn()} onCancel={vi.fn()} />,
    )

    continueToTools()
    const registrations = screen.getByRole('checkbox', { name: /registrations/i })
    const ticketing = screen.getByRole('checkbox', { name: /ticketing/i })

    expect(registrations).toBeChecked()
    expect(ticketing).not.toBeChecked()
    fireEvent.click(ticketing)
    expect(registrations).not.toBeChecked()
    expect(ticketing).toBeChecked()
  })

  it('submits only the existing payload keys for a free virtual draft', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <EventCreationWizard onSubmit={onSubmit} onCancel={vi.fn()} />,
    )

    const future = continueToTools('virtual')
    fireEvent.change(screen.getByLabelText('Capacity'), { target: { value: '80' } })
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }))
    fireEvent.click(screen.getByRole('radio', { name: /save as draft/i }))
    fireEvent.click(screen.getByRole('button', { name: 'Create draft' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
    const payload = onSubmit.mock.calls[0][0]
    expect(payload).toMatchObject({
      title: 'Campus technology forum',
      description: 'A detailed technology forum for the entire campus community.',
      date: future.dateTime,
      endDate: futureLocalDate(25 * 60).dateTime,
      capacity: 80,
      category: 'Art & Exhibition',
      location: 'Online',
      format: 'virtual',
      meetingUrl: 'https://meet.example.test/campus-forum',
      virtualPlatform: 'googleMeet',
      ticketingEnabled: false,
      registrationsEnabled: true,
      votingEnabled: false,
      salesStartsAt: null,
      salesEndsAt: null,
      imageUrl: null,
      isPublished: false,
      priceMinor: 0,
      currency: 'GHS',
    })
  })

  it('runs a complete validation pass immediately before submission', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2030-01-01T10:00:00Z'))
    const onSubmit = vi.fn()
    render(
      <EventCreationWizard onSubmit={onSubmit} onCancel={vi.fn()} />,
    )

    continueToVenue(10)
    fireEvent.change(screen.getByLabelText('Venue address'), {
      target: { value: 'Great Hall' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }))
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }))
    expect(screen.getByRole('heading', { name: 'Review and create' })).toBeVisible()

    vi.setSystemTime(new Date('2030-01-01T10:20:00Z'))
    fireEvent.click(screen.getByRole('button', { name: 'Publish event' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(screen.getByRole('heading', { name: 'Basic information' })).toBeVisible()
    expect(screen.getByText('Choose a future event date.')).toBeVisible()
    vi.useRealTimers()
  })
})
