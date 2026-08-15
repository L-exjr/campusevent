import { useCallback } from 'react'
import { Alert, Badge, Card } from 'react-bootstrap'
import { useParams, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import ErrorState from '../components/shared/ErrorState'
import LoadingState from '../components/shared/LoadingState'
import { useApiResource } from '../hooks/useApiResource'

export default function BookingRequestTrackingPage() {
  const { id = '' } = useParams()
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const load = useCallback(() => api.trackBookingRequest(id, token), [id, token])
  const { data, loading, error, reload } = useApiResource(load)
  if (loading) return <LoadingState label="Loading request status" />
  if (error || !data) return <ErrorState message={error ?? 'Request not found.'} onRetry={() => void reload()} />
  return <Card className="border-0 mx-auto" style={{ maxWidth: 820 }}><Card.Body className="p-4 p-lg-5">
    <p className="eyebrow">Private request tracking</p>
    <div className="d-flex justify-content-between gap-3"><h1>{data.organizationName}</h1><Badge className="align-self-start">{data.status}</Badge></div>
    <p className="lead">{data.eventType} · {data.estimatedAttendance} expected attendees</p>
    {data.quote && <Alert variant="primary"><h2 className="h5">Organizer quote</h2><p className="mb-1"><strong>GHS {(data.quote.proposedFeeMinor / 100).toLocaleString(undefined, { minimumFractionDigits: 2 })}</strong> · {data.quote.proposedTimeline}</p><p className="mb-0">{data.quote.message}</p></Alert>}
    <h2 className="h4 mt-4">Status history</h2>
    <ol>{data.statusHistory.map(item => <li key={item.id} className="mb-3"><strong>{item.status}</strong> — {new Date(item.createdAt).toLocaleString()}<br /><span className="text-secondary">{item.note}</span></li>)}</ol>
  </Card.Body></Card>
}
