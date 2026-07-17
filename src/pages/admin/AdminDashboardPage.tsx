import { useCallback } from 'react'
import Badge from 'react-bootstrap/Badge'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import Table from 'react-bootstrap/Table'
import { api } from '../../api'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import StatCard from '../../components/shared/StatCard'
import { useApiResource } from '../../hooks/useApiResource'
import { formatPercent } from '../../utils/formatters'

export default function AdminDashboardPage() {
  const loadReports = useCallback(() => api.getReports(), [])
  const { data: report, loading, error, reload } = useApiResource(loadReports)

  if (loading) return <LoadingState label="Compiling system reports" />
  if (error || !report) return <ErrorState message={error ?? 'No report returned.'} onRetry={() => void reload()} />

  return (
    <>
      <section className="dashboard-hero dashboard-hero--admin">
        <PageHeader
          eyebrow="System overview"
          title="Reports dashboard"
          description="A clear view of event activity, registration volume, and recorded attendance."
        />
      </section>
      <Row className="g-3 mb-5">
        <Col sm={6} xl={3}>
          <StatCard label="Total events" value={report.totalEvents} note="System-wide" />
        </Col>
        <Col sm={6} xl={3}>
          <StatCard label="Registrations" value={report.totalRegistrations} note="Across all events" tone="success" />
        </Col>
        <Col sm={6} xl={3}>
          <StatCard label="Attendance rate" value={formatPercent(report.attendanceRate)} note="Of recorded registrations" tone="warning" />
        </Col>
        <Col sm={6} xl={3}>
          <StatCard label="User accounts" value={report.totalUsers} note="Students, organizers & admins" tone="ink" />
        </Col>
      </Row>
      <Row className="g-4">
        <Col xl={8}>
          <div className="section-heading mb-3">
            <p className="eyebrow mb-1">Event performance</p>
            <h2 className="h3 mb-0">Attendance by event</h2>
          </div>
          <div className="table-shell">
            <Table responsive hover className="align-middle mb-0">
              <thead>
                <tr>
                  <th>Event</th>
                  <th>Organizer</th>
                  <th>Registered</th>
                  <th>Attended</th>
                  <th>Rate</th>
                </tr>
              </thead>
              <tbody>
                {report.events.map((event) => (
                  <tr key={event.eventId}>
                    <td className="fw-semibold">{event.title}</td>
                    <td>{event.organizerName}</td>
                    <td>{event.registrations}</td>
                    <td>{event.attended}</td>
                    <td>
                      <Badge bg={event.attendanceRate >= 60 ? 'success' : 'light'} text={event.attendanceRate >= 60 ? undefined : 'dark'}>
                        {formatPercent(event.attendanceRate)}
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </div>
        </Col>
        <Col xl={4}>
          <div className="section-heading mb-3">
            <p className="eyebrow mb-1">Community leaders</p>
            <h2 className="h3 mb-0">Most active organizers</h2>
          </div>
          <div className="table-shell">
            <Table hover className="align-middle mb-0">
              <thead>
                <tr><th>Organizer</th><th>Events</th><th>Sign-ups</th></tr>
              </thead>
              <tbody>
                {report.organizers.map((organizer, index) => (
                  <tr key={organizer.organizerId}>
                    <td><span className="rank-number">{index + 1}</span> {organizer.organizerName}</td>
                    <td>{organizer.events}</td>
                    <td>{organizer.registrations}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </div>
        </Col>
      </Row>
    </>
  )
}
