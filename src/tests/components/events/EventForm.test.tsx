import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import EventForm from '../../../components/events/EventForm'
import { renderWithAuth } from '../../testUtils'

describe('EventForm', () => {
  it('rejects missing required fields and shows validation guidance', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    renderWithAuth(
      <EventForm
        submitLabel="Create event"
        onSubmit={onSubmit}
        onCancel={vi.fn()}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Review event' }))

    expect(screen.getByLabelText('Event title')).toBeInvalid()
    expect(screen.getByLabelText('Description')).toBeInvalid()
    expect(screen.getByLabelText('Event date')).toBeInvalid()
    expect(screen.getByLabelText('Start time')).toBeInvalid()
    expect(screen.getByLabelText('Venue')).toBeInvalid()
    expect(screen.getByText('Enter an event title.')).toBeInTheDocument()
    expect(screen.getByText('Add a short description.')).toBeInTheDocument()
    expect(screen.getByText('Choose a future event date.')).toBeInTheDocument()
    expect(screen.getByText('Choose the start time.')).toBeInTheDocument()
    expect(screen.getByText('Enter the physical venue.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('shows an inline meeting-link error for a virtual event', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    renderWithAuth(
      <EventForm submitLabel="Create event" onSubmit={onSubmit} onCancel={vi.fn()} />,
    )

    await user.selectOptions(screen.getByLabelText('Event format'), 'virtual')
    await user.click(screen.getByRole('button', { name: 'Review event' }))

    expect(screen.getByLabelText('Virtual meeting link')).toBeInvalid()
    expect(screen.getByText('Enter a valid meeting link beginning with http:// or https://.')).toBeVisible()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits a complete virtual event without regressing the working fields', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    const future = new Date(Date.now() + 24 * 60 * 60 * 1000)
    const localFuture = new Date(future.getTime() - future.getTimezoneOffset() * 60_000)
      .toISOString()
      .slice(0, 16)
    renderWithAuth(
      <EventForm submitLabel="Create event" onSubmit={onSubmit} onCancel={vi.fn()} />,
    )

    await user.type(screen.getByLabelText('Event title'), 'Virtual careers workshop')
    await user.type(screen.getByLabelText('Description'), 'A complete virtual workshop for graduating students.')
    await user.type(screen.getByLabelText('Event date'), localFuture.slice(0, 10))
    await user.type(screen.getByLabelText('Start time'), localFuture.slice(11, 16))
    await user.selectOptions(screen.getByLabelText('Event format'), 'virtual')
    await user.type(screen.getByLabelText('Virtual meeting link'), 'https://meet.example.test/careers')
    await user.click(screen.getByRole('button', { name: 'Review event' }))
    expect(screen.getByRole('heading', { name: 'Review before creating' })).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'Create event' }))

    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      title: 'Virtual careers workshop',
      format: 'virtual',
      location: 'Online',
      meetingUrl: 'https://meet.example.test/careers',
    }))
  })

  it('reviews every supported paid field but blocks submission without a verified organizer subaccount', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    const future = new Date(Date.now() + 4 * 24 * 60 * 60 * 1000)
    const eventDate = new Date(future.getTime() - future.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
    const salesStart = new Date(Date.now() + 24 * 60 * 60 * 1000)
    const salesEnd = new Date(Date.now() + 2 * 24 * 60 * 60 * 1000)
    const local = (value: Date) => new Date(value.getTime() - value.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
    renderWithAuth(<EventForm submitLabel="Create event" onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.type(screen.getByLabelText('Event title'), 'Paid event review')
    await user.type(screen.getByLabelText('Description'), 'A complete paid event configuration review.')
    await user.type(screen.getByLabelText('Event date'), eventDate.slice(0, 10))
    await user.type(screen.getByLabelText('Start time'), eventDate.slice(11, 16))
    await user.type(screen.getByLabelText('Venue'), 'Main Hall')
    await user.clear(screen.getByLabelText('Ticket price'))
    await user.type(screen.getByLabelText('Ticket price'), '125')
    await user.type(screen.getByLabelText('Ticket sales start'), local(salesStart))
    await user.type(screen.getByLabelText('Ticket sales end'), local(salesEnd))
    await user.click(screen.getByRole('button', { name: 'Review event' }))

    expect(screen.getByText('GHS 125.00 · Single general-admission price')).toBeVisible()
    expect(screen.getByText('Multiple tiers are not supported by the current payment model.')).toBeVisible()
    expect(screen.getByText('Estimated Paystack fee: 1.95% · GHS 2.44')).toBeVisible()
    expect(screen.getByText('0.00% · GHS 0.00')).toBeVisible()
    expect(screen.getByText('GHS 122.56 per ticket')).toBeVisible()
    expect(screen.getByText('Paystack’s standard Ghana schedule is automatic settlement on the next working day.')).toBeVisible()
    expect(screen.getByRole('button', { name: 'Create event' })).toBeDisabled()
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
