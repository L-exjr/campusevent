import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import type { BookingRequest, User } from '../../types'

interface BookingRequestCardProps {
  request: BookingRequest
  organizers: User[]
  selectedOrganizerId: string
  busy: boolean
  canAssign: boolean
  onOrganizerChange: (organizerId: string) => void
  onAssign: () => void
  onClose: () => void
}

export default function BookingRequestCard({
  request,
  organizers,
  selectedOrganizerId,
  busy,
  canAssign,
  onOrganizerChange,
  onAssign,
  onClose,
}: BookingRequestCardProps) {
  return (
    <Card className="border-0">
      <Card.Body className="p-4">
        <div className="d-flex justify-content-between gap-3">
          <div>
            <h2 className="h5">{request.organizationName}</h2>
            <p className="text-secondary mb-2">
              {request.eventType} · {new Date(request.proposedDate).toLocaleString()} · {request.estimatedAttendance} guests
            </p>
          </div>
          <Badge bg="secondary" className="align-self-start">{request.status}</Badge>
        </div>
        <p>{request.description}</p>
        <p className="small mb-3">
          Contact: <a href={`mailto:${request.email}`}>{request.contactName}</a> · {request.phone}
        </p>
        {request.assignedOrganizerName && (
          <p className="fw-semibold">Assigned to {request.assignedOrganizerName}</p>
        )}
        {canAssign && (
          <div className="d-flex flex-column flex-md-row gap-2">
            <Form.Select
              aria-label={`Organizer for ${request.organizationName}`}
              value={selectedOrganizerId}
              disabled={busy}
              onChange={(event) => onOrganizerChange(event.target.value)}
            >
              <option value="">Choose an Organizer</option>
              {organizers.map((user) => (
                <option key={user.id} value={user.id}>{user.name}</option>
              ))}
            </Form.Select>
            <Button disabled={busy || !selectedOrganizerId} onClick={onAssign}>
              {request.assignedOrganizerId ? 'Reassign' : 'Send'}
            </Button>
            <Button variant="outline-danger" disabled={busy} onClick={onClose}>
              Close request
            </Button>
          </div>
        )}
      </Card.Body>
    </Card>
  )
}
