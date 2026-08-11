import Alert from 'react-bootstrap/Alert'
import Card from 'react-bootstrap/Card'
import { useLocation } from 'react-router-dom'
import LinkButton from '../components/shared/LinkButton'

export default function BookingRequestThankYouPage() {
  const location = useLocation()
  const message = (location.state as { message?: string } | null)?.message
  return (
    <Card className="detail-card border-0 mx-auto" style={{ maxWidth: 760 }}>
      <Card.Body className="p-4 p-lg-5 text-center">
        <p className="eyebrow">Request received</p>
        <h1>Thank you for telling us about your event.</h1>
        <p className="lead text-secondary mt-3">
          {message ?? 'Your request is in the organizer queue.'}
        </p>
        <Alert variant="light" className="my-4">
          We aim to reply within 24 hours on working days. Keep an eye on the email address you supplied.
        </Alert>
        <div className="d-flex flex-wrap justify-content-center gap-2">
          <LinkButton to="/events">Explore events</LinkButton>
          <LinkButton to="/about" variant="outline-primary">How Campus Events works</LinkButton>
        </div>
      </Card.Body>
    </Card>
  )
}
