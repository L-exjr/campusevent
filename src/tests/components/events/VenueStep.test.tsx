import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import VenueStep from '../../../components/events/create-event/VenueStep'
import type { EventInput } from '../../../types'

vi.mock('../../../components/events/create-event/InteractiveLocationMap', () => ({
  default: ({ onPick }: { onPick: (point: { latitude: number; longitude: number }) => void }) => (
    <button type="button" aria-label="Test map point" onClick={() => onPick({ latitude: 5.6037, longitude: -0.187 })}>
      Map
    </button>
  ),
}))

const physicalValues: EventInput = {
  title: '',
  description: '',
  date: '',
  capacity: 50,
  category: 'Art & Exhibition',
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
  afterEach(() => vi.unstubAllGlobals())

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
    expect(onValuesChange).toHaveBeenLastCalledWith({
      location: 'Great HallA',
      latitude: null,
      longitude: null,
    })
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

  it('searches an address and sets the resolved pin', async () => {
    const onValuesChange = vi.fn()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => [{ lat: '5.5600', lon: '-0.2050', display_name: 'University of Ghana, Accra' }],
    }))
    render(<VenueStep values={physicalValues} onValuesChange={onValuesChange} />)

    await userEvent.clear(screen.getByLabelText('Venue address'))
    await userEvent.type(screen.getByLabelText('Venue address'), 'University of Ghana')

    await waitFor(() => expect(onValuesChange).toHaveBeenCalledWith({
      location: 'University of Ghana, Accra',
      latitude: 5.56,
      longitude: -0.205,
    }), { timeout: 2000 })
    expect(screen.queryByRole('button', { name: /find on map/i })).not.toBeInTheDocument()
  })

  it('sets a clicked map point and reverse-geocodes it into the address', async () => {
    const user = userEvent.setup()
    const onValuesChange = vi.fn()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ display_name: 'Independence Square, Accra' }),
    }))
    render(<VenueStep values={{ ...physicalValues, latitude: 5.55, longitude: -0.2 }} onValuesChange={onValuesChange} />)

    await user.click(screen.getByRole('button', { name: 'Test map point' }))

    expect(onValuesChange).toHaveBeenCalledWith({ latitude: 5.6037, longitude: -0.187 })
    await waitFor(() => expect(onValuesChange).toHaveBeenCalledWith({
      location: 'Independence Square, Accra',
      latitude: 5.6037,
      longitude: -0.187,
    }))
  })

  it('keeps clicked coordinates usable when reverse geocoding finds no address', async () => {
    const user = userEvent.setup()
    const onValuesChange = vi.fn()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => ({ error: 'Unable to geocode' }) }))
    render(<VenueStep values={{ ...physicalValues, latitude: 5.55, longitude: -0.2 }} onValuesChange={onValuesChange} />)

    await user.click(screen.getByRole('button', { name: 'Test map point' }))

    await waitFor(() => expect(onValuesChange).toHaveBeenCalledWith({
      location: '5.603700, -0.187000',
      latitude: 5.6037,
      longitude: -0.187,
    }))
    expect(screen.getByRole('status')).toHaveTextContent(/coordinates are saved/i)
    expect(screen.getByLabelText('Venue address')).toBeRequired()
  })
})
