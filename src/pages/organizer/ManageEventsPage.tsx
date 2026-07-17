import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import { api } from '../../api'
import EventForm from '../../components/events/EventForm'
import OrganizerEventTable from '../../components/organizer/OrganizerEventTable'
import ConfirmModal from '../../components/shared/ConfirmModal'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import type { EventInput, EventItem } from '../../types'

export default function ManageEventsPage() {
  const { user } = useAuth()
  const [editorOpen, setEditorOpen] = useState(false)
  const [editing, setEditing] = useState<EventItem | null>(null)
  const [deleting, setDeleting] = useState<EventItem | null>(null)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const loadEvents = useCallback(() => api.getOrganizerEvents(user!.id), [user])
  const { data: events, loading, error, reload } = useApiResource(loadEvents)

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
    try {
      await api.deleteEvent(deleting.id)
      setNotice('Event and its registrations were deleted.')
      setDeleting(null)
      await reload()
    } catch (caught) {
      setNotice(null)
      setActionError(caught instanceof Error ? caught.message : 'The event could not be deleted.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Organizer tools"
        title="Manage events"
        description="Create events, update details, review registrants, and record attendance."
        action={<Button size="lg" onClick={openCreate}>+ Create event</Button>}
      />
      {notice && <Alert variant="success" dismissible onClose={() => setNotice(null)}>{notice}</Alert>}
      {loading ? (
        <LoadingState label="Loading your events" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : events?.length ? (
        <OrganizerEventTable events={events} onEdit={openEdit} onDelete={setDeleting} />
      ) : (
        <EmptyState title="Build your first event" message="Create an event and student registrations will appear here." action={<Button onClick={openCreate}>Create event</Button>} />
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
          <Modal.Title as="h2" className="h4">{editing ? 'Edit event' : 'Create an event'}</Modal.Title>
        </Modal.Header>
        <Modal.Body className="p-4">
          <EventForm
            key={editing?.id ?? 'new-event'}
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
        message={`Deleting “${deleting?.title ?? ''}” also removes its registrations. This cannot be undone.`}
        busy={busy}
        onConfirm={() => void deleteEvent()}
        onHide={() => setDeleting(null)}
      />
    </>
  )
}
