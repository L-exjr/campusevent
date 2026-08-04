import Button from 'react-bootstrap/Button'

interface PaginationControlsProps {
  page: number
  totalPages: number
  totalCount: number
  label: string
  onPageChange: (page: number) => void
}

export default function PaginationControls({
  page,
  totalPages,
  totalCount,
  label,
  onPageChange,
}: PaginationControlsProps) {
  if (totalPages <= 1) return null
  return (
    <div className="d-flex justify-content-between align-items-center gap-3 mt-3">
      <Button
        variant="outline-secondary"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        Previous
      </Button>
      <span className="small text-secondary text-center">
        Page {page} of {totalPages} · {totalCount} {label}
      </span>
      <Button
        variant="outline-secondary"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Next
      </Button>
    </div>
  )
}
