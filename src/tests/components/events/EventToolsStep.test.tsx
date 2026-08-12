import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import EventToolsStep from '../../../components/events/create-event/EventToolsStep'
import type { EventInput } from '../../../types'

const values: EventInput = {
  title: '',
  description: '',
  date: '2030-08-20T18:30',
  capacity: 50,
  category: 'Academic',
  location: '',
  format: 'physical',
  meetingUrl: null,
  salesStartsAt: '2030-08-01T09:00',
  salesEndsAt: '2030-08-19T18:30',
  imageUrl: null,
  isPublished: true,
  priceMinor: 12500,
  currency: 'GHS',
}

describe('EventToolsStep', () => {
  it('requires one mutually exclusive registration payment mode', () => {
    render(
      <EventToolsStep
        values={values}
        registrationMode="free"
        onRegistrationModeChange={vi.fn()}
        onValuesChange={vi.fn()}
      />,
    )

    expect(screen.getByRole('radio', { name: /free registration/i })).toBeChecked()
    expect(screen.getByRole('radio', { name: /paid ticketing/i })).not.toBeChecked()
    expect(screen.getAllByRole('radio')).toHaveLength(2)
    expect(screen.queryByRole('checkbox', { name: /registration|ticketing/i })).not.toBeInTheDocument()
  })

  it('changes registration mode without clearing preserved paid values', async () => {
    const user = userEvent.setup()
    const onRegistrationModeChange = vi.fn()
    const onValuesChange = vi.fn()
    render(
      <EventToolsStep
        values={values}
        registrationMode="free"
        onRegistrationModeChange={onRegistrationModeChange}
        onValuesChange={onValuesChange}
      />,
    )

    await user.click(screen.getByRole('radio', { name: /paid ticketing/i }))

    expect(onRegistrationModeChange).toHaveBeenCalledWith('paid')
    expect(onValuesChange).not.toHaveBeenCalled()
  })

  it('shows capacity for free registration and keeps paid-only fields hidden', () => {
    render(
      <EventToolsStep
        values={values}
        registrationMode="free"
        onRegistrationModeChange={vi.fn()}
        onValuesChange={vi.fn()}
      />,
    )

    expect(screen.getByLabelText('Capacity')).toHaveValue(50)
    expect(screen.queryByLabelText('Ticket price')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Ticket sales start')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Ticket sales end')).not.toBeInTheDocument()
  })

  it('shows paid sales fields and the inline Paystack disclosure without terms links', () => {
    render(
      <EventToolsStep
        values={values}
        registrationMode="paid"
        onRegistrationModeChange={vi.fn()}
        onValuesChange={vi.fn()}
      />,
    )

    expect(screen.getByLabelText('Ticket price')).toHaveValue(125)
    expect(screen.getByLabelText('Ticket sales start')).toHaveValue('2030-08-01T09:00')
    expect(screen.getByLabelText('Ticket sales end')).toHaveValue('2030-08-19T18:30')
    expect(screen.getByText(/Paystack’s current Ghana processing fee is 1.95%/)).toBeVisible()
    expect(screen.getByText(/platform fee is 0.00%/i)).toBeVisible()
    expect(screen.getByText('GHS 2.44')).toBeVisible()
    expect(screen.getByText('GHS 122.56')).toBeVisible()
    expect(screen.queryByRole('link', { name: /pricing|terms/i })).not.toBeInTheDocument()
  })

  it('presents voting as a static post-creation note', () => {
    render(
      <EventToolsStep
        values={values}
        registrationMode="free"
        onRegistrationModeChange={vi.fn()}
        onValuesChange={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Voting is configured after creation' })).toBeVisible()
    expect(screen.queryByRole('checkbox', { name: /voting/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('textbox', { name: /voting/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /voting/i })).not.toBeInTheDocument()
  })
})
