import { useCallback } from 'react'
import Badge from 'react-bootstrap/Badge'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import Table from 'react-bootstrap/Table'
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
  const loadEvents = useCallback(() => api.getOrganizerEvents(user!.id, true, 1, 4), [user])
  const { data: eventPage, loading, error, reload } = useApiResource(loadEvents)

  if (loading) return <LoadingState label="Preparing organizer workspace" />
  if (error || !eventPage) return <ErrorState message={error ?? 'No events returned.'} onRetry={() => void reload()} />

  const events = eventPage.items
  const registrations = events.reduce((total, event) => total + event.registeredCount, 0)
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
      <div className="d-flex justify-content-between align-items-end mb-3">
        <div>
          <p className="eyebrow mb-1">At a glance</p>
          <h2 className="h3 mb-0">Your upcoming schedule</h2>
        </div>
        <LinkButton to="/organizer/events" variant="link" className="text-decoration-none">View all</LinkButton>
      </div>
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
