import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Table from 'react-bootstrap/Table'
import { QRCodeSVG } from 'qrcode.react'
import { api } from '../../api'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { formatDateTime } from '../../utils/formatters'
import type { Ticket } from '../../types'

export default function MyRegistrationsPage() {
  const { user } = useAuth()
  const [page, setPage] = useState(1)
  const [ticket, setTicket] = useState<Ticket | null>(null)
  const [ticketBusy, setTicketBusy] = useState(false)
  const [ticketError, setTicketError] = useState<string | null>(null)
  const [certificateBusyId, setCertificateBusyId] = useState<string | null>(null)
  const [certificateError, setCertificateError] = useState<string | null>(null)
  const [currentTime] = useState(() => Date.now())
  const loadRegistrations = useCallback(
    () => api.getStudentRegistrations(user!.id, page, 20),
    [page, user],
  )
  const { data: registrations, loading, error, reload } = useApiResource(loadRegistrations)

  const showTicket = async (registrationId: string) => {
    setTicketBusy(true)
    setTicketError(null)
    try {
      setTicket(await api.getTicket(registrationId))
    } catch (caught) {
      setTicketError(caught instanceof Error ? caught.message : 'The ticket could not be loaded.')
    } finally {
      setTicketBusy(false)
    }
  }

  const downloadCertificate = async (registrationId: string) => {
    setCertificateBusyId(registrationId)
    setCertificateError(null)
    try {
      const certificate = await api.getCertificate(registrationId)
      window.location.assign(certificate.downloadUrl)
    } catch (caught) {
      setCertificateError(
        caught instanceof Error ? caught.message : 'The certificate could not be generated.',
      )
    } finally {
      setCertificateBusyId(null)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Your calendar"
        title="My registrations"
        description="Everything you’ve signed up for, arranged by event date."
        action={<LinkButton to="/student/events">Find more events</LinkButton>}
      />
      {certificateError && (
        <Alert variant="danger" dismissible onClose={() => setCertificateError(null)}>
          {certificateError}
        </Alert>
      )}
      {loading ? (
        <LoadingState label="Loading registrations" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : registrations?.items.length ? (
        <>
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
              {registrations.items.map(({ event, registration }) => (
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
                    <div className="d-flex justify-content-end gap-2">
                      <Button
                        variant="primary"
                        size="sm"
                        disabled={ticketBusy}
                        onClick={() => void showTicket(registration.id)}
                      >
                        Ticket
                      </Button>
                      {registration.attended && new Date(event.date).getTime() <= currentTime && (
                        <Button
                          variant="outline-success"
                          size="sm"
                          disabled={certificateBusyId === registration.id}
                          onClick={() => void downloadCertificate(registration.id)}
                        >
                          {certificateBusyId === registration.id ? 'Preparing…' : 'Certificate'}
                        </Button>
                      )}
                      <LinkButton to={`/student/events/${event.id}`} variant="outline-primary" size="sm">
                        View
                      </LinkButton>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </div>
        <PaginationControls {...registrations} label="registrations" onPageChange={setPage} />
        </>
      ) : (
        <EmptyState
          title="Your calendar is wide open"
          message="Register for an upcoming event and it will appear here."
          action={<LinkButton to="/student/events">Browse events</LinkButton>}
        />
      )}
      <Modal show={Boolean(ticket) || Boolean(ticketError)} onHide={() => { setTicket(null); setTicketError(null) }} centered>
        <Modal.Header closeButton>
          <Modal.Title as="h2" className="h4">Event ticket</Modal.Title>
        </Modal.Header>
        <Modal.Body className="text-center p-4">
          {ticketError && <Alert variant="danger">{ticketError}</Alert>}
          {ticket && (
            <>
              <h3 className="h5">{ticket.eventTitle}</h3>
              <p className="text-secondary">{ticket.studentName}</p>
              <div className="bg-white d-inline-flex p-3 border rounded" aria-label="Signed QR event ticket">
                <QRCodeSVG value={ticket.token} size={256} level="M" />
              </div>
              <p className="small text-secondary mt-3 mb-0">
                Present this QR code at the entrance. Do not share it; it can be checked in only once.
              </p>
            </>
          )}
        </Modal.Body>
      </Modal>
    </>
  )
}
