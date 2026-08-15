import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import ButtonGroup from 'react-bootstrap/ButtonGroup'
import ProgressBar from 'react-bootstrap/ProgressBar'
import Table from 'react-bootstrap/Table'
import type { EventAccess, EventItem } from '../../types'
import { formatDateTime } from '../../utils/formatters'
import LinkButton from '../shared/LinkButton'

interface OrganizerEventTableProps {
  events: EventItem[]
  onEdit: (event: EventItem) => void
  onDelete: (event: EventItem) => void
  access: Record<string, EventAccess>
}

export default function OrganizerEventTable({
  events,
  onEdit,
  onDelete,
  access,
}: OrganizerEventTableProps) {
  return (
    <div className="table-shell">
      <Table responsive hover className="align-middle mb-0">
        <thead>
          <tr>
            <th>Event</th>
            <th>Date</th>
            <th>Registration</th>
            <th className="text-end">Actions</th>
          </tr>
        </thead>
        <tbody>
          {events.map((event) => (
            <tr key={event.id}>
              <td>
                <div className="fw-semibold">{event.title}</div>
                <Badge bg="light" text="dark" className="mt-1">
                  {event.category}
                </Badge>
              </td>
              <td>
                <div>{formatDateTime(event.date)}</div>
                <small className="text-secondary">{event.location}</small>
              </td>
              <td className="registration-cell">
                <div className="d-flex justify-content-between small mb-1">
                  <span>{event.registeredCount} registered</span>
                  <span>{event.capacity}</span>
                </div>
                <ProgressBar now={(event.registeredCount / event.capacity) * 100} />
              </td>
              <td className="text-end">
                <ButtonGroup size="sm" className="flex-wrap table-actions">
                  {access[event.id]?.canViewAttendees && <LinkButton to={`/organizer/events/${event.id}/registrants`} variant="outline-secondary">
                    Registrants
                  </LinkButton>}
                  {access[event.id]?.canCheckIn && <LinkButton to={`/organizer/events/${event.id}/attendance`} variant="outline-secondary">
                    Attendance
                  </LinkButton>}
                  {access[event.id]?.canManageOperations && <LinkButton to={`/organizer/events/${event.id}/voting`} variant="outline-secondary">
                    Voting
                  </LinkButton>}
                  {access[event.id]?.canManageTeam && <LinkButton to={`/organizer/events/${event.id}/team`} variant="outline-secondary">Team</LinkButton>}
                  {access[event.id]?.canEdit && <Button variant="outline-primary" onClick={() => onEdit(event)}>
                    Edit
                  </Button>}
                  {access[event.id]?.canDelete && <Button variant="outline-danger" onClick={() => onDelete(event)}>
                    Delete
                  </Button>}
                </ButtonGroup>
              </td>
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  )
}
