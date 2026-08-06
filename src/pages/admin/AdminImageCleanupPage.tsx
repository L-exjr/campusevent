import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Table from 'react-bootstrap/Table'
import { api } from '../../api'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { formatDateTime } from '../../utils/formatters'

export default function AdminImageCleanupPage() {
  const [page, setPage] = useState(1)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const load = useCallback(() => api.getFailedImageCleanups(page, 20), [page])
  const { data, loading, error, reload } = useApiResource(load)

  const retry = async (id: string) => {
    setBusyId(id); setNotice(null); setActionError(null)
    try {
      await api.retryFailedImageCleanup(id)
      setNotice('The image was returned to the cleanup queue.')
      await reload()
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'The image could not be retried.')
    } finally { setBusyId(null) }
  }

  return <>
    <PageHeader eyebrow="Storage operations" title="Failed image cleanup" description="Inspect and retry orphaned files that exhausted automatic deletion attempts." />
    {notice && <Alert variant="success">{notice}</Alert>}
    {actionError && <Alert variant="danger">{actionError}</Alert>}
    {loading ? <LoadingState label="Loading failed image cleanup" /> : error ? (
      <ErrorState message={error} onRetry={() => void reload()} />
    ) : data?.items.length ? <>
      <div className="table-shell"><Table responsive hover className="align-middle mb-0">
        <thead><tr><th>Object</th><th>Created</th><th>Attempts</th><th>Last error</th><th className="text-end">Action</th></tr></thead>
        <tbody>{data.items.map((item) => <tr key={item.id}>
          <td><div className="fw-semibold">{item.kind}</div><small className="text-secondary text-break">{item.bucket}/{item.objectKey}</small></td>
          <td>{formatDateTime(item.createdAt)}</td>
          <td>{item.deleteAttemptCount} this cycle · {item.lifetimeDeleteAttemptCount} lifetime</td>
          <td>{item.lastError ?? 'No provider error recorded'}</td>
          <td className="text-end"><Button size="sm" disabled={busyId === item.id} onClick={() => void retry(item.id)}>{busyId === item.id ? 'Retrying…' : 'Retry'}</Button></td>
        </tr>)}</tbody>
      </Table></div>
      <PaginationControls {...data} label="failed image cleanup items" onPageChange={setPage} />
    </> : <EmptyState title="No failed image cleanup" message="Files that exhaust automatic deletion retries will appear here." />}
  </>
}
