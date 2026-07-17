import { useCallback } from 'react'
import Badge from 'react-bootstrap/Badge'
import Table from 'react-bootstrap/Table'
import { api } from '../../api'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { formatDateTime } from '../../utils/formatters'

export default function MyRegistrationsPage() {
  const { user } = useAuth()
  const loadRegistrations = useCallback(
    () => api.getStudentRegistrations(user!.id),
    [user],
  )
  const { data: registrations, loading, error, reload } = useApiResource(loadRegistrations)

  return (
    <>
      <PageHeader
        eyebrow="Your calendar"
        title="My registrations"
        description="Everything you’ve signed up for, arranged by event date."
        action={<LinkButton to="/student/events">Find more events</LinkButton>}
      />
      {loading ? (
        <LoadingState label="Loading registrations" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : registrations?.length ? (
        <div className="table-shell">
          <Table responsive hover className="align-middle mb-0">
            <thead>
              <tr>
                <th>Event</th>
                <th>Date</th>
                <th>Location</th>
                <th>Status</th>
                <th className="text-end">Details</th>
              </tr>
            </thead>
            <tbody>
              {registrations.map(({ event, registration }) => (
                <tr key={registration.id}>
                  <td>
                    <div className="fw-semibold">{event.title}</div>
                    <small className="text-secondary">{event.category}</small>
                  </td>
                  <td>{formatDateTime(event.date)}</td>
                  <td>{event.location}</td>
                  <td>
                    <Badge bg={registration.attended ? 'success' : 'primary'}>
                      {registration.attended ? 'Attended' : 'Registered'}
                    </Badge>
                  </td>
                  <td className="text-end">
                    <LinkButton to={`/student/events/${event.id}`} variant="outline-primary" size="sm">
                      View
                    </LinkButton>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </div>
      ) : (
        <EmptyState
          title="Your calendar is wide open"
          message="Register for an upcoming event and it will appear here."
          action={<LinkButton to="/student/events">Browse events</LinkButton>}
        />
      )}
    </>
  )
}
