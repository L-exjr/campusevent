import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import ButtonGroup from 'react-bootstrap/ButtonGroup'
import Table from 'react-bootstrap/Table'
import type { EventItem } from '../../types'
import { formatDateTime } from '../../utils/formatters'
import LinkButton from '../shared/LinkButton'

interface AdminEventTableProps {
  events: EventItem[]
  onEdit: (event: EventItem) => void
  onTransfer: (event: EventItem) => void
  onDelete: (event: EventItem) => void
}

export default function AdminEventTable({ events, onEdit, onTransfer, onDelete }: AdminEventTableProps) {
  return (
    <div className="table-shell">
      <Table responsive hover className="align-middle mb-0">
        <thead>
          <tr>
            <th>Event</th>
            <th>Organizer</th>
            <th>Date & location</th>
            <th>Registrations</th>
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
              <td>{event.organizerName}</td>
              <td>
                <div>{formatDateTime(event.date)}</div>
                <small className="text-secondary">{event.location}</small>
              </td>
              <td>
                {event.registeredCount} / {event.capacity}
              </td>
              <td className="text-end">
                <ButtonGroup size="sm" className="flex-wrap table-actions">
                  <LinkButton
                    to={`/admin/events/${event.id}/registrants`}
                    variant="outline-secondary"
                  >
                    Registrants
                  </LinkButton>
                  <LinkButton
                    to={`/admin/events/${event.id}/voting`}
                    variant="outline-secondary"
                  >
                    Voting
                  </LinkButton>
                  <Button variant="outline-primary" onClick={() => onEdit(event)}>
                    Edit
                  </Button>
                  <Button variant="outline-primary" onClick={() => onTransfer(event)}>
                    Transfer
                  </Button>
                  <Button variant="outline-danger" onClick={() => onDelete(event)}>
                    Delete
                  </Button>
                </ButtonGroup>
              </td>
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  )
}
