import Card from 'react-bootstrap/Card'
import type { ReactNode } from 'react'

interface EmptyStateProps {
  title: string
  message: string
  action?: ReactNode
}

export default function EmptyState({ title, message, action }: EmptyStateProps) {
  return (
    <Card className="empty-state border-0">
      <Card.Body className="text-center py-5 px-4">
        <div className="empty-state__mark" aria-hidden="true">
          ✦
        </div>
        <Card.Title as="h2" className="h5 mt-3">
          {title}
        </Card.Title>
        <Card.Text className="text-secondary mx-auto">{message}</Card.Text>
        {action}
      </Card.Body>
    </Card>
  )
}
