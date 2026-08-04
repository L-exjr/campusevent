import { useCallback, useEffect, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import { api } from '../../api'
import type { BookingRequest, User } from '../../types'
import BookingRequestCard from '../../components/admin/BookingRequestCard'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'

const assignableStatuses = new Set<BookingRequest['status']>([
  'submitted',
  'underReview',
  'sentToOrganizer',
])

export default function AdminBookingQueue() {
  const load = useCallback(() => api.getBookingRequests(), [])
  const { data, loading, error, reload, setData } = useApiResource(load)
  const [organizers, setOrganizers] = useState<User[]>([])
  const [selected, setSelected] = useState<Record<string, string>>({})
  const [busyId, setBusyId] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  useEffect(() => {
    void api.getUsers()
      .then((users) => setOrganizers(users.filter(
        (user) => user.role === 'organizer' && user.active,
      )))
      .catch(() => setActionError('Organizers could not be loaded.'))
  }, [])

  const replaceRequest = (updated: BookingRequest) => {
    setData((current) => (current ?? []).map(
      (item) => item.id === updated.id ? updated : item,
    ))
  }

  const assign = async (request: BookingRequest) => {
    const organizerId = selected[request.id]
    if (!organizerId) return
    setBusyId(request.id)
    setNotice(null)
    setActionError(null)
    try {
      replaceRequest(await api.assignBookingRequest(request.id, organizerId))
      setNotice(request.assignedOrganizerId
        ? 'Request reassigned to the selected Organizer.'
        : 'Request sent to the Organizer.')
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'Unable to assign the request.')
    } finally {
      setBusyId(null)
    }
  }

  const close = async (request: BookingRequest) => {
    setBusyId(request.id)
    setNotice(null)
    setActionError(null)
    try {
      replaceRequest(await api.updateBookingRequestStatus(request.id, 'closed'))
      setNotice('Request closed.')
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'Unable to close the request.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Public requests"
        title="Booking request queue"
        description="Review incoming organization requests and route them to an active Organizer."
      />
      {notice && <Alert variant="success">{notice}</Alert>}
      {actionError && <Alert variant="danger">{actionError}</Alert>}
      {loading ? (
        <LoadingState label="Loading booking requests" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : data?.length ? (
        <div className="d-grid gap-3">
          {data.map((request) => {
            const canAssign = assignableStatuses.has(request.status)
            const busy = busyId === request.id
            return (
              <BookingRequestCard
                key={request.id}
                request={request}
                organizers={organizers}
                selectedOrganizerId={selected[request.id] ?? ''}
                busy={busy}
                canAssign={canAssign}
                onOrganizerChange={(organizerId) => setSelected((current) => ({
                  ...current,
                  [request.id]: organizerId,
                }))}
                onAssign={() => void assign(request)}
                onClose={() => void close(request)}
              />
            )
          })}
        </div>
      ) : (
        <EmptyState title="No booking requests" message="New public requests will appear here." />
      )}
    </>
  )
}
