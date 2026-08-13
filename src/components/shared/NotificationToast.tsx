import { useEffect } from 'react'
import ToastContainer from 'react-bootstrap/ToastContainer'

interface NotificationToastProps {
  message: string | null
  onClose: () => void
  title?: string
}

export default function NotificationToast({
  message,
  onClose,
  title = 'Update complete',
}: NotificationToastProps) {
  useEffect(() => {
    if (!message) return undefined
    const timeout = window.setTimeout(onClose, 5000)
    return () => window.clearTimeout(timeout)
  }, [message, onClose])

  return (
    <ToastContainer className="notification-toast-container" position="top-end">
      {message && (
        <div
          className="toast notification-toast show"
          role="status"
          aria-live="polite"
          aria-atomic="true"
        >
          <div className="toast-header">
            <span className="notification-toast__mark" aria-hidden="true">
              ✓
            </span>
            <strong className="me-auto">{title}</strong>
            <button type="button" className="btn-close" aria-label="Close" onClick={onClose} />
          </div>
          <div className="toast-body">{message}</div>
        </div>
      )}
    </ToastContainer>
  )
}
