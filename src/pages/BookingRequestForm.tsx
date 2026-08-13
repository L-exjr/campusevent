import { useState, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import PageHeader from '../components/shared/PageHeader'
import type { BookingRequestInput } from '../types'
import '../components/events/create-event/EventCreationWizard.css'

const initial: BookingRequestInput = { organizationName:'', contactName:'', email:'', phone:'', eventType:'', proposedDate:'', alternativeDates:'', flexibilityNote:'', estimatedAttendance:1, preferredOrganizer:'', description:'', website:'' }

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
}
