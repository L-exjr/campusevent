import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Modal from 'react-bootstrap/Modal'
import Row from 'react-bootstrap/Row'
import { api } from '../../api'
import AdminEventTable from '../../components/admin/AdminEventTable'
import EventForm from '../../components/events/EventForm'
import ConfirmModal from '../../components/shared/ConfirmModal'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import type { EventInput, EventItem } from '../../types'
import { EVENT_CATEGORIES } from '../../types'

export default function AdminEventsPage() {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [editorOpen, setEditorOpen] = useState(false)
  const [editing, setEditing] = useState<EventItem | null>(null)
  const [deleting, setDeleting] = useState<EventItem | null>(null)
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const loadEvents = useCallback(() => api.getAllEvents(), [])
  const { data: events, loading, error, reload } = useApiResource(loadEvents)

  const filteredEvents = (events ?? []).filter((event) => {
    const query = search.trim().toLowerCase()
    return (
      (!query || event.title.toLowerCase().includes(query) || event.organizerName.toLowerCase().includes(query)) &&
      (!category || event.category === category)
    )
  })

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

  const saveEvent = async (input: EventInput) => {
    setBusy(true)
    setActionError(null)
    try {
      if (editing) {
        await api.updateEvent(editing.id, input)
        setNotice('Event updated successfully.')
      } else {
        await api.createEvent(input)
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

  return (
    <>
      <PageHeader
        eyebrow="System-wide oversight"
        title="All events"
        description="Create events and review, correct, or remove any event in the system."
        action={<Button size="lg" onClick={openCreate}>+ Create event</Button>}
      />
      {notice && <Alert variant="success" dismissible onClose={() => setNotice(null)}>{notice}</Alert>}
      {actionError && !editorOpen && <Alert variant="danger" dismissible onClose={() => setActionError(null)}>{actionError}</Alert>}
      <Card className="filter-card border-0 mb-4">
        <Card.Body>
          <Row className="g-3 align-items-end">
            <Col md={7}>
              <Form.Group controlId="admin-event-search">
                <Form.Label>Search events</Form.Label>
                <Form.Control value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Event title or organizer" />
              </Form.Group>
            </Col>
            <Col md={4}>
              <Form.Group controlId="admin-event-category">
                <Form.Label>Category</Form.Label>
                <Form.Select value={category} onChange={(event) => setCategory(event.target.value)}>
                  <option value="">All categories</option>
                  {EVENT_CATEGORIES.map((item) => <option key={item}>{item}</option>)}
                </Form.Select>
              </Form.Group>
            </Col>
            <Col md={1}>
              <Button variant="light" className="w-100" onClick={() => { setSearch(''); setCategory('') }}>Reset</Button>
            </Col>
          </Row>
        </Card.Body>
      </Card>
      {loading ? (
        <LoadingState label="Loading all events" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : filteredEvents.length ? (
        <AdminEventTable events={filteredEvents} onEdit={openEdit} onDelete={setDeleting} />
      ) : (
        <EmptyState
          title={events?.length ? 'No matching events' : 'Create the first event'}
          message={events?.length ? 'Try a different title, organizer, or category.' : 'Create an event to make it available to Students.'}
          action={!events?.length ? <Button onClick={openCreate}>Create event</Button> : undefined}
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
          <EventForm
            key={editing?.id ?? 'admin-new-event'}
            event={editing}
            busy={busy}
            error={actionError}
            submitLabel={editing ? 'Save changes' : 'Create event'}
            onSubmit={saveEvent}
            onCancel={() => setEditorOpen(false)}
          />
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
    </>
  )
}
