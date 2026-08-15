import { useEffect, useState, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { useNavigate, useSearchParams } from 'react-router-dom'
import Spinner from 'react-bootstrap/Spinner'
import { api } from '../api'
import { EVENT_CATEGORIES, type BookingRequestInput, type OrganizerSummary } from '../types'
import { formatDateTime } from '../utils/formatters'
import LinkButton from '../components/shared/LinkButton'

const initial: BookingRequestInput = {
  organizationName: '',
  contactName: '',
  email: '',
  phone: '',
  eventType: '',
  eventCategory: '',
  budgetMinimumMinor: null,
  budgetMaximumMinor: null,
  proposedDate: '',
  expectedEndDate: null,
  alternativeDates: '',
  flexibilityNote: '',
  estimatedAttendance: 1,
  requiresTicketing: false,
  requiresVoting: false,
  requiresRegistration: true,
  referenceLinks: '',
  preferredOrganizer: '',
  requestedOrganizerId: null,
  description: '',
  website: '',
}

function splitDateTime(value: string) {
  return { date: value.slice(0, 10), time: value.slice(11, 16) }
}

function combineDateTime(date: string, time: string) {
  return date && time ? `${date}T${time}` : ''
}

function displayDateTime(value: string) {
  return value && Number.isFinite(new Date(value).getTime()) ? formatDateTime(value) : value
}

export default function BookingRequestForm() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [form, setForm] = useState(initial)
  const initialDateTime = splitDateTime(initial.proposedDate)
  const [proposedDate, setProposedDate] = useState(initialDateTime.date)
  const [proposedTime, setProposedTime] = useState(initialDateTime.time)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [validated, setValidated] = useState(false)
  const [reviewing, setReviewing] = useState(false)
  const [selectedOrganizer, setSelectedOrganizer] = useState<OrganizerSummary | null>(null)

  useEffect(() => {
    const id = searchParams.get('organizerId')
    if (!id) return
    void api.getOrganizer(id).then(organizer => {
      setSelectedOrganizer(organizer)
      setForm(current => ({ ...current, requestedOrganizerId: organizer.id }))
    }).catch(() => setError('That organizer is no longer available in the public directory.'))
  }, [searchParams])

  const update = (field: keyof BookingRequestInput, value: string | number | boolean | null) =>
    setForm((current) => ({ ...current, [field]: value }))

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const normalized: BookingRequestInput = {
      ...form,
      organizationName: form.organizationName.trim(),
      contactName: form.contactName.trim(),
      email: form.email.trim(),
      phone: form.phone.trim(),
      eventType: form.eventType.trim(),
      proposedDate: combineDateTime(proposedDate, proposedTime),
      alternativeDates: form.alternativeDates?.trim(),
      flexibilityNote: form.flexibilityNote?.trim(),
      preferredOrganizer: form.preferredOrganizer?.trim(),
      description: form.description.trim(),
    }
    const hasValidText =
      normalized.organizationName.length >= 2 &&
      normalized.contactName.length >= 2 &&
      normalized.eventType.length >= 2 &&
      normalized.description.length >= 10

    if (!event.currentTarget.checkValidity() || !hasValidText || !normalized.proposedDate) {
      setValidated(true)
      return
    }

    if (!reviewing) {
      setForm(normalized)
      setValidated(true)
      setReviewing(true)
      return
    }

    setBusy(true)
    setError(null)
    try {
      const submission = await api.submitBookingRequest(normalized)
      setForm(initial)
      navigate('/request-organizer/thank-you', { state: submission })
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Unable to submit your request.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <header className="page-header">
        <p className="eyebrow">Public booking request</p>
        <h1>Request an Organizer</h1>
        <p>Tell us what you are planning. No account is required, and we aim to reply within one working day.</p>
      </header>

      <Card className="booking-request-card border-0">
        <Card.Body className="p-4 p-lg-5">
          <ol className="form-progress" aria-label="Booking request progress">
            <li className={!reviewing ? 'is-active' : 'is-complete'} aria-current={!reviewing ? 'step' : undefined}>
              <span>1</span> Request details
            </li>
            <li className={reviewing ? 'is-active' : ''} aria-current={reviewing ? 'step' : undefined}>
              <span>2</span> Review and send
            </li>
          </ol>

          {error && <Alert variant="danger" role="alert">{error}</Alert>}

          <Form noValidate validated={validated} aria-busy={busy} onSubmit={(event) => void submit(event)}>
            {reviewing ? (
              <section aria-labelledby="booking-review-title">
                <p className="eyebrow mb-2">Almost done</p>
                <h2 id="booking-review-title" className="h3">Review your request</h2>
                <p className="text-secondary">Confirm the details we will send to the coordination team.</p>
                <dl className="row review-list mb-0">
                  <dt className="col-sm-4">Organization</dt><dd className="col-sm-8">{form.organizationName}</dd>
                  <dt className="col-sm-4">Contact</dt><dd className="col-sm-8">{form.contactName} · {form.email} · {form.phone}</dd>
                  <dt className="col-sm-4">Event</dt><dd className="col-sm-8">{form.eventType}</dd>
                  <dt className="col-sm-4">Preferred schedule</dt><dd className="col-sm-8">{displayDateTime(form.proposedDate)}</dd>
                  <dt className="col-sm-4">Attendance</dt><dd className="col-sm-8">About {form.estimatedAttendance.toLocaleString()} people</dd>
                  <dt className="col-sm-4">Category</dt><dd className="col-sm-8">{form.eventCategory || 'Not specified'}</dd>
                  <dt className="col-sm-4">Required tools</dt><dd className="col-sm-8">{[form.requiresRegistration && 'Registration', form.requiresTicketing && 'Ticketing', form.requiresVoting && 'Voting'].filter(Boolean).join(', ') || 'None specified'}</dd>
                  <dt className="col-sm-4">Description</dt><dd className="col-sm-8 review-list__long-text">{form.description}</dd>
                  {form.alternativeDates && <><dt className="col-sm-4">Alternative dates</dt><dd className="col-sm-8">{form.alternativeDates}</dd></>}
                  {form.flexibilityNote && <><dt className="col-sm-4">Flexibility</dt><dd className="col-sm-8">{form.flexibilityNote}</dd></>}
                  {form.preferredOrganizer && <><dt className="col-sm-4">Preferred Organizer</dt><dd className="col-sm-8">{form.preferredOrganizer}</dd></>}
                  <dt className="col-sm-4">Organizer path</dt><dd className="col-sm-8">{selectedOrganizer ? `Directory selection: ${selectedOrganizer.name}` : 'No preference — admin triage'}</dd>
                </dl>
              </section>
            ) : (
              <Row className="g-4">
                <Col xs={12}>
                  <div className="form-section-heading">
                    <span>01</span>
                    <div><h2>Who should we contact?</h2><p>We will use these details only to follow up on this request.</p></div>
                  </div>
                </Col>
                <Col md={6}>
                  <Form.Group controlId="booking-organization">
                    <Form.Label>Organization name</Form.Label>
                    <Form.Control required minLength={2} maxLength={200} value={form.organizationName} isInvalid={validated && form.organizationName.trim().length < 2} onChange={(event) => update('organizationName', event.target.value)} />
                    <Form.Control.Feedback type="invalid">Enter the organization name.</Form.Control.Feedback>
                  </Form.Group>
                </Col>
                <Col md={6}>
                  <Form.Group controlId="booking-contact-name">
                    <Form.Label>Contact name</Form.Label>
                    <Form.Control required minLength={2} maxLength={150} autoComplete="name" value={form.contactName} isInvalid={validated && form.contactName.trim().length < 2} onChange={(event) => update('contactName', event.target.value)} />
                    <Form.Control.Feedback type="invalid">Enter the name of the person we should contact.</Form.Control.Feedback>
                  </Form.Group>
                </Col>
                <Col md={6}>
                  <Form.Group controlId="booking-email">
                    <Form.Label>Email address</Form.Label>
                    <Form.Control required type="email" maxLength={320} autoComplete="email" value={form.email} onChange={(event) => update('email', event.target.value)} />
                    <Form.Control.Feedback type="invalid">Enter a valid email address.</Form.Control.Feedback>
                  </Form.Group>
                </Col>
                <Col md={6}>
                  <Form.Group controlId="booking-phone">
                    <Form.Label>Phone number</Form.Label>
                    <Form.Control required type="tel" maxLength={50} autoComplete="tel" value={form.phone} onChange={(event) => update('phone', event.target.value)} />
                    <Form.Control.Feedback type="invalid">Enter a phone number.</Form.Control.Feedback>
                  </Form.Group>
                </Col>

                <Col xs={12}>
                  <div className="form-section-heading mt-2">
                    <span>02</span>
                    <div><h2>When and what are you planning?</h2><p>A preferred schedule helps us find the right Organizer.</p></div>
                  </div>
                </Col>
                <Col md={5}>
                  <Form.Group controlId="booking-event-type">
                    <Form.Label>Event type or purpose</Form.Label>
                    <Form.Control required minLength={2} maxLength={150} placeholder="e.g. alumni dinner or career workshop" value={form.eventType} isInvalid={validated && form.eventType.trim().length < 2} onChange={(event) => update('eventType', event.target.value)} />
                    <Form.Control.Feedback type="invalid">Describe the event type or purpose.</Form.Control.Feedback>
                  </Form.Group>
                </Col>
                <Col md={7}>
                  <Form.Group controlId="booking-category">
                    <Form.Label>Event category</Form.Label>
                    <Form.Select value={form.eventCategory ?? ''} onChange={(event) => update('eventCategory', event.target.value)}>
                      <option value="">Choose a category (optional)</option>
                      {EVENT_CATEGORIES.map(category => <option key={category}>{category}</option>)}
                    </Form.Select>
                  </Form.Group>
                </Col>
                <Col sm={6} md={3}>
                  <Form.Group controlId="booking-proposed-date">
                    <Form.Label>Preferred date</Form.Label>
                    <Form.Control required type="date" value={proposedDate} onChange={(event) => { const date = event.target.value; setProposedDate(date); update('proposedDate', combineDateTime(date, proposedTime)) }} />
                    <Form.Control.Feedback type="invalid">Choose a preferred date.</Form.Control.Feedback>
                  </Form.Group>
                </Col>
                <Col sm={6} md={4}>
                  <Form.Group controlId="booking-proposed-time">
                    <Form.Label>Preferred start time</Form.Label>
                    <Form.Control required type="time" value={proposedTime} onChange={(event) => { const time = event.target.value; setProposedTime(time); update('proposedDate', combineDateTime(proposedDate, time)) }} />
                    <Form.Control.Feedback type="invalid">Choose a preferred start time.</Form.Control.Feedback>
                  </Form.Group>
                </Col>
                <Col md={4}>
                  <Form.Group controlId="booking-attendance">
                    <Form.Label>Estimated attendance</Form.Label>
                    <Form.Control required type="number" min={1} max={100000} value={form.estimatedAttendance} onChange={(event) => update('estimatedAttendance', Number(event.target.value))} />
                    <Form.Control.Feedback type="invalid">Enter at least one attendee.</Form.Control.Feedback>
                  </Form.Group>
                </Col>
                <Col md={4}>
                  <Form.Group controlId="booking-expected-end">
                    <Form.Label>Expected end date/time</Form.Label>
                    <Form.Control type="datetime-local" value={form.expectedEndDate ?? ''} onChange={(event) => update('expectedEndDate', event.target.value || null)} />
                  </Form.Group>
                </Col>
                <Col md={4}>
                  <Form.Group controlId="booking-budget-min">
                    <Form.Label>Minimum budget (GHS, optional)</Form.Label>
                    <Form.Control type="number" min={0} value={form.budgetMinimumMinor == null ? '' : form.budgetMinimumMinor / 100} onChange={(event) => update('budgetMinimumMinor', event.target.value === '' ? null : Math.round(Number(event.target.value) * 100))} />
                  </Form.Group>
                </Col>
                <Col md={4}>
                  <Form.Group controlId="booking-budget-max">
                    <Form.Label>Maximum budget (GHS, optional)</Form.Label>
                    <Form.Control type="number" min={0} value={form.budgetMaximumMinor == null ? '' : form.budgetMaximumMinor / 100} onChange={(event) => update('budgetMaximumMinor', event.target.value === '' ? null : Math.round(Number(event.target.value) * 100))} />
                  </Form.Group>
                </Col>
                <Col xs={12}>
                  <Form.Label>Required platform tools</Form.Label>
                  <div className="d-flex flex-wrap gap-4">
                    <Form.Check id="requires-registration" label="Registration" checked={form.requiresRegistration} onChange={(event) => update('requiresRegistration', event.target.checked)} />
                    <Form.Check id="requires-ticketing" label="Ticketing" checked={form.requiresTicketing} onChange={(event) => update('requiresTicketing', event.target.checked)} />
                    <Form.Check id="requires-voting" label="Voting" checked={form.requiresVoting} onChange={(event) => update('requiresVoting', event.target.checked)} />
                  </div>
                </Col>
                <Col xs={12}>
                  <Form.Group controlId="booking-reference-links">
                    <Form.Label>Attachments or reference links (optional)</Form.Label>
                    <Form.Control as="textarea" rows={2} maxLength={4000} value={form.referenceLinks ?? ''} placeholder="Paste one secure file or reference URL per line" onChange={(event) => update('referenceLinks', event.target.value)} />
                    <Form.Text>Use links to cloud-hosted attachments; do not include passwords or private credentials.</Form.Text>
                  </Form.Group>
                </Col>
                <Col md={8}>
                  <Form.Group controlId="booking-description">
                    <Form.Label>What support do you need?</Form.Label>
                    <Form.Control as="textarea" rows={5} required minLength={10} maxLength={5000} value={form.description} isInvalid={validated && form.description.trim().length < 10} placeholder="Describe the event, audience, venue needs, and what you want the Organizer to handle." onChange={(event) => update('description', event.target.value)} />
                    <Form.Control.Feedback type="invalid">Provide at least 10 characters so we can understand the request.</Form.Control.Feedback>
                    <Form.Text>{form.description.length}/5000 characters</Form.Text>
                  </Form.Group>
                </Col>

                <Col xs={12}>
                  <div className="form-section-heading mt-2"><span>03</span><div><h2>Do you have an Organizer in mind?</h2><p>A directory selection is a preference; administrators still handle final assignment.</p></div></div>
                  <Card className="border-0 bg-light"><Card.Body className="d-flex flex-column flex-md-row justify-content-between gap-3 align-items-md-center"><div><h3 className="h5 mb-1">{selectedOrganizer ? selectedOrganizer.name : 'No Organizer preference'}</h3><p className="text-secondary mb-0">{selectedOrganizer ? 'This organizer will be recorded as your requested preference.' : 'Your request will go to the existing admin triage queue.'}</p></div><div className="d-flex gap-2"><LinkButton to="/organizers" variant="outline-primary">Back to Organizer directory</LinkButton>{selectedOrganizer && <Button type="button" variant="light" onClick={() => { setSelectedOrganizer(null); update('requestedOrganizerId', null) }}>Clear selection</Button>}</div></Card.Body></Card>
                </Col>

                <Col xs={12}>
                  <details className="optional-fields">
                    <summary>Add optional scheduling preferences</summary>
                    <Row className="g-3 pt-3">
                      <Col md={6}><Form.Group controlId="booking-alternatives"><Form.Label>Alternative dates</Form.Label><Form.Control maxLength={500} value={form.alternativeDates} onChange={(event) => update('alternativeDates', event.target.value)} /></Form.Group></Col>
                      <Col md={6}><Form.Group controlId="booking-organizer"><Form.Label>Organizer preference notes</Form.Label><Form.Control maxLength={200} value={form.preferredOrganizer} placeholder="Fallback name or additional notes" onChange={(event) => update('preferredOrganizer', event.target.value)} /></Form.Group></Col>
                      <Col xs={12}><Form.Group controlId="booking-flexibility"><Form.Label>Scheduling flexibility</Form.Label><Form.Control as="textarea" rows={3} maxLength={1000} value={form.flexibilityNote} onChange={(event) => update('flexibilityNote', event.target.value)} /></Form.Group></Col>
                    </Row>
                  </details>
                </Col>

                <div className="booking-honeypot" aria-hidden="true"><label htmlFor="booking-website">Website</label><input id="booking-website" tabIndex={-1} autoComplete="off" value={form.website} onChange={(event) => update('website', event.target.value)} /></div>
              </Row>
            )}

            <div className="form-actions mt-4">
              {reviewing && <Button type="button" variant="light" disabled={busy} onClick={() => setReviewing(false)}>Back to edit</Button>}
              <Button type="submit" size="lg" disabled={busy}>
                {busy && <Spinner size="sm" className="me-2" aria-hidden="true" />}
                {busy ? 'Sending request…' : reviewing ? 'Send request' : 'Review request'}
              </Button>
            </div>
          </Form>
        </Card.Body>
      </Card>
    </>
  )
}
