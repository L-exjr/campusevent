import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import VenueStep from '../../../components/events/create-event/VenueStep'
import type { EventInput } from '../../../types'

const physicalValues: EventInput = {
  title: '',
  description: '',
  date: '',
  capacity: 50,
  category: 'Academic',
  location: 'Great Hall',
  format: 'physical',
  meetingUrl: 'https://meet.example.test/preserved',
  salesStartsAt: null,
  salesEndsAt: null,
  imageUrl: null,
  isPublished: true,
  priceMinor: 0,
  currency: 'GHS',
}

describe('VenueStep', () => {
  it('offers in-person, virtual, and hybrid venue types', () => {
    render(<VenueStep values={physicalValues} onValuesChange={vi.fn()} />)

    expect(screen.getByRole('radio', { name: /in person/i })).toBeChecked()
    expect(screen.getByRole('radio', { name: /virtual/i })).not.toBeChecked()
    expect(screen.getByRole('radio', { name: /hybrid/i })).not.toBeChecked()
    expect(screen.getAllByRole('radio')).toHaveLength(3)
  })

  it('shows and updates the physical venue address', async () => {
    const user = userEvent.setup()
    const onValuesChange = vi.fn()
    render(<VenueStep values={physicalValues} onValuesChange={onValuesChange} />)

    expect(screen.getByLabelText('Venue address')).toHaveValue('Great Hall')
    expect(screen.queryByLabelText('Meeting link')).not.toBeInTheDocument()

    await user.type(screen.getByLabelText('Venue address'), 'A')
    expect(onValuesChange).toHaveBeenLastCalledWith({ location: 'Great HallA' })
  })

  it('selects virtual without clearing preserved venue data', async () => {
    const user = userEvent.setup()
    const onValuesChange = vi.fn()
    render(<VenueStep values={physicalValues} onValuesChange={onValuesChange} />)

    await user.click(screen.getByRole('radio', { name: /virtual/i }))

    expect(onValuesChange).toHaveBeenCalledWith({ format: 'virtual' })
    expect(onValuesChange).not.toHaveBeenCalledWith(expect.objectContaining({ location: '' }))
    expect(onValuesChange).not.toHaveBeenCalledWith(expect.objectContaining({ meetingUrl: null }))
  })

  it('shows the meeting link and supplied validation error for virtual events', () => {
    render(
      <VenueStep
        values={{ ...physicalValues, format: 'virtual' }}
        errors={{ meetingUrl: 'Enter a valid HTTPS meeting link.' }}
        onValuesChange={vi.fn()}
      />,
    )

    expect(screen.queryByLabelText('Venue address')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Meeting link')).toHaveValue(
      'https://meet.example.test/preserved',
    )
    expect(screen.getByLabelText('Meeting link')).toHaveClass('is-invalid')
    expect(screen.getByText('Enter a valid HTTPS meeting link.')).toBeVisible()
  })

  it('requires both venue details for hybrid events', () => {
    render(
      <VenueStep
        values={{ ...physicalValues, format: 'hybrid' }}
        onValuesChange={vi.fn()}
      />,
    )

    expect(screen.getByLabelText('Venue address')).toHaveValue('Great Hall')
    expect(screen.getByLabelText('Meeting link')).toHaveValue(
      'https://meet.example.test/preserved',
    )
  })
})
