import { useState, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { api } from '../api'
import type { BookingRequestInput } from '../types'

const initial: BookingRequestInput = { organizationName:'', contactName:'', email:'', phone:'', eventType:'', proposedDate:'', alternativeDates:'', flexibilityNote:'', estimatedAttendance:1, preferredOrganizer:'', description:'', website:'' }

export default function BookingRequestForm() {
  const [form, setForm] = useState(initial); const [message,setMessage]=useState<string|null>(null); const [error,setError]=useState<string|null>(null); const [busy,setBusy]=useState(false)
  const update = (field: keyof BookingRequestInput, value: string | number) => setForm((current) => ({...current,[field]:value}))
  const submit = async (event: FormEvent) => { event.preventDefault(); setBusy(true); setError(null)
    try { setMessage(await api.submitBookingRequest(form)); setForm(initial) } catch(caught) { setError(caught instanceof Error ? caught.message : 'Unable to submit your request.') } finally { setBusy(false) }
  }
  return <><header className="page-header"><p className="eyebrow">Public booking request</p><h1>Request an Organizer</h1><p>Tell us what your organization is planning. No account is required.</p></header>
    <Card className="border-0"><Card.Body className="p-4 p-lg-5">{message&&<Alert variant="success">{message}</Alert>}{error&&<Alert variant="danger">{error}</Alert>}
      <Form onSubmit={(event)=>void submit(event)}><Row className="g-3">
        <Col md={6}><Form.Group><Form.Label>Organization name</Form.Label><Form.Control required maxLength={200} value={form.organizationName} onChange={e=>update('organizationName',e.target.value)}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Contact name</Form.Label><Form.Control required maxLength={150} value={form.contactName} onChange={e=>update('contactName',e.target.value)}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Email</Form.Label><Form.Control required type="email" value={form.email} onChange={e=>update('email',e.target.value)}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Phone</Form.Label><Form.Control required type="tel" value={form.phone} onChange={e=>update('phone',e.target.value)}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Event type or purpose</Form.Label><Form.Control required value={form.eventType} onChange={e=>update('eventType',e.target.value)}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Proposed date</Form.Label><Form.Control required type="datetime-local" value={form.proposedDate} onChange={e=>update('proposedDate',e.target.value)}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Alternative dates</Form.Label><Form.Control value={form.alternativeDates} onChange={e=>update('alternativeDates',e.target.value)}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Estimated attendance</Form.Label><Form.Control required type="number" min={1} max={100000} value={form.estimatedAttendance} onChange={e=>update('estimatedAttendance',Number(e.target.value))}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Preferred Organizer <span className="text-secondary">(optional)</span></Form.Label><Form.Control value={form.preferredOrganizer} onChange={e=>update('preferredOrganizer',e.target.value)}/></Form.Group></Col>
        <Col md={6}><Form.Group><Form.Label>Scheduling flexibility</Form.Label><Form.Control value={form.flexibilityNote} onChange={e=>update('flexibilityNote',e.target.value)}/></Form.Group></Col>
        <Col xs={12}><Form.Group><Form.Label>Description</Form.Label><Form.Control as="textarea" rows={5} required minLength={10} value={form.description} onChange={e=>update('description',e.target.value)}/></Form.Group></Col>
        <div className="position-absolute opacity-0 pe-none" aria-hidden="true"><label htmlFor="booking-website">Website</label><input id="booking-website" tabIndex={-1} autoComplete="off" value={form.website} onChange={e=>update('website',e.target.value)}/></div>
        <Col xs={12}><Button type="submit" size="lg" disabled={busy}>{busy?'Submitting…':'Submit request'}</Button></Col>
      </Row></Form></Card.Body></Card></>
}
