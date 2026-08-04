import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import { api } from '../../api'
import type { BookingRequest, User } from '../../types'
import BookingRequestCard from '../../components/admin/BookingRequestCard'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

const assignableStatuses = new Set<BookingRequest['status']>([
  'submitted',
  'underReview',
  'sentToOrganizer',
])

export default function AdminBookingQueue() {
  const [page, setPage] = useState(1)
  const load = useCallback(() => api.getBookingRequests(page, 20), [page])
  const { data, loading, error, reload, setData } = useApiResource(load)
  const [organizerSearch, setOrganizerSearch] = useState('')
  const debouncedOrganizerSearch = useDebouncedValue(organizerSearch)
  const [organizers, setOrganizers] = useState<User[]>([])
  const loadOrganizers = useCallback(async (signal: AbortSignal) => {
    const result = await api.searchOrganizers(debouncedOrganizerSearch, 20, signal)
    setOrganizers((current) => {
      const known = new Map(current.map((organizer) => [organizer.id, organizer]))
      result.items.forEach((organizer) => known.set(organizer.id, organizer))
      return [...known.values()].sort((left, right) => left.name.localeCompare(right.name))
    })
    return result
  }, [debouncedOrganizerSearch])
  const {
    data: organizerPage,
    loading: organizersLoading,
    error: organizersError,
  } = useApiResource(loadOrganizers)
  const [selected, setSelected] = useState<Record<string, string>>({})
  const [busyId, setBusyId] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const replaceRequest = (updated: BookingRequest) => {
    setData((current) => current ? {
      ...current,
      items: current.items.map((item) => item.id === updated.id ? updated : item),
    } : current)
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
      <Card className="filter-card border-0 mb-4">
        <Card.Body>
          <Form.Group controlId="booking-organizer-search">
            <Form.Label>Find an Organizer to assign</Form.Label>
            <Form.Control
              type="search"
              value={organizerSearch}
              onChange={(event) => setOrganizerSearch(event.target.value)}
              placeholder="Search by name or email"
            />
            <Form.Text className={organizersError ? 'text-danger' : undefined}>
              {organizersError
                ? organizersError
                : organizersLoading
                  ? 'Searching Organizers…'
                  : `${organizerPage?.totalCount ?? 0} matching active Organizer(s)`}
            </Form.Text>
          </Form.Group>
        </Card.Body>
      </Card>
      {loading ? (
        <LoadingState label="Loading booking requests" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : data?.items.length ? (
        <>
        <div className="d-grid gap-3">
          {data.items.map((request) => {
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
        <PaginationControls {...data} label="requests" onPageChange={setPage} />
        </>
      ) : (
        <EmptyState title="No booking requests" message="New public requests will appear here." />
      )}
    </>
  )
}
