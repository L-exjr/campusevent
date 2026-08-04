import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import Modal from 'react-bootstrap/Modal'
import { api } from '../../api'
import { useApiResource } from '../../hooks/useApiResource'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import type { EventItem } from '../../types'

interface TransferEventOwnershipModalProps {
  event: EventItem
  busy: boolean
  error: string | null
  onTransfer: (organizerId: string) => void
  onHide: () => void
}

export default function TransferEventOwnershipModal({
  event,
  busy,
  error,
  onTransfer,
  onHide,
}: TransferEventOwnershipModalProps) {
  const [search, setSearch] = useState('')
  const [selectedOrganizerId, setSelectedOrganizerId] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const load = useCallback(
    (signal: AbortSignal) => api.searchOrganizers(debouncedSearch, 20, signal),
    [debouncedSearch],
  )
  const { data, loading, error: searchError } = useApiResource(load)
  const organizers = data?.items.filter((organizer) => organizer.id !== event.organizerId) ?? []

  return (
    <Modal show centered onHide={onHide} backdrop={busy ? 'static' : true} keyboard={!busy}>
      <Modal.Header closeButton={!busy}>
        <Modal.Title as="h2" className="h4">Transfer event ownership</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <p>
          Move <strong>{event.title}</strong> from {event.organizerName} to another active Organizer.
        </p>
        {error && <Alert variant="danger">{error}</Alert>}
        <Form.Group controlId="transfer-organizer-search" className="mb-3">
          <Form.Label>Search Organizers</Form.Label>
          <Form.Control
            type="search"
            value={search}
            disabled={busy}
            onChange={(changeEvent) => setSearch(changeEvent.target.value)}
            placeholder="Name or email"
          />
        </Form.Group>
        <Form.Group controlId="transfer-organizer">
          <Form.Label>New owner</Form.Label>
          <Form.Select
            value={selectedOrganizerId}
            disabled={busy || loading}
            onChange={(changeEvent) => setSelectedOrganizerId(changeEvent.target.value)}
          >
            <option value="">{loading ? 'Searching…' : 'Choose an Organizer'}</option>
            {organizers.map((organizer) => (
              <option key={organizer.id} value={organizer.id}>
                {organizer.name} ({organizer.email})
              </option>
            ))}
          </Form.Select>
          {searchError && <Form.Text className="text-danger">{searchError}</Form.Text>}
          {!loading && !searchError && data && organizers.length === 0 && (
            <Form.Text>No other active Organizers match this search.</Form.Text>
          )}
        </Form.Group>
      </Modal.Body>
      <Modal.Footer>
        <Button variant="outline-secondary" disabled={busy} onClick={onHide}>Cancel</Button>
        <Button
          disabled={busy || !selectedOrganizerId}
          onClick={() => onTransfer(selectedOrganizerId)}
        >
          {busy ? 'Transferring…' : 'Transfer ownership'}
        </Button>
      </Modal.Footer>
    </Modal>
  )
}
