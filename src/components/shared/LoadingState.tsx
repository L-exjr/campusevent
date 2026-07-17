import Spinner from 'react-bootstrap/Spinner'

interface LoadingStateProps {
  label?: string
  fullPage?: boolean
}

export default function LoadingState({
  label = 'Loading data',
  fullPage = false,
}: LoadingStateProps) {
  return (
    <div
      className={`loading-state ${fullPage ? 'loading-state--full' : ''}`}
      role="status"
      aria-live="polite"
    >
      <Spinner animation="border" variant="primary" />
      <span>{label}…</span>
    </div>
  )
}
