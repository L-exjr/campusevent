import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import type { EventInput, EventItem } from '../../types'
import { EVENT_CATEGORIES } from '../../types'
import { formatDateTime, toDateTimeLocal } from '../../utils/formatters'
import {
  calculatePaidEventSettlement,
  PAYSTACK_GHANA_PROCESSING_FEE_BASIS_POINTS,
  PLATFORM_FEE_BASIS_POINTS,
} from '../../utils/paymentPolicy'
import {
  DEFAULT_EVENT_IMAGE,
  IMAGE_ACCEPT,
  uploadImage,
  validateImageFile,
} from '../../api/imageStorage'

interface EventFormProps {
  event?: EventItem | null
  busy?: boolean
  error?: string | null
  submitLabel: string
  onSubmit: (input: EventInput) => Promise<void>
  onCancel: () => void
}

function initialValues(event?: EventItem | null): EventInput {
  return event
    ? {
        title: event.title,
        description: event.description,
        date: toDateTimeLocal(event.date),
        capacity: event.capacity,
        category: event.category,
        location: event.location,
        format: event.format,
        meetingUrl: event.meetingUrl,
        salesStartsAt: event.salesStartsAt ? toDateTimeLocal(event.salesStartsAt) : null,
        salesEndsAt: event.salesEndsAt ? toDateTimeLocal(event.salesEndsAt) : null,
        imageUrl: event.imageUrl,
        isPublished: event.isPublished,
        version: event.version,
        priceMinor: event.priceMinor,
        currency: event.currency,
      }
    : {
        title: '',
        description: '',
        date: '',
        capacity: 50,
        category: 'Art & Exhibition',
        location: '',
        format: 'physical',
        meetingUrl: null,
        salesStartsAt: null,
        salesEndsAt: null,
        imageUrl: null,
        isPublished: true,
        priceMinor: 0,
        currency: 'GHS',
      }
}

function splitDateTime(value: string | null | undefined) {
  if (!value) return { date: '', time: '' }
  return { date: value.slice(0, 10), time: value.slice(11, 16) }
}

function combineDateTime(date: string, time: string) {
  return date && time ? `${date}T${time}` : ''
}

export default function EventForm({
  event,
  busy = false,
  error,
  submitLabel,
  onSubmit,
  onCancel,
}: EventFormProps) {
  const [initial] = useState(() => initialValues(event))
  const [values, setValues] = useState<EventInput>(initial)
  const initialEventDateTime = splitDateTime(initial.date)
  const [eventDate, setEventDate] = useState(initialEventDateTime.date)
  const [eventTime, setEventTime] = useState(initialEventDateTime.time)
  const [validated, setValidated] = useState(false)
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imagePreview, setImagePreview] = useState(event?.imageUrl ?? DEFAULT_EVENT_IMAGE)
  const [imageError, setImageError] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [reviewing, setReviewing] = useState(false)
  const [minimumDateTime] = useState(() =>
    toDateTimeLocal(new Date(Date.now() + 5 * 60_000).toISOString()),
  )
  const paidSettlement = calculatePaidEventSettlement(values.priceMinor)

  useEffect(() => () => {
    if (imagePreview.startsWith('blob:')) URL.revokeObjectURL(imagePreview)
  }, [imagePreview])

  const handleImageChange = (change: ChangeEvent<HTMLInputElement>) => {
    const file = change.target.files?.[0]
    setImageError(null)
    if (!file) {
      setImageFile(null)
      setImagePreview(values.imageUrl ?? DEFAULT_EVENT_IMAGE)
      return
    }
    try {
      validateImageFile(file)
      setImageFile(file)
      setImagePreview(URL.createObjectURL(file))
    } catch (caught) {
      change.target.value = ''
      setImageFile(null)
      setImageError(caught instanceof Error ? caught.message : 'Choose a valid image.')
    }
  }

  const handleSubmit = async (submission: FormEvent<HTMLFormElement>) => {
    submission.preventDefault()
    const form = submission.currentTarget
    const normalizedValues = {
      ...values,
      title: values.title.trim(),
      description: values.description.trim(),
      date: combineDateTime(eventDate, eventTime),
      location: values.format === 'virtual' ? 'Online' : values.location.trim(),
      meetingUrl: values.format === 'physical' ? null : values.meetingUrl?.trim() || null,
      salesStartsAt: values.salesStartsAt || null,
      salesEndsAt: values.salesEndsAt || null,
    }
    const hasValidLocation = normalizedValues.location.length > 0
    const hasValidMeetingUrl = Boolean(
          normalizedValues.meetingUrl &&
          /^https?:\/\//i.test(normalizedValues.meetingUrl),
        )
    const hasValidVenue = normalizedValues.format === 'physical'
      ? hasValidLocation
      : normalizedValues.format === 'virtual'
        ? hasValidMeetingUrl
        : hasValidLocation && hasValidMeetingUrl
    const hasValidTrimmedText =
      normalizedValues.title.length >= 3 &&
      normalizedValues.description.length >= 10 &&
      hasValidVenue
    const salesStart = normalizedValues.salesStartsAt
      ? new Date(normalizedValues.salesStartsAt).getTime()
      : Number.NaN
    const salesEnd = normalizedValues.salesEndsAt
      ? new Date(normalizedValues.salesEndsAt).getTime()
      : Number.NaN
    const eventStart = new Date(normalizedValues.date).getTime()
    const hasValidSalesWindow = normalizedValues.priceMinor === 0 || (
      Number.isFinite(salesStart) && Number.isFinite(salesEnd) &&
      salesStart < salesEnd && salesEnd <= eventStart
    )
    if (!form.checkValidity() || !hasValidTrimmedText || !hasValidSalesWindow) {
      submission.stopPropagation()
      setValidated(true)
      return
    }
    if (!reviewing) {
      setValidated(true)
      setReviewing(true)
      return
    }
    setUploading(true)
    setImageError(null)
    try {
      const imageUrl = imageFile
        ? await uploadImage(imageFile, 'event-images')
        : normalizedValues.imageUrl
      await onSubmit({ ...normalizedValues, imageUrl })
    } catch (caught) {
      setImageError(caught instanceof Error ? caught.message : 'The image could not be uploaded.')
    } finally {
      setUploading(false)
    }
  }

  return (
    <Form
      noValidate
      validated={validated}
      aria-busy={busy || uploading}
      onSubmit={(submission) => void handleSubmit(submission)}
    >
      <ol className="form-progress" aria-label="Event form progress">
        <li
          className={!reviewing ? 'is-active' : 'is-complete'}
          aria-current={!reviewing ? 'step' : undefined}
        >
          <span>1</span> Event details
        </li>
        <li className={reviewing ? 'is-active' : ''} aria-current={reviewing ? 'step' : undefined}>
          <span>2</span> Review
        </li>
      </ol>
      {(error || imageError) && <Alert variant="danger" role="alert">{error ?? imageError}</Alert>}
      {reviewing ? <>
        <h3 className="h5">Review before {event ? 'saving' : 'creating'}</h3>
        <p className="text-secondary">Confirm the public details below. Use Back to edit if anything needs changing.</p>
        <dl className="row review-list mb-3">
          <dt className="col-sm-4">Event</dt><dd className="col-sm-8">{values.title}</dd>
          <dt className="col-sm-4">Starts</dt><dd className="col-sm-8">{formatDateTime(values.date)}</dd>
          <dt className="col-sm-4">Format</dt><dd className="col-sm-8">{values.format === 'physical' ? values.location : values.format === 'virtual' ? `Virtual · ${values.meetingUrl}` : `Hybrid · ${values.location} · ${values.meetingUrl}`}</dd>
          <dt className="col-sm-4">Capacity</dt><dd className="col-sm-8">{values.capacity}</dd>
          <dt className="col-sm-4">Ticketing</dt><dd className="col-sm-8">{values.priceMinor > 0 ? `${values.currency} ${(values.priceMinor / 100).toFixed(2)} · Single general-admission price` : 'Free event'}</dd>
          {values.priceMinor > 0 && <>
            <dt className="col-sm-4">Ticket tiers</dt><dd className="col-sm-8">Multiple tiers are not supported by the current payment model.</dd>
            <dt className="col-sm-4">Sales window</dt><dd className="col-sm-8">{values.salesStartsAt} to {values.salesEndsAt}</dd>
            <dt className="col-sm-4">Processing fee</dt><dd className="col-sm-8">Estimated Paystack fee: {(PAYSTACK_GHANA_PROCESSING_FEE_BASIS_POINTS / 100).toFixed(2)}% · {values.currency} {(paidSettlement.processingFeeMinor / 100).toFixed(2)}</dd>
            <dt className="col-sm-4">Platform fee</dt><dd className="col-sm-8">{(PLATFORM_FEE_BASIS_POINTS / 100).toFixed(2)}% · {values.currency} {(paidSettlement.platformFeeMinor / 100).toFixed(2)}</dd>
            <dt className="col-sm-4">Estimated settlement</dt><dd className="col-sm-8">{values.currency} {(paidSettlement.estimatedNetMinor / 100).toFixed(2)} per ticket</dd>
            <dt className="col-sm-4">Settlement timing</dt><dd className="col-sm-8">Paystack’s standard Ghana schedule is automatic settlement on the next working day.</dd>
          </>}
        </dl>
        {values.priceMinor > 0 && <Alert variant="warning">
          Paid-event creation remains blocked until an administrator provisions and verifies an organizer-specific Paystack subaccount. This prevents ticket revenue from being routed to an unverified destination.
        </Alert>}
      </> : <Row className="g-3">
        <Col xs={12}>
          <div className="form-section-heading">
            <span>01</span>
            <div><h3>Event story</h3><p>Give students enough context to decide whether to attend.</p></div>
          </div>
        </Col>
        <Col xs={12}>
          <Form.Group controlId="event-image">
            <Form.Label>Cover image</Form.Label>
            <div className="d-flex flex-column flex-md-row gap-3 align-items-md-center">
              <img
                src={imagePreview}
                alt="Event cover preview"
                className="rounded border object-fit-cover"
                style={{ width: 220, aspectRatio: '16 / 9' }}
              />
              <div className="flex-grow-1">
                <Form.Control
                  type="file"
                  accept={IMAGE_ACCEPT}
                  onChange={handleImageChange}
                  disabled={busy || uploading}
                />
                <Form.Text>Optional. JPG, PNG, or WebP; maximum 5 MB.</Form.Text>
              </div>
            </div>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <Form.Group controlId="event-title">
            <Form.Label>Event title</Form.Label>
            <Form.Control
              required
              minLength={3}
              maxLength={200}
              isInvalid={validated && values.title.trim().length < 3}
              value={values.title}
              onChange={(eventValue) => setValues({ ...values, title: eventValue.target.value })}
              placeholder="e.g. Future of AI Symposium"
            />
            <Form.Control.Feedback type="invalid">Enter an event title.</Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <Form.Group controlId="event-description">
            <Form.Label>Description</Form.Label>
            <Form.Control
              as="textarea"
              rows={4}
              required
              minLength={10}
              maxLength={5000}
              isInvalid={validated && values.description.trim().length < 10}
              value={values.description}
              onChange={(eventValue) =>
                setValues({ ...values, description: eventValue.target.value })
              }
              placeholder="What should attendees expect?"
            />
            <Form.Control.Feedback type="invalid">Add a short description.</Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <div className="form-section-heading mt-2">
            <span>02</span>
            <div><h3>Schedule and place</h3><p>Set when it starts and how attendees will join.</p></div>
          </div>
        </Col>
        <Col md={4}>
          <Form.Group controlId="event-date">
            <Form.Label>Event date</Form.Label>
            <Form.Control
              type="date"
              required
              min={event ? undefined : minimumDateTime.slice(0, 10)}
              value={eventDate}
              onChange={(eventValue) => {
                const date = eventValue.target.value
                setEventDate(date)
                setValues({ ...values, date: combineDateTime(date, eventTime) })
              }}
            />
            <Form.Control.Feedback type="invalid">
              {event ? 'Choose the event date.' : 'Choose a future event date.'}
            </Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col md={4}>
          <Form.Group controlId="event-time">
            <Form.Label>Start time</Form.Label>
            <Form.Control
              type="time"
              required
              min={!event && eventDate === minimumDateTime.slice(0, 10) ? minimumDateTime.slice(11, 16) : undefined}
              value={eventTime}
              onChange={(eventValue) => {
                const time = eventValue.target.value
                setEventTime(time)
                setValues({ ...values, date: combineDateTime(eventDate, time) })
              }}
            />
            <Form.Control.Feedback type="invalid">Choose the start time.</Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col md={4}>
          <Form.Group controlId="event-category">
            <Form.Label>Category</Form.Label>
            <Form.Select
              value={values.category}
              onChange={(eventValue) =>
                setValues({
                  ...values,
                  category: eventValue.target.value as EventInput['category'],
                })
              }
            >
              {EVENT_CATEGORIES.map((category) => (
                <option key={category}>{category}</option>
              ))}
            </Form.Select>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <Form.Group controlId="event-format">
            <Form.Label>Event format</Form.Label>
            <Form.Select
              value={values.format}
              onChange={(eventValue) => setValues({
                ...values,
                format: eventValue.target.value as EventInput['format'],
              })}
            >
              <option value="physical">Physical venue</option>
              <option value="virtual">Virtual meeting</option>
              <option value="hybrid">Hybrid venue and virtual meeting</option>
            </Form.Select>
          </Form.Group>
        </Col>
        {values.format !== 'virtual' && <Col md={values.format === 'hybrid' ? 6 : 8}>
          <Form.Group controlId="event-location">
            <Form.Label>Venue</Form.Label>
            <Form.Control
              required
              maxLength={300}
              isInvalid={validated && values.location.trim().length === 0}
              value={values.location}
              onChange={(eventValue) => setValues({ ...values, location: eventValue.target.value })}
              placeholder="Building, room, or full venue address"
            />
            <Form.Control.Feedback type="invalid">Enter the physical venue.</Form.Control.Feedback>
          </Form.Group>
        </Col>}
        {values.format !== 'physical' && <Col md={values.format === 'hybrid' ? 6 : 8}>
          <Form.Group controlId="event-meeting-url">
            <Form.Label>Virtual meeting link</Form.Label>
            <Form.Control
              type="url"
              required
              maxLength={2048}
              isInvalid={validated && !(
                values.meetingUrl && /^https?:\/\//i.test(values.meetingUrl.trim())
              )}
              value={values.meetingUrl ?? ''}
              onChange={(eventValue) => setValues({ ...values, meetingUrl: eventValue.target.value })}
              placeholder="https://meet.example.com/your-event"
            />
            <Form.Control.Feedback type="invalid">Enter a valid meeting link beginning with http:// or https://.</Form.Control.Feedback>
          </Form.Group>
        </Col>}
        <Col md={4}>
          <Form.Group controlId="event-capacity">
            <Form.Label>Capacity</Form.Label>
            <Form.Control
              type="number"
              required
              min={1}
              max={100000}
              value={values.capacity}
              onChange={(eventValue) =>
                setValues({ ...values, capacity: Number(eventValue.target.value) })
              }
            />
            <Form.Control.Feedback type="invalid">Use at least 1.</Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <div className="form-section-heading mt-2">
            <span>03</span>
            <div><h3>Tickets and visibility</h3><p>Choose free or paid admission, then decide whether to publish.</p></div>
          </div>
        </Col>
        <Col md={8}>
          <Form.Group controlId="event-price">
            <Form.Label>Ticket price</Form.Label>
            <Form.Control
              type="number"
              required
              min={0}
              step="0.01"
              value={(values.priceMinor / 100).toFixed(2)}
              onChange={(eventValue) =>
                setValues({
                  ...values,
                  priceMinor: Math.max(0, Math.round(Number(eventValue.target.value) * 100)),
                  salesStartsAt: Number(eventValue.target.value) > 0 ? values.salesStartsAt : null,
                  salesEndsAt: Number(eventValue.target.value) > 0 ? values.salesEndsAt : null,
                })
              }
            />
            <Form.Text>Use 0.00 for a free event. Price cannot change after payment or registration activity begins.</Form.Text>
          </Form.Group>
        </Col>
        {values.priceMinor > 0 && <>
          <Col md={6}>
            <Form.Group controlId="event-sales-start">
              <Form.Label>Ticket sales start</Form.Label>
              <Form.Control
                type="datetime-local"
                required
                isInvalid={validated && !values.salesStartsAt}
                value={values.salesStartsAt ?? ''}
                onChange={(eventValue) => setValues({ ...values, salesStartsAt: eventValue.target.value })}
              />
              <Form.Control.Feedback type="invalid">Choose when ticket sales open.</Form.Control.Feedback>
            </Form.Group>
          </Col>
          <Col md={6}>
            <Form.Group controlId="event-sales-end">
              <Form.Label>Ticket sales end</Form.Label>
              <Form.Control
                type="datetime-local"
                required
                max={values.date || undefined}
                isInvalid={validated && (!values.salesEndsAt || !values.salesStartsAt || values.salesEndsAt <= values.salesStartsAt || Boolean(values.date && values.salesEndsAt > values.date))}
                value={values.salesEndsAt ?? ''}
                onChange={(eventValue) => setValues({ ...values, salesEndsAt: eventValue.target.value })}
              />
              <Form.Control.Feedback type="invalid">Sales must end after they start and no later than the event.</Form.Control.Feedback>
            </Form.Group>
          </Col>
          <Col xs={12}>
            <Alert variant="warning" className="mb-0">
              Paystack’s current Ghana processing fee is 1.95%, with automatic settlement on the next working day. This project adds no platform fee. Paid creation stays unavailable until the organizer has a verified Paystack subaccount.
            </Alert>
          </Col>
        </>}
        <Col md={4}>
          <Form.Group controlId="event-currency">
            <Form.Label>Currency</Form.Label>
            <Form.Select value={values.currency} disabled>
              <option value="GHS">GHS</option>
            </Form.Select>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <Form.Check
            id="event-published"
            type="switch"
            label="Publish this event so Students can find and register for it"
            checked={values.isPublished ?? true}
            onChange={(eventValue) => setValues({ ...values, isPublished: eventValue.target.checked })}
          />
          {event && !event.isPublished && <Form.Text>This event is currently a private draft.</Form.Text>}
        </Col>
      </Row>}
      <div className="form-actions mt-4">
        <Button variant="light" onClick={reviewing ? () => setReviewing(false) : onCancel} disabled={busy || uploading}>
          {reviewing ? 'Back to edit' : 'Cancel'}
        </Button>
        <Button type="submit" disabled={busy || uploading || (reviewing && values.priceMinor > 0)}>
          {uploading ? 'Uploading image…' : busy ? 'Saving…' : reviewing ? submitLabel : 'Review event'}
        </Button>
      </div>
    </Form>
  )
}
