import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'

interface ConfirmModalProps {
  show: boolean
  title: string
  message: string
  confirmLabel?: string
  confirmVariant?: 'danger' | 'primary' | 'success' | 'warning'
  busy?: boolean
  onConfirm: () => void
  onHide: () => void
}

export default function ConfirmModal({
  show,
  title,
  message,
  confirmLabel = 'Delete',
  confirmVariant = 'danger',
  busy = false,
  onConfirm,
  onHide,
}: ConfirmModalProps) {
  return (
    <Modal
      show={show}
      onHide={() => {
        if (!busy) onHide()
      }}
      backdrop={busy ? 'static' : true}
      keyboard={!busy}
      centered
    >
      <Modal.Header closeButton={!busy}>
        <Modal.Title as="h2" className="h5">
          {title}
        </Modal.Title>
      </Modal.Header>
      <Modal.Body>{message}</Modal.Body>
      <Modal.Footer>
        <Button variant="light" onClick={onHide} disabled={busy}>
          Cancel
        </Button>
        <Button variant={confirmVariant} onClick={onConfirm} disabled={busy}>
          {busy ? 'Working…' : confirmLabel}
        </Button>
      </Modal.Footer>
    </Modal>
  )
}
