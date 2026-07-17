import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'

interface ErrorStateProps {
  message: string
  onRetry?: () => void
}

export default function ErrorState({ message, onRetry }: ErrorStateProps) {
  return (
    <Alert variant="danger" className="d-flex flex-column flex-sm-row gap-3 align-items-sm-center">
      <div className="flex-grow-1">
        <Alert.Heading as="h2" className="h6 mb-1">
          We couldn’t load this view
        </Alert.Heading>
        <span>{message}</span>
      </div>
      {onRetry && (
        <Button variant="outline-danger" size="sm" onClick={onRetry}>
          Try again
        </Button>
      )}
    </Alert>
  )
}
