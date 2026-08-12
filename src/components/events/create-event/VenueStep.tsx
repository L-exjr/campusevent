import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import type { EventInput } from '../../../types'

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
                onChange={(event) => onValuesChange({ location: event.target.value })}
              />
              <Form.Control.Feedback type="invalid">
                {errors.location ?? 'Enter the physical venue.'}
              </Form.Control.Feedback>
              <Form.Text>Include enough detail for attendees to find the venue.</Form.Text>
            </Form.Group>
          </Col>
        )}
        {values.format !== 'physical' && (
          <Col md={values.format === 'hybrid' ? 6 : 12}>
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
