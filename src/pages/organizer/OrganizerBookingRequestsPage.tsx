import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import { api } from '../../api'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import NotificationToast from '../../components/shared/NotificationToast'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import type { BookingRequest } from '../../types'

export default function OrganizerBookingRequestsPage() {
  const [page, setPage] = useState(1)
  const loadRequests = useCallback(() => api.getAssignedBookingRequests(page, 20), [page])
  const { data, loading, error, reload, setData } = useApiResource(loadRequests)
  const [notes, setNotes] = useState<Record<string, string>>({})
  const [busyId, setBusyId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const respond = async (request: BookingRequest, accept: boolean) => {
    setBusyId(request.id)
    setActionError(null)
    setNotice(null)
    try {
      const updated = await api.respondToBookingRequest(request.id, accept, notes[request.id])
      setData((current) => current ? {
        ...current,
        items: current.items.map((item) => item.id === updated.id ? updated : item),
      } : current)
      setNotice(accept
        ? 'Request accepted. An unpublished event draft is ready in Manage events.'
        : 'Request declined.')
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'Unable to respond to the request.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Organizer tools"
        title="Assigned booking requests"
        description="Review requests routed to you and accept or decline each assignment."
      />
      <NotificationToast message={notice} onClose={() => setNotice(null)} />
      {actionError && <Alert variant="danger">{actionError}</Alert>}
      {loading ? (
        <LoadingState label="Loading assigned booking requests" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : data?.items.length ? (
        <>
        <div className="d-grid gap-3">
          {data.items.map((request) => {
            const awaitingResponse = request.status === 'sentToOrganizer'
            const busy = busyId === request.id
            return (
              <Card key={request.id} className="border-0">
                <Card.Body className="p-4">
                  <div className="d-flex flex-wrap justify-content-between gap-3">
                    <div>
                      <h2 className="h5 mb-1">{request.organizationName}</h2>
                      <p className="text-secondary mb-3">
                        {request.eventType} · {new Date(request.proposedDate).toLocaleString()} · {request.estimatedAttendance} guests
                      </p>
                    </div>
                    <Badge bg={awaitingResponse ? 'primary' : 'secondary'} className="align-self-start">
                      {request.status}
                    </Badge>
                  </div>
                  <p>{request.description}</p>
                  <p className="small">
                    Contact: <a href={`mailto:${request.email}`}>{request.contactName}</a> · {request.phone}
                  </p>
                  {awaitingResponse ? (
                    <>
                      <Form.Group controlId={`booking-note-${request.id}`} className="mb-3">
                        <Form.Label>Response note (optional)</Form.Label>
                        <Form.Control
                          as="textarea"
                          rows={2}
                          maxLength={1000}
                          value={notes[request.id] ?? ''}
                          onChange={(event) => setNotes((current) => ({
                            ...current,
                            [request.id]: event.target.value,
                          }))}
                        />
                      </Form.Group>
                      <div className="d-flex gap-2">
                        <Button disabled={busy} onClick={() => void respond(request, true)}>
                          {busy ? 'Saving…' : 'Accept request'}
                        </Button>
                        <Button variant="outline-danger" disabled={busy} onClick={() => void respond(request, false)}>
                          Decline request
                        </Button>
                      </div>
                    </>
                  ) : (
                    <p className="mb-0 fw-semibold">
                      {request.organizerResponseNote || 'Response recorded.'}
                    </p>
                  )}
                </Card.Body>
              </Card>
            )
          })}
        </div>
        <PaginationControls {...data} label="requests" onPageChange={setPage} />
        </>
      ) : (
        <EmptyState
          title="No assigned requests"
          message="Requests will appear here after an administrator routes them to you."
        />
      )}
    </>
  )
}
