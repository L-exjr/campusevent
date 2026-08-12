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
      aria-busy="true"
    >
      <div className="loading-state__panel">
        <div className="d-flex align-items-center gap-3">
          <Spinner animation="border" variant="primary" />
          <span>{label}…</span>
        </div>
        {!fullPage && (
          <div className="loading-skeleton" aria-hidden="true">
            <span className="loading-skeleton__line loading-skeleton__line--short" />
            <span className="loading-skeleton__line" />
            <span className="loading-skeleton__line loading-skeleton__line--medium" />
          </div>
        )}
      </div>
    </div>
  )
}
