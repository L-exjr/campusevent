import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { useNavigate } from 'react-router-dom'
import Spinner from 'react-bootstrap/Spinner'
import { api } from '../api'
import PageHeader from '../components/shared/PageHeader'
import type { BookingRequestInput } from '../types'
import '../components/events/create-event/EventCreationWizard.css'
import { formatDateTime } from '../utils/formatters'

const initial: BookingRequestInput = {
  organizationName: '',
  contactName: '',
  email: '',
  phone: '',
  eventType: '',
  proposedDate: '',
  alternativeDates: '',
  flexibilityNote: '',
  estimatedAttendance: 1,
  preferredOrganizer: '',
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
  const [form, setForm] = useState(initial); const [error,setError]=useState<string|null>(null); const [busy,setBusy]=useState(false)
  const update = (field: keyof BookingRequestInput, value: string | number) => setForm((current) => ({...current,[field]:value}))
  const [proposedDate = '', proposedTime = ''] = form.proposedDate.split('T')
  const updateProposedDate = (date: string) => update('proposedDate', `${date}T${proposedTime}`)
  const updateProposedTime = (time: string) => update('proposedDate', `${proposedDate}T${time}`)
  const submit = async (event: FormEvent) => { event.preventDefault(); setBusy(true); setError(null)
    try { const message = await api.submitBookingRequest(form); setForm(initial); navigate('/request-organizer/thank-you', { state: { message } }) } catch(caught) { setError(caught instanceof Error ? caught.message : 'Unable to submit your request.') } finally { setBusy(false) }
  }
  return <>
    <PageHeader eyebrow="Public booking request" title="Request an Organizer" description="Tell us about your event and we’ll connect you with the right support. No account is required." />
    <Card className="booking-request-card event-creation-wizard border-0"><Card.Body className="p-4 p-lg-5">
      <div className="booking-request-intro"><span className="eyebrow">Plan with confidence</span><p className="mb-0 text-secondary">Complete the details below. Required fields are marked with <span aria-hidden="true">*</span>.</p></div>
      {error && <Alert variant="danger" role="alert" aria-live="assertive"><strong>We couldn’t send your request.</strong><div>{error}</div></Alert>}
      <Form onSubmit={(event)=>void submit(event)}>
        <fieldset aria-labelledby="request-about-heading"><div className="form-section-heading mb-4"><span>01</span><div><h3 id="request-about-heading">About you</h3><p>Who should we contact about this event?</p></div></div><Row className="g-4">
          <Col md={6}><Form.Group controlId="organizationName"><Form.Label>Organization name <span aria-hidden="true">*</span></Form.Label><Form.Control required maxLength={200} value={form.organizationName} onChange={e=>update('organizationName',e.target.value)} /></Form.Group></Col>
          <Col md={6}><Form.Group controlId="contactName"><Form.Label>Contact name <span aria-hidden="true">*</span></Form.Label><Form.Control required maxLength={150} value={form.contactName} onChange={e=>update('contactName',e.target.value)} /></Form.Group></Col>
          <Col md={6}><Form.Group controlId="email"><Form.Label>Email <span aria-hidden="true">*</span></Form.Label><Form.Control required type="email" autoComplete="email" value={form.email} onChange={e=>update('email',e.target.value)} /></Form.Group></Col>
          <Col md={6}><Form.Group controlId="phone"><Form.Label>Phone <span aria-hidden="true">*</span></Form.Label><Form.Control required type="tel" autoComplete="tel" value={form.phone} onChange={e=>update('phone',e.target.value)} /></Form.Group></Col>
        </Row></fieldset>
        <fieldset aria-labelledby="request-details-heading"><div className="form-section-heading mb-4"><span>02</span><div><h3 id="request-details-heading">Event details</h3><p>Share the essentials so we can prepare the best response.</p></div></div><Row className="g-4">
          <Col md={6}><Form.Group controlId="eventType"><Form.Label>Event type or purpose <span aria-hidden="true">*</span></Form.Label><Form.Control required value={form.eventType} onChange={e=>update('eventType',e.target.value)} /></Form.Group></Col>
          <Col sm={6} md={3}><Form.Group controlId="proposedDate"><Form.Label>Proposed date <span aria-hidden="true">*</span></Form.Label><Form.Control required type="date" value={proposedDate} onChange={e=>updateProposedDate(e.target.value)} /></Form.Group></Col>
          <Col sm={6} md={3}><Form.Group controlId="proposedTime"><Form.Label>Start time <span aria-hidden="true">*</span></Form.Label><Form.Control required type="time" value={proposedTime} onChange={e=>updateProposedTime(e.target.value)} /></Form.Group></Col>
          <Col md={6}><Form.Group controlId="alternativeDates"><Form.Label>Alternative dates <span className="text-secondary fw-normal">(optional)</span></Form.Label><Form.Control value={form.alternativeDates} onChange={e=>update('alternativeDates',e.target.value)} /></Form.Group></Col>
          <Col md={6}><Form.Group controlId="estimatedAttendance"><Form.Label>Estimated attendance <span aria-hidden="true">*</span></Form.Label><Form.Control required type="number" min={1} max={100000} value={form.estimatedAttendance} onChange={e=>update('estimatedAttendance',Number(e.target.value))} /></Form.Group></Col>
          <Col md={6}><Form.Group controlId="preferredOrganizer"><Form.Label>Preferred Organizer <span className="text-secondary fw-normal">(optional)</span></Form.Label><Form.Control value={form.preferredOrganizer} onChange={e=>update('preferredOrganizer',e.target.value)} /></Form.Group></Col>
          <Col md={6}><Form.Group controlId="flexibilityNote"><Form.Label>Scheduling flexibility <span className="text-secondary fw-normal">(optional)</span></Form.Label><Form.Control value={form.flexibilityNote} onChange={e=>update('flexibilityNote',e.target.value)} /></Form.Group></Col>
          <Col xs={12}><Form.Group controlId="description"><Form.Label>Event description <span aria-hidden="true">*</span></Form.Label><Form.Control as="textarea" rows={5} required minLength={10} value={form.description} onChange={e=>update('description',e.target.value)} /><Form.Text>Include the venue, goals, and anything else your Organizer should know.</Form.Text></Form.Group></Col>
        </Row></fieldset>
        <div className="position-absolute opacity-0 pe-none" aria-hidden="true"><label htmlFor="booking-website">Website</label><input id="booking-website" tabIndex={-1} autoComplete="off" value={form.website} onChange={e=>update('website',e.target.value)} /></div>
        <div className="booking-request-actions form-actions"><p className="text-secondary mb-0 me-auto"><strong>Response promise:</strong> we aim to reply within 24 hours on working days.</p><Button type="submit" size="lg" disabled={busy}>{busy?'Submitting…':'Submit request'}</Button></div>
      </Form>
    </Card.Body></Card>
  </>
  const [form, setForm] = useState(initial)
  const initialDateTime = splitDateTime(initial.proposedDate)
  const [proposedDate, setProposedDate] = useState(initialDateTime.date)
  const [proposedTime, setProposedTime] = useState(initialDateTime.time)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [validated, setValidated] = useState(false)
  const [reviewing, setReviewing] = useState(false)

  const update = (field: keyof BookingRequestInput, value: string | number) =>
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
      const message = await api.submitBookingRequest(normalized)
      setForm(initial)
      navigate('/request-organizer/thank-you', { state: { message } })
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
                  <dt className="col-sm-4">Description</dt><dd className="col-sm-8 review-list__long-text">{form.description}</dd>
                  {form.alternativeDates && <><dt className="col-sm-4">Alternative dates</dt><dd className="col-sm-8">{form.alternativeDates}</dd></>}
                  {form.flexibilityNote && <><dt className="col-sm-4">Flexibility</dt><dd className="col-sm-8">{form.flexibilityNote}</dd></>}
                  {form.preferredOrganizer && <><dt className="col-sm-4">Preferred Organizer</dt><dd className="col-sm-8">{form.preferredOrganizer}</dd></>}
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
                <Col md={8}>
                  <Form.Group controlId="booking-description">
                    <Form.Label>What support do you need?</Form.Label>
                    <Form.Control as="textarea" rows={5} required minLength={10} maxLength={5000} value={form.description} isInvalid={validated && form.description.trim().length < 10} placeholder="Describe the event, audience, venue needs, and what you want the Organizer to handle." onChange={(event) => update('description', event.target.value)} />
                    <Form.Control.Feedback type="invalid">Provide at least 10 characters so we can understand the request.</Form.Control.Feedback>
                    <Form.Text>{form.description.length}/5000 characters</Form.Text>
                  </Form.Group>
                </Col>

                <Col xs={12}>
                  <details className="optional-fields">
                    <summary>Add optional scheduling preferences</summary>
                    <Row className="g-3 pt-3">
                      <Col md={6}><Form.Group controlId="booking-alternatives"><Form.Label>Alternative dates</Form.Label><Form.Control maxLength={500} value={form.alternativeDates} onChange={(event) => update('alternativeDates', event.target.value)} /></Form.Group></Col>
                      <Col md={6}><Form.Group controlId="booking-organizer"><Form.Label>Preferred Organizer</Form.Label><Form.Control maxLength={200} value={form.preferredOrganizer} onChange={(event) => update('preferredOrganizer', event.target.value)} /></Form.Group></Col>
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
