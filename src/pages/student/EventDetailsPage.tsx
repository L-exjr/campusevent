import { useCallback, useEffect, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import ProgressBar from 'react-bootstrap/ProgressBar'
import Row from 'react-bootstrap/Row'
import { useLocation, useParams } from 'react-router-dom'
import { api } from '../../api'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { formatDate, formatDateTime, formatTime } from '../../utils/formatters'
import { DEFAULT_EVENT_IMAGE } from '../../api/imageStorage'

export default function EventDetailsPage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const location = useLocation()
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [currentTime, setCurrentTime] = useState(() => Date.now())
  const loadDetails = useCallback(
    async () => ({
      event: await api.getEvent(id),
      isRegistered: user?.role === 'student'
        ? await api.isRegisteredForEvent(id)
        : false,
    }),
    [id, user],
  )
  const { data, loading, error, reload } = useApiResource(loadDetails)

  useEffect(() => {
    if (!data?.event.title) return
    document.title = `${data.event.title} | Campus Events`
    return () => {
      document.title = 'Campus Events'
    }
  }, [data?.event.title])

  useEffect(() => {
    if (!data?.event.date) return
    const nextBoundary = [data.event.salesStartsAt, data.event.salesEndsAt, data.event.date]
      .filter((value): value is string => Boolean(value))
      .map((value) => new Date(value).getTime())
      .filter((value) => value > currentTime)
      .sort((left, right) => left - right)[0]
    if (!nextBoundary) return
    const remaining = nextBoundary - currentTime
    const timeout = window.setTimeout(
      () => setCurrentTime(Date.now()),
      Math.min(remaining + 1, 2_147_483_647),
    )
    return () => window.clearTimeout(timeout)
  }, [currentTime, data?.event.date, data?.event.salesEndsAt, data?.event.salesStartsAt])

  if (loading) return <LoadingState label="Loading event details" />
  if (error || !data) return <ErrorState message={error ?? 'No event returned.'} onRetry={() => void reload()} />

  const isRegistered = data.isRegistered
  const isFull = data.event.registeredCount >= data.event.capacity
  const eventStarted = new Date(data.event.date).getTime() <= currentTime
  const salesNotOpen = data.event.priceMinor > 0 && Boolean(
    data.event.salesStartsAt && new Date(data.event.salesStartsAt).getTime() > currentTime,
  )
  const salesEnded = data.event.priceMinor > 0 && Boolean(
    data.event.salesEndsAt && new Date(data.event.salesEndsAt).getTime() <= currentTime,
  )
  const registrationClosed = eventStarted || salesEnded
  const spotsLeft = Math.max(data.event.capacity - data.event.registeredCount, 0)
  const fill = Math.min((data.event.registeredCount / data.event.capacity) * 100, 100)

  const handleRegister = async () => {
    setBusy(true)
    setActionError(null)
    try {
      if (data.event.priceMinor > 0) {
        const payment = await api.initializeEventPayment(data.event.id)
        window.location.assign(payment.authorizationUrl)
      } else {
        await api.registerForEvent(data.event.id, user!.id)
        setSuccess(true)
        await reload()
      }
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'Registration failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <LinkButton to="/events" variant="link" className="px-0 text-decoration-none mb-3">
        ← Back to events
      </LinkButton>
      {success && <Alert variant="success">You’re registered. We’ll see you there!</Alert>}
      {actionError && <Alert variant="danger">{actionError}</Alert>}
      <Row className="g-4">
        <Col lg={8}>
          <Card className="detail-card border-0 h-100">
            <Card.Img
              variant="top"
              src={data.event.imageUrl ?? DEFAULT_EVENT_IMAGE}
              alt={`${data.event.title} cover`}
              className="object-fit-cover"
              style={{ maxHeight: 420 }}
            />
            <Card.Body className="p-4 p-lg-5">
              <Badge bg="light" text="dark" className="category-badge mb-4">
                {data.event.category}
              </Badge>
              <h1 className="event-detail-title">{data.event.title}</h1>
              <p className="lead text-secondary mt-3 mb-5">{data.event.description}</p>
              <Row className="g-4 detail-facts">
                <Col sm={6}>
                  <span className="detail-facts__label">Date</span>
                  <strong>{formatDate(data.event.date)}</strong>
                  <small>{formatTime(data.event.date)}</small>
                </Col>
                <Col sm={6}>
                  <span className="detail-facts__label">{data.event.format === 'virtual' ? 'Online' : 'Location'}</span>
                  <strong>{data.event.location}</strong>
                  <small>{data.event.format === 'virtual' ? 'Virtual event' : 'Campus venue'}</small>
                  {data.event.format === 'physical' && <a
                    href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(data.event.location)}`}
                    target="_blank"
                    rel="noreferrer"
                    className="small d-block mt-2"
                  >
                    Open maps and directions ↗
                  </a>}
                  {data.event.format === 'virtual' && data.event.meetingUrl && isRegistered && <a
                    href={data.event.meetingUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="small d-block mt-2"
                  >
                    Join virtual meeting ↗
                  </a>}
                </Col>
                <Col sm={6}>
                  <span className="detail-facts__label">Organizer</span>
                  <strong>{data.event.organizerName}</strong>
                  <small>Event lead</small>
                </Col>
                <Col sm={6}>
                  <span className="detail-facts__label">Category</span>
                  <strong>{data.event.category}</strong>
                  <small>Open to students</small>
                </Col>
              </Row>
            </Card.Body>
          </Card>
        </Col>
        <Col lg={4}>
          <Card className="registration-card border-0">
            <Card.Body className="p-4">
              <p className="eyebrow mb-2">Registration</p>
              <h2 className="h4 mb-3">
                {salesNotOpen ? 'Ticket sales not open' : registrationClosed ? 'Registration closed' : 'Reserve your place'}
              </h2>
              <div className="d-flex justify-content-between mb-2">
                <span className="text-secondary">Spots filled</span>
                <strong>{data.event.registeredCount} / {data.event.capacity}</strong>
              </div>
              <ProgressBar now={fill} className="capacity-progress mb-3" />
              <p className="small text-secondary">
                {salesNotOpen
                  ? `Ticket sales open ${formatDateTime(data.event.salesStartsAt!)}.`
                  : registrationClosed
                  ? salesEnded && !eventStarted ? 'The ticket sales window has ended.' : 'This event has already started or ended.'
                  : isFull
                    ? 'This event has reached capacity.'
                    : `${spotsLeft} ${spotsLeft === 1 ? 'place remains' : 'places remain'}.`}
              </p>
              {salesNotOpen ? (
                <Button size="lg" className="w-100" disabled>
                  Sales not open
                </Button>
              ) : registrationClosed ? (
                <Button size="lg" className="w-100" disabled>
                  Registration closed
                </Button>
              ) : isRegistered ? (
                <Button size="lg" className="w-100" disabled>
                  Already registered
                </Button>
              ) : isFull ? (
                <Button size="lg" className="w-100" disabled>
                  Event full
                </Button>
              ) : user?.role === 'student' ? (
                <Button
                  size="lg"
                  className="w-100"
                  disabled={busy}
                  onClick={() => void handleRegister()}
                >
                  {busy
                    ? data.event.priceMinor > 0 ? 'Opening checkout…' : 'Registering…'
                    : data.event.priceMinor > 0
                      ? `Pay GHS ${(data.event.priceMinor / 100).toFixed(2)}`
                      : 'Register now'}
                </Button>
              ) : user ? (
                <Alert variant="light" className="mb-0">
                  Event registration is available to Student accounts.
                </Alert>
              ) : (
                <div className="d-grid gap-2">
                  <LinkButton to="/login" state={{ from: location.pathname }} size="lg">
                    Sign in to register
                  </LinkButton>
                  <LinkButton
                    to="/register"
                    state={{ from: location.pathname }}
                    variant="outline-primary"
                  >
                    Create a Student account
                  </LinkButton>
                </div>
              )}
              <p className="registration-note text-center mb-0 mt-3">
                {data.event.priceMinor > 0
                  ? 'Your place is confirmed only after Paystack verifies payment.'
                  : 'This is a free event. No payment is required.'}
              </p>
              <LinkButton
                to={`/events/${data.event.id}/voting`}
                variant="outline-secondary"
                className="w-100 mt-3"
              >
                View event voting
              </LinkButton>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </>
  )
}
