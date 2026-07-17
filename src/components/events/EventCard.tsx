import Badge from 'react-bootstrap/Badge'
import Card from 'react-bootstrap/Card'
import ProgressBar from 'react-bootstrap/ProgressBar'
import type { EventItem } from '../../types'
import { formatDate, formatTime } from '../../utils/formatters'
import LinkButton from '../shared/LinkButton'

interface EventCardProps {
  event: EventItem
}

export default function EventCard({ event }: EventCardProps) {
  const fill = Math.min((event.registeredCount / event.capacity) * 100, 100)
  const spotsLeft = Math.max(event.capacity - event.registeredCount, 0)

  return (
    <Card className="event-card h-100 border-0">
      <Card.Body className="d-flex flex-column p-4">
        <div className="d-flex justify-content-between align-items-start gap-3 mb-4">
          <Badge bg="light" text="dark" className="category-badge">
            {event.category}
          </Badge>
          <div className="date-tile text-center flex-shrink-0">
            <span>{new Date(event.date).toLocaleDateString('en', { month: 'short' })}</span>
            <strong>{new Date(event.date).getDate()}</strong>
          </div>
        </div>
        <Card.Title as="h2" className="h4 mb-2">
          {event.title}
        </Card.Title>
        <Card.Text className="text-secondary event-card__description">
          {event.description}
        </Card.Text>
        <div className="event-meta mt-3">
          <div>
            <span aria-hidden="true">◷</span> {formatDate(event.date)} · {formatTime(event.date)}
          </div>
          <div>
            <span aria-hidden="true">⌖</span> {event.location}
          </div>
        </div>
        <div className="mt-auto pt-4">
          <div className="d-flex justify-content-between small text-secondary mb-2">
            <span>{spotsLeft ? `${spotsLeft} spots left` : 'Event full'}</span>
            <span>
              {event.registeredCount}/{event.capacity}
            </span>
          </div>
          <ProgressBar now={fill} className="capacity-progress mb-3" visuallyHidden />
          <LinkButton to={`/events/${event.id}`} variant="outline-primary" className="w-100">
            View event
          </LinkButton>
        </div>
      </Card.Body>
    </Card>
  )
}
