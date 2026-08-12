import Alert from 'react-bootstrap/Alert'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import type { EventInput } from '../../../types'
import {
  calculatePaidEventSettlement,
  PAYSTACK_GHANA_PROCESSING_FEE_BASIS_POINTS,
  PLATFORM_FEE_BASIS_POINTS,
} from '../../../utils/paymentPolicy'

export type RegistrationMode = 'free' | 'paid'

export interface EventToolsErrors {
  capacity?: string
  priceMinor?: string
  salesStartsAt?: string
  salesEndsAt?: string
}

interface EventToolsStepProps {
  values: EventInput
  registrationMode: RegistrationMode
  errors?: EventToolsErrors
  disabled?: boolean
  onRegistrationModeChange: (mode: RegistrationMode) => void
  onValuesChange: (changes: Partial<EventInput>) => void
}

function formatMoney(amountMinor: number) {
  return `GHS ${(amountMinor / 100).toFixed(2)}`
}

export default function EventToolsStep({
  values,
  registrationMode,
  errors = {},
  disabled = false,
  onRegistrationModeChange,
  onValuesChange,
}: EventToolsStepProps) {
  const settlement = calculatePaidEventSettlement(values.priceMinor)

  return (
    <section className="event-wizard-step" aria-labelledby="event-tools-heading">
      <div className="form-section-heading mb-4">
        <span>03</span>
        <div>
          <h3 id="event-tools-heading">Event tools</h3>
          <p>Set registration capacity and choose how attendees secure their place.</p>
        </div>
      </div>

      <fieldset>
        <legend className="form-label">Registration payment mode</legend>
        <div className="event-choice-grid mb-4">
          <label
            className={`event-choice-card${registrationMode === 'free' ? ' is-selected' : ''}`}
          >
            <Form.Check.Input
              type="radio"
              name="create-event-registration-mode"
              value="free"
              checked={registrationMode === 'free'}
              disabled={disabled}
              onChange={() => onRegistrationModeChange('free')}
            />
            <span>
              <strong>Free registration</strong>
              <small>Attendees register without making a payment.</small>
            </span>
          </label>

          <label
            className={`event-choice-card${registrationMode === 'paid' ? ' is-selected' : ''}`}
          >
            <Form.Check.Input
              type="radio"
              name="create-event-registration-mode"
              value="paid"
              checked={registrationMode === 'paid'}
              disabled={disabled}
              onChange={() => onRegistrationModeChange('paid')}
            />
            <span>
              <strong>Paid ticketing</strong>
              <small>Attendees pay one general-admission ticket price.</small>
            </span>
          </label>
        </div>
      </fieldset>

      <Row className="g-3">
        <Col md={registrationMode === 'paid' ? 6 : 12}>
          <Form.Group controlId="create-event-capacity">
            <Form.Label>Capacity</Form.Label>
            <Form.Control
              type="number"
              required
              min={1}
              max={100000}
              step={1}
              value={values.capacity}
              isInvalid={Boolean(errors.capacity)}
              disabled={disabled}
              onChange={(event) => onValuesChange({ capacity: Number(event.target.value) })}
            />
            <Form.Control.Feedback type="invalid">
              {errors.capacity ?? 'Event capacity must be between 1 and 100000.'}
            </Form.Control.Feedback>
            <Form.Text>Registration closes when this number of attendees is reached.</Form.Text>
          </Form.Group>
        </Col>

        {registrationMode === 'paid' && (
          <>
            <Col md={6}>
              <Form.Group controlId="create-event-price">
                <Form.Label>Ticket price</Form.Label>
                <div className="input-group">
                  <span className="input-group-text">GHS</span>
                  <Form.Control
                    type="number"
                    required
                    min="0.01"
                    step="0.01"
                    value={(values.priceMinor / 100).toFixed(2)}
                    isInvalid={Boolean(errors.priceMinor)}
                    disabled={disabled}
                    onChange={(event) => {
                      const amount = Number(event.target.value)
                      onValuesChange({
                        priceMinor: Number.isFinite(amount)
                          ? Math.max(0, Math.round(amount * 100))
                          : 0,
                      })
                    }}
                  />
                  <Form.Control.Feedback type="invalid">
                    {errors.priceMinor ?? 'Enter a ticket price greater than zero.'}
                  </Form.Control.Feedback>
                </div>
                <Form.Text>One general-admission price applies to every registration.</Form.Text>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group controlId="create-event-sales-start">
                <Form.Label>Ticket sales start</Form.Label>
                <Form.Control
                  type="datetime-local"
                  required
                  value={values.salesStartsAt ?? ''}
                  isInvalid={Boolean(errors.salesStartsAt)}
                  disabled={disabled}
                  onChange={(event) =>
                    onValuesChange({ salesStartsAt: event.target.value || null })
                  }
                />
                <Form.Control.Feedback type="invalid">
                  {errors.salesStartsAt ?? 'Choose when ticket sales open.'}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group controlId="create-event-sales-end">
                <Form.Label>Ticket sales end</Form.Label>
                <Form.Control
                  type="datetime-local"
                  required
                  max={values.date || undefined}
                  value={values.salesEndsAt ?? ''}
                  isInvalid={Boolean(errors.salesEndsAt)}
                  disabled={disabled}
                  onChange={(event) =>
                    onValuesChange({ salesEndsAt: event.target.value || null })
                  }
                />
                <Form.Control.Feedback type="invalid">
                  {errors.salesEndsAt ??
                    'Sales must end after they start and no later than the event.'}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col xs={12}>
              <Alert variant="warning" className="mb-0">
                <Alert.Heading as="h4" className="h6">Paid-ticket fee disclosure</Alert.Heading>
                <p className="mb-2">
                  Paystack’s current Ghana processing fee is{' '}
                  {(PAYSTACK_GHANA_PROCESSING_FEE_BASIS_POINTS / 100).toFixed(2)}%. The
                  platform fee is {(PLATFORM_FEE_BASIS_POINTS / 100).toFixed(2)}%.
                  Paystack’s standard Ghana schedule automatically settles funds on the next
                  working day.
                </p>
                <dl className="event-fee-summary mb-2">
                  <div>
                    <dt>Ticket price</dt>
                    <dd>{formatMoney(values.priceMinor)}</dd>
                  </div>
                  <div>
                    <dt>Estimated Paystack fee</dt>
                    <dd>{formatMoney(settlement.processingFeeMinor)}</dd>
                  </div>
                  <div>
                    <dt>Platform fee</dt>
                    <dd>{formatMoney(settlement.platformFeeMinor)}</dd>
                  </div>
                  <div>
                    <dt>Estimated settlement</dt>
                    <dd>{formatMoney(settlement.estimatedNetMinor)}</dd>
                  </div>
                </dl>
                <p className="mb-0">
                  Paid-event creation remains unavailable until an administrator provisions
                  and verifies the organizer’s Paystack subaccount.
                </p>
              </Alert>
            </Col>
          </>
        )}

        <Col xs={12}>
          <Alert variant="info" className="mb-0" role="note">
            <Alert.Heading as="h4" className="h6">Voting is configured after creation</Alert.Heading>
            Once this event has been created, voting categories, nominees, pricing, and dates
            can be configured from the event dashboard.
          </Alert>
        </Col>
      </Row>
    </section>
  )
}
