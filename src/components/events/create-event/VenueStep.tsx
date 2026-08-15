import { useEffect, useRef, useState } from 'react'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import type { EventInput } from '../../../types'
import InteractiveLocationMap from './InteractiveLocationMap'

const DEFAULT_MAP_LOCATION = { latitude: 6.6745, longitude: -1.5716 }

export interface VenueErrors {
  location?: string
  meetingUrl?: string
}

interface VenueStepProps {
  values: EventInput
  errors?: VenueErrors
  disabled?: boolean
  onValuesChange: (changes: Partial<EventInput>) => void
}

export default function VenueStep({
  values,
  errors = {},
  disabled = false,
  onValuesChange,
}: VenueStepProps) {
  const [mapBusy, setMapBusy] = useState(false)
  const [mapMessage, setMapMessage] = useState<string | null>(null)
  const reverseTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const reverseRequest = useRef<AbortController | null>(null)
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const searchRequest = useRef<AbortController | null>(null)

  useEffect(() => () => {
    if (reverseTimer.current) clearTimeout(reverseTimer.current)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    reverseRequest.current?.abort()
    searchRequest.current?.abort()
  }, [])

  const scheduleAddressSearch = (address: string) => {
    onValuesChange({ location: address, latitude: null, longitude: null })
    setMapMessage(null)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchRequest.current?.abort()
    const query = address.trim()
    if (query.length < 3) {
      setMapBusy(false)
      return
    }

    setMapBusy(true)
    setMapMessage('Finding this address on the map…')
    searchTimer.current = setTimeout(async () => {
      const controller = new AbortController()
      searchRequest.current = controller
      try {
        const response = await fetch(
          `https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q=${encodeURIComponent(query)}`,
          { headers: { Accept: 'application/json' }, signal: controller.signal },
        )
        if (!response.ok) throw new Error('Location search is temporarily unavailable.')
        const [result] = await response.json() as Array<{ lat: string; lon: string; display_name: string }>
        if (!result) throw new Error('No matching location found. Add more address detail or choose the point manually.')
        onValuesChange({ location: result.display_name, latitude: Number(result.lat), longitude: Number(result.lon) })
        setMapMessage('Map pin updated from the address.')
      } catch (caught) {
        if (controller.signal.aborted) return
        setMapMessage(caught instanceof Error ? caught.message : 'Location search failed.')
      } finally {
        if (!controller.signal.aborted) setMapBusy(false)
      }
    }, 650)
  }

  const pickMapLocation = ({ latitude, longitude }: { latitude: number; longitude: number }) => {
    onValuesChange({ latitude, longitude })
    setMapBusy(true)
    setMapMessage('Finding the nearest address…')
    if (reverseTimer.current) clearTimeout(reverseTimer.current)
    reverseRequest.current?.abort()

    reverseTimer.current = setTimeout(async () => {
      const controller = new AbortController()
      reverseRequest.current = controller
      try {
        const response = await fetch(
          `https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${latitude}&lon=${longitude}`,
          { headers: { Accept: 'application/json' }, signal: controller.signal },
        )
        if (!response.ok) throw new Error('No address was found for this point.')
        const result = await response.json() as { display_name?: string; error?: string }
        if (!result.display_name) throw new Error(result.error || 'No address was found for this point.')
        onValuesChange({ location: result.display_name, latitude, longitude })
        setMapMessage('Address updated from the selected map point.')
      } catch {
        if (controller.signal.aborted) return
        const coordinates = `${latitude.toFixed(6)}, ${longitude.toFixed(6)}`
        onValuesChange({ location: coordinates, latitude, longitude })
        setMapMessage('No street address was found. Coordinates are saved; you can replace the address text with a manual venue description.')
      } finally {
        if (!controller.signal.aborted) setMapBusy(false)
      }
    }, 350)
  }
  return (
    <section className="event-wizard-step" aria-labelledby="venue-heading">
      <div className="form-section-heading mb-4">
        <span>02</span>
        <div>
          <h3 id="venue-heading">Venue</h3>
          <p>Choose how attendees will join and provide the relevant location details.</p>
        </div>
      </div>

      <fieldset>
        <legend className="form-label">How will attendees join?</legend>
        <div className="event-choice-grid event-choice-grid--three mb-4">
          <label
            className={`event-choice-card${values.format === 'physical' ? ' is-selected' : ''}`}
          >
            <Form.Check.Input
              type="radio"
              name="create-event-format"
              value="physical"
              checked={values.format === 'physical'}
              disabled={disabled}
              onChange={() => onValuesChange({ format: 'physical' })}
            />
            <span>
              <strong>In person</strong>
              <small>Attendees meet at a physical venue.</small>
            </span>
          </label>

          <label
            className={`event-choice-card${values.format === 'virtual' ? ' is-selected' : ''}`}
          >
            <Form.Check.Input
              type="radio"
              name="create-event-format"
              value="virtual"
              checked={values.format === 'virtual'}
              disabled={disabled}
              onChange={() => onValuesChange({ format: 'virtual' })}
            />
            <span>
              <strong>Virtual</strong>
              <small>Attendees join through an online meeting link.</small>
            </span>
          </label>

          <label
            className={`event-choice-card${values.format === 'hybrid' ? ' is-selected' : ''}`}
          >
            <Form.Check.Input
              type="radio"
              name="create-event-format"
              value="hybrid"
              checked={values.format === 'hybrid'}
              disabled={disabled}
              onChange={() => onValuesChange({ format: 'hybrid' })}
            />
            <span>
              <strong>Hybrid</strong>
              <small>Attendees can join at the venue or through a meeting link.</small>
            </span>
          </label>
        </div>
      </fieldset>

      <Row className="g-3">
        {values.format !== 'virtual' && (
          <Col md={values.format === 'hybrid' ? 6 : 12}>
            <Form.Group controlId="create-event-location">
              <Form.Label>Venue address</Form.Label>
              <Form.Control
                required
                maxLength={300}
                value={values.location}
                isInvalid={Boolean(errors.location)}
                disabled={disabled}
                placeholder="Building, room, or full venue address"
                autoComplete="street-address"
                onChange={(event) => scheduleAddressSearch(event.target.value)}
              />
              <Form.Control.Feedback type="invalid">
                {errors.location ?? 'Enter the physical venue.'}
              </Form.Control.Feedback>
              <Form.Text>Type an address to place the pin automatically, then click the map or drag the pin to refine it.</Form.Text>
              {mapMessage && <div className="small mt-2" role="status">{mapMessage}</div>}
              <InteractiveLocationMap
                latitude={values.latitude ?? DEFAULT_MAP_LOCATION.latitude}
                longitude={values.longitude ?? DEFAULT_MAP_LOCATION.longitude}
                disabled={disabled || mapBusy}
                onPick={pickMapLocation}
              />
            </Form.Group>
          </Col>
        )}
        {values.format !== 'physical' && (
          <Col md={values.format === 'hybrid' ? 6 : 12}>
            <Form.Group controlId="create-event-platform" className="mb-3">
              <Form.Label>Streaming platform</Form.Label>
              <Form.Select required value={values.virtualPlatform ?? ''} disabled={disabled}
                onChange={(event) => onValuesChange({ virtualPlatform: event.target.value as EventInput['virtualPlatform'] })}>
                <option value="">Choose a platform</option>
                <option value="zoom">Zoom</option><option value="googleMeet">Google Meet</option>
                <option value="microsoftTeams">Microsoft Teams</option><option value="youtubeLive">YouTube Live</option>
                <option value="custom">Custom link</option>
              </Form.Select>
            </Form.Group>
            <Form.Group controlId="create-event-meeting-url">
              <Form.Label>Meeting link</Form.Label>
              <Form.Control
                type="url"
                required
                maxLength={2048}
                value={values.meetingUrl ?? ''}
                isInvalid={Boolean(errors.meetingUrl)}
                disabled={disabled}
                placeholder="https://meet.example.com/your-event"
                inputMode="url"
                onChange={(event) => onValuesChange({ meetingUrl: event.target.value })}
              />
              <Form.Control.Feedback type="invalid">
                {errors.meetingUrl ??
                  'Enter a valid meeting link beginning with http:// or https://.'}
              </Form.Control.Feedback>
              <Form.Text>Attendees will use this link to join the event online.</Form.Text>
            </Form.Group>
          </Col>
        )}
      </Row>
    </section>
  )
}
