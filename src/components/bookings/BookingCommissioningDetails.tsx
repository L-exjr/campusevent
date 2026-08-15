import { Alert, Badge } from 'react-bootstrap'
import type { BookingRequest } from '../../types'

export default function BookingCommissioningDetails({ request }: { request: BookingRequest }) {
  const tools = [request.requiresRegistration && 'Registration', request.requiresTicketing && 'Ticketing', request.requiresVoting && 'Voting'].filter(Boolean)
  return <>
    <dl className="row small">
      <dt className="col-sm-4">Category</dt><dd className="col-sm-8">{request.eventCategory || 'Not specified'}</dd>
      <dt className="col-sm-4">Expected range</dt><dd className="col-sm-8">{new Date(request.proposedDate).toLocaleString()}{request.expectedEndDate ? ` – ${new Date(request.expectedEndDate).toLocaleString()}` : ''}</dd>
      <dt className="col-sm-4">Budget</dt><dd className="col-sm-8">{request.budgetMinimumMinor != null || request.budgetMaximumMinor != null ? `GHS ${((request.budgetMinimumMinor ?? 0) / 100).toLocaleString()} – ${request.budgetMaximumMinor == null ? 'open' : (request.budgetMaximumMinor / 100).toLocaleString()}` : 'Not specified'}</dd>
      <dt className="col-sm-4">Required tools</dt><dd className="col-sm-8">{tools.length ? tools.map(tool => <Badge bg="light" text="dark" className="me-1" key={String(tool)}>{tool}</Badge>) : 'None specified'}</dd>
      {request.referenceLinks && <><dt className="col-sm-4">References</dt><dd className="col-sm-8" style={{ whiteSpace: 'pre-wrap' }}>{request.referenceLinks}</dd></>}
    </dl>
    {request.quote && <Alert variant="primary"><strong>Quote: GHS {(request.quote.proposedFeeMinor / 100).toLocaleString(undefined, { minimumFractionDigits: 2 })}</strong><br />{request.quote.proposedTimeline}<br />{request.quote.message}</Alert>}
    <details><summary className="fw-semibold">Status history ({request.statusHistory?.length ?? 0})</summary><ol className="mt-2">{request.statusHistory?.map(item => <li key={item.id}>{item.status} · {new Date(item.createdAt).toLocaleString()}{item.note ? ` — ${item.note}` : ''}</li>)}</ol></details>
  </>
}
