import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Modal from 'react-bootstrap/Modal'
import Row from 'react-bootstrap/Row'
import { api } from '../../api'
import { uploadImage } from '../../api/imageStorage'
import AdminEventTable from '../../components/admin/AdminEventTable'
import TransferEventOwnershipModal from '../../components/admin/TransferEventOwnershipModal'
import EventForm from '../../components/events/EventForm'
import EventCreationWizard from '../../components/events/create-event/EventCreationWizard'
import ConfirmModal from '../../components/shared/ConfirmModal'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import NotificationToast from '../../components/shared/NotificationToast'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import type { EventInput, EventItem } from '../../types'
import { EVENT_CATEGORIES } from '../../types'

export default function AdminEventsPage() {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [editorOpen, setEditorOpen] = useState(false)
  const [editing, setEditing] = useState<EventItem | null>(null)
  const [deleting, setDeleting] = useState<EventItem | null>(null)
  const [transferring, setTransferring] = useState<EventItem | null>(null)
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebouncedValue(search)
  const loadEvents = useCallback(
    (signal: AbortSignal) => api.getAllEvents(page, 20, { search: debouncedSearch, category }, signal),
    [category, debouncedSearch, page],
  )
  const { data: eventPage, loading, error, reload } = useApiResource(loadEvents)

  const openCreate = () => {
    setEditing(null)
    setActionError(null)
    setEditorOpen(true)
  }

  const openEdit = (event: EventItem) => {
    setEditing(event)
    setActionError(null)
    setEditorOpen(true)
  }

  const saveEvent = async (input: EventInput, pendingImage: File | null = null) => {
    setBusy(true)
    setActionError(null)
    try {
      if (editing) {
        await api.updateEvent(editing.id, input)
        setNotice('Event updated successfully.')
      } else {
        const created = await api.createEvent(input)
        if (pendingImage) {
          const imageUrl = await uploadImage(pendingImage, 'event-images', created.id)
          await api.updateEvent(created.id, { ...input, imageUrl, version: created.version })
        }
        setNotice('Event created successfully.')
      }
      setEditorOpen(false)
      setEditing(null)
      await reload()
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'The event could not be saved.')
    } finally {
      setBusy(false)
    }
  }

  const deleteEvent = async () => {
    if (!deleting) return
    setBusy(true)
    setActionError(null)
    try {
      await api.deleteEvent(deleting.id)
      setNotice('Event and associated registrations were deleted.')
      setDeleting(null)
      await reload()
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'The event could not be deleted.')
    } finally {
      setBusy(false)
    }
  }

  const transferOwnership = async (organizerId: string) => {
    if (!transferring) return
    setBusy(true)
    setActionError(null)
    try {
      await api.transferEventOwnership(transferring.id, organizerId, transferring.version)
      setNotice('Event ownership transferred successfully.')
      setTransferring(null)
      await reload()
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'Event ownership could not be transferred.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="System-wide oversight"
        title="All events"
        description="Create events and review, correct, or remove any event in the system."
        action={<Button size="lg" onClick={openCreate}>+ Create event</Button>}
      />
      <NotificationToast message={notice} onClose={() => setNotice(null)} />
      {actionError && !editorOpen && <Alert variant="danger" dismissible onClose={() => setActionError(null)}>{actionError}</Alert>}
      <Card className="filter-card border-0 mb-4">
        <Card.Body>
          <Row className="g-3 align-items-end">
            <Col md={6}>
              <Form.Group controlId="admin-event-search">
                <Form.Label>Search events</Form.Label>
                <Form.Control value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="Event title or organizer" />
              </Form.Group>
            </Col>
            <Col md={4}>
              <Form.Group controlId="admin-event-category">
                <Form.Label>Category</Form.Label>
                <Form.Select value={category} onChange={(event) => { setCategory(event.target.value); setPage(1) }}>
                  <option value="">All categories</option>
                  {EVENT_CATEGORIES.map((item) => <option key={item}>{item}</option>)}
                </Form.Select>
              </Form.Group>
            </Col>
            <Col md={2}>
              <Button variant="light" className="w-100 text-nowrap" onClick={() => { setSearch(''); setCategory(''); setPage(1) }}>Reset</Button>
            </Col>
          </Row>
        </Card.Body>
      </Card>
      {loading ? (
        <LoadingState label="Loading all events" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : eventPage?.items.length ? (
        <>
          <AdminEventTable
            events={eventPage.items}
            onEdit={openEdit}
            onTransfer={(event) => { setActionError(null); setTransferring(event) }}
            onDelete={setDeleting}
          />
          <PaginationControls {...eventPage} label="events" onPageChange={setPage} />
        </>
      ) : (
        <EmptyState
          title={search || category ? 'No matching events' : 'Create the first event'}
          message={search || category ? 'Try a different title, organizer, or category.' : 'Create an event to make it available to Students.'}
          action={!search && !category ? <Button onClick={openCreate}>Create event</Button> : undefined}
        />
      )}
      <Modal
        show={editorOpen}
        onHide={() => {
          if (!busy) setEditorOpen(false)
        }}
        backdrop={busy ? 'static' : true}
        keyboard={!busy}
        size="lg"
        centered
      >
        <Modal.Header closeButton={!busy}>
          <Modal.Title as="h2" className="h4">
            {editing ? 'Edit system event' : 'Create an event'}
          </Modal.Title>
        </Modal.Header>
        <Modal.Body className="p-4">
          {editing ? (
            <EventForm
              key={editing.id}
              event={editing}
              busy={busy}
              error={actionError}
              submitLabel="Save changes"
              onSubmit={saveEvent}
              onCancel={() => setEditorOpen(false)}
            />
          ) : (
            <EventCreationWizard
              key="admin-new-event"
              busy={busy}
              error={actionError}
              onSubmit={saveEvent}
              onCancel={() => setEditorOpen(false)}
            />
          )}
        </Modal.Body>
      </Modal>
      <ConfirmModal
        show={Boolean(deleting)}
        title="Delete this event?"
        message={`“${deleting?.title ?? ''}” and all associated registrations will be permanently removed.`}
        busy={busy}
        onConfirm={() => void deleteEvent()}
        onHide={() => setDeleting(null)}
      />
      {transferring && (
        <TransferEventOwnershipModal
          event={transferring}
          busy={busy}
          error={actionError}
          onTransfer={(organizerId) => void transferOwnership(organizerId)}
          onHide={() => { if (!busy) { setTransferring(null); setActionError(null) } }}
        />
      )}
    </>
  )
}
