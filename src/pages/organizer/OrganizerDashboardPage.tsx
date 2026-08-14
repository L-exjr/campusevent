import { useCallback, useState } from 'react'
import Badge from 'react-bootstrap/Badge'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import Table from 'react-bootstrap/Table'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import { api } from '../../api'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import StatCard from '../../components/shared/StatCard'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { formatDateTime } from '../../utils/formatters'

export default function OrganizerDashboardPage() {
  const { user } = useAuth()
  const [couponCode, setCouponCode] = useState('')
  const [couponPercent, setCouponPercent] = useState(10)
  const loadEvents = useCallback(async () => ({
    events: await api.getOrganizerEvents(user!.id, true, 1, 4),
    analytics: await api.getOrganizerAnalytics().catch(() => ({ registrationCount: 0,
      ticketRevenueMinor: 0, currency: 'GHS' as const, attendedCount: 0,
      attendanceRate: 0, registrationsOverTime: [] })),
    coupons: await api.getCoupons().catch(() => []),
  }), [user])
  const { data, loading, error, reload } = useApiResource(loadEvents)
  const eventPage = data?.events

  if (loading) return <LoadingState label="Preparing organizer workspace" />
  if (error || !eventPage || !data) return <ErrorState message={error ?? 'No events returned.'} onRetry={() => void reload()} />

  const events = eventPage.items
  const registrations = data.analytics.registrationCount
  const capacity = events.reduce((total, event) => total + event.capacity, 0)
  const nextEvent = events[0]

  return (
    <>
      <section className="dashboard-hero dashboard-hero--organizer">
        <PageHeader
          eyebrow="Organizer workspace"
          title={`Make it memorable, ${user?.name.split(' ')[0]}.`}
          description="Plan your events, follow registrations, and keep attendance accurate."
          action={<LinkButton to="/organizer/events" size="lg">Manage events</LinkButton>}
        />
      </section>
      <Row className="g-3 mb-5">
        <Col md={4}>
          <StatCard label="Upcoming events" value={eventPage.totalCount} note="Your future schedule" />
        </Col>
        <Col md={4}>
          <StatCard label="Total registrations" value={registrations} note="Across your portfolio" tone="success" />
        </Col>
        <Col md={4}>
          <StatCard
            label="Capacity filled"
            value={capacity ? `${Math.round((registrations / capacity) * 100)}%` : '0%'}
            note={nextEvent ? `Next: ${nextEvent.title}` : 'Create your first event'}
            tone="ink"
          />
        </Col>
      </Row>
      <Row className="g-3 mb-5">
        <Col md={4}><StatCard label="Ticket revenue" value={`GHS ${(data.analytics.ticketRevenueMinor / 100).toFixed(2)}`} note="Verified paid orders" tone="success" /></Col>
        <Col md={4}><StatCard label="Attendance rate" value={`${data.analytics.attendanceRate.toFixed(1)}%`} note={`${data.analytics.attendedCount} checked in`} /></Col>
        <Col md={4}><StatCard label="Registration days" value={data.analytics.registrationsOverTime.length} note="Daily registration trend" /></Col>
      </Row>
      <div className="d-flex justify-content-between align-items-end mb-3">
        <div>
          <p className="eyebrow mb-1">At a glance</p>
          <h2 className="h3 mb-0">Your upcoming schedule</h2>
        </div>
        <LinkButton to="/organizer/events" variant="link" className="text-decoration-none">View all</LinkButton>
      </div>
      <Card className="detail-card border-0 mb-5"><Card.Body>
        <h2 className="h4">Discount coupons</h2>
        <Form className="d-flex flex-wrap gap-2 align-items-end" onSubmit={async (event) => {
          event.preventDefault()
          await api.createCoupon({ code: couponCode, percentageDiscount: couponPercent,
            usageLimit: null, eventId: null, expiresAt: null, isActive: true })
          setCouponCode(''); await reload()
        }}>
          <Form.Group><Form.Label>Code</Form.Label><Form.Control required minLength={3} value={couponCode}
            onChange={(event) => setCouponCode(event.target.value.toUpperCase())} /></Form.Group>
          <Form.Group><Form.Label>Discount %</Form.Label><Form.Control type="number" min={1} max={99}
            value={couponPercent} onChange={(event) => setCouponPercent(Number(event.target.value))} /></Form.Group>
          <Button type="submit">Create coupon</Button>
        </Form>
        {data.coupons.length > 0 && <div className="mt-3 d-flex flex-column gap-2">
          {data.coupons.map((coupon) => <div key={coupon.id} className="d-flex justify-content-between align-items-center border rounded p-2">
            <span>{coupon.code} ({coupon.percentageDiscount}% · {coupon.used} uses) · {coupon.isActive ? 'Active' : 'Inactive'}</span>
            <Button size="sm" variant="outline-secondary" onClick={async () => {
              await api.updateCoupon(coupon.id, { code: coupon.code,
                percentageDiscount: coupon.percentageDiscount, usageLimit: coupon.usageLimit,
                eventId: coupon.eventId, expiresAt: coupon.expiresAt, isActive: !coupon.isActive })
              await reload()
            }}>{coupon.isActive ? 'Deactivate' : 'Activate'}</Button>
          </div>)}
        </div>}
      </Card.Body></Card>
      {events.length ? (
        <div className="table-shell">
          <Table responsive hover className="align-middle mb-0">
            <thead>
              <tr><th>Event</th><th>Date</th><th>Registrations</th><th className="text-end">Open</th></tr>
            </thead>
            <tbody>
              {events.slice(0, 4).map((event) => (
                <tr key={event.id}>
                  <td><div className="fw-semibold">{event.title}</div><Badge bg="light" text="dark">{event.category}</Badge></td>
                  <td>{formatDateTime(event.date)}</td>
                  <td>{event.registeredCount} / {event.capacity}</td>
                  <td className="text-end">
                    <LinkButton to={`/organizer/events/${event.id}/registrants`} size="sm" variant="outline-primary">Registrants</LinkButton>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </div>
      ) : (
        <EmptyState title="No events to manage" message="Create your first event to start accepting registrations." action={<LinkButton to="/organizer/events">Create event</LinkButton>} />
      )}
    </>
  )
}
