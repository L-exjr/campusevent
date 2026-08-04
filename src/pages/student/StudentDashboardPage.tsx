import { useCallback } from 'react'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import { api } from '../../api'
import EventCard from '../../components/events/EventCard'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import StatCard from '../../components/shared/StatCard'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { formatDate } from '../../utils/formatters'

export default function StudentDashboardPage() {
  const { user } = useAuth()
  const loadDashboard = useCallback(
    async () => ({
      events: await api.getEvents({}, 1, 3),
      registrations: await api.getStudentRegistrations(user!.id, 1, 1),
    }),
    [user],
  )
  const { data, loading, error, reload } = useApiResource(loadDashboard)

  if (loading) return <LoadingState label="Preparing your dashboard" />
  if (error || !data) return <ErrorState message={error ?? 'No data returned.'} onRetry={() => void reload()} />

  const nextEvent = data.registrations.items[0]?.event

  return (
    <>
      <section className="dashboard-hero">
        <PageHeader
          eyebrow="Student dashboard"
          title={`Good to see you, ${user?.name.split(' ')[0]}.`}
          description="Your next great campus moment could be one registration away."
          action={
            <LinkButton to="/student/events" size="lg">
              Explore events
            </LinkButton>
          }
        />
      </section>
      <Row className="g-3 mb-5">
        <Col md={4}>
          <StatCard label="My registrations" value={data.registrations.totalCount} note="Across upcoming events" />
        </Col>
        <Col md={4}>
          <StatCard label="Events to discover" value={data.events.totalCount} note="Open for registration" tone="success" />
        </Col>
        <Col md={4}>
          <StatCard
            label="Coming up next"
            value={nextEvent ? formatDate(nextEvent.date) : 'Nothing yet'}
            note={nextEvent?.title ?? 'Browse events to get started'}
            tone="ink"
          />
        </Col>
      </Row>
      <div className="d-flex justify-content-between align-items-end mb-3">
        <div>
          <p className="eyebrow mb-1">Curated for campus</p>
          <h2 className="h3 mb-0">Upcoming events</h2>
        </div>
        <LinkButton to="/student/events" variant="link" className="text-decoration-none">
          View all
        </LinkButton>
      </div>
      {data.events.items.length ? (
        <Row className="g-4">
          {data.events.items.map((event) => (
            <Col lg={4} md={6} key={event.id}>
              <EventCard event={event} />
            </Col>
          ))}
        </Row>
      ) : (
        <EmptyState title="No events yet" message="Check back soon for new campus events." />
      )}
    </>
  )
}
