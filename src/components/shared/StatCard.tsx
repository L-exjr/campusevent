import Card from 'react-bootstrap/Card'

interface StatCardProps {
  label: string
  value: string | number
  note?: string
  tone?: 'primary' | 'success' | 'warning' | 'ink'
}

export default function StatCard({
  label,
  value,
  note,
  tone = 'primary',
}: StatCardProps) {
  return (
    <Card className={`stat-card stat-card--${tone} h-100 border-0`}>
      <Card.Body>
        <Card.Text className="text-secondary small fw-semibold mb-2">{label}</Card.Text>
        <Card.Title as="p" className="stat-card__value mb-1">
          {value}
        </Card.Title>
        {note && <Card.Text className="small text-secondary mb-0">{note}</Card.Text>}
      </Card.Body>
    </Card>
  )
}
