import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
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

export default function AdminEmailOutboxPage() {
  const [page, setPage] = useState(1)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const load = useCallback(() => api.getFailedEmails(page, 20), [page])
  const { data, loading, error, reload } = useApiResource(load)

  const retry = async (id: string) => {
    setBusyId(id)
    setNotice(null)
    setActionError(null)
    try {
      await api.retryFailedEmail(id)
      setNotice('The email was returned to the delivery queue.')
      await reload()
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'The email could not be retried.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Delivery operations"
        title="Failed emails"
        description="Inspect exhausted delivery attempts and retry messages that remain safe and valid."
      />
      {notice && <Alert variant="success">{notice}</Alert>}
      {actionError && <Alert variant="danger">{actionError}</Alert>}
      {loading ? (
        <LoadingState label="Loading failed emails" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : data?.items.length ? (
        <>
          <div className="table-shell">
            <Table responsive hover className="align-middle mb-0">
              <thead>
                <tr>
                  <th>Message</th>
                  <th>Created</th>
                  <th>Attempts</th>
                  <th>Last error</th>
                  <th className="text-end">Action</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((message) => (
                  <tr key={message.id}>
                    <td>
                      <div className="fw-semibold">{message.kind}</div>
                      <small className="text-secondary">{message.aggregateId}</small>
                    </td>
                    <td>{formatDateTime(message.createdAt)}</td>
                    <td>{message.attemptCount}</td>
                    <td>{message.lastError ?? 'No provider error recorded'}</td>
                    <td className="text-end">
                      {message.canRetry ? (
                        <Button
                          size="sm"
                          disabled={busyId === message.id}
                          onClick={() => void retry(message.id)}
                        >
                          {busyId === message.id ? 'Retrying…' : 'Retry'}
                        </Button>
                      ) : (
                        <Badge bg="light" text="dark">Regenerate required</Badge>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </div>
          <PaginationControls {...data} label="failed emails" onPageChange={setPage} />
        </>
      ) : (
        <EmptyState
          title="No failed emails"
          message="Messages that exhaust automatic delivery retries will appear here."
        />
      )}
    </>
  )
}
