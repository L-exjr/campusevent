import { useCallback, useState } from 'react'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Table from 'react-bootstrap/Table'
import { api } from '../../api'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { formatDateTime } from '../../utils/formatters'

export default function AdminAuditLogsPage() {
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebouncedValue(search)
  const load = useCallback(
    (signal: AbortSignal) => api.getAdminAuditLogs(debouncedSearch, page, 20, signal),
    [debouncedSearch, page],
  )
  const { data, loading, error, reload } = useApiResource(load)
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)

  const exportCsv = async () => {
    setExporting(true); setExportError(null)
    try {
      const blob = await api.exportAdminAuditLogs()
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url; link.download = 'admin-audit.csv'; link.click()
      URL.revokeObjectURL(url)
    } catch (caught) {
      setExportError(caught instanceof Error ? caught.message : 'The audit export failed.')
    } finally { setExporting(false) }
  }

  return (
    <>
      <PageHeader
        eyebrow="Administrative accountability"
        title="Audit log"
        description="Review security-sensitive actions performed by system administrators."
      />
      {exportError && <p className="text-danger" role="alert">{exportError}</p>}
      <Card className="filter-card border-0 mb-4">
        <Card.Body>
          <Form.Group controlId="audit-search">
            <Form.Label>Search audit records</Form.Label>
            <Form.Control
              type="search"
              value={search}
              onChange={(event) => { setSearch(event.target.value); setPage(1) }}
              placeholder="Administrator, action, or target"
            />
          </Form.Group>
          <Button className="mt-3" variant="outline-primary" disabled={exporting} onClick={() => void exportCsv()}>
            {exporting ? 'Exporting…' : 'Export last 30 days'}
          </Button>
        </Card.Body>
      </Card>
      {loading ? (
        <LoadingState label="Loading audit records" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : data?.items.length ? (
        <>
          <div className="table-shell">
            <Table responsive hover className="align-middle mb-0">
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Administrator</th>
                  <th>Action</th>
                  <th>Target</th>
                  <th>Details</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((log) => (
                  <tr key={log.id}>
                    <td>{formatDateTime(log.createdAt)}</td>
                    <td>{log.actorName}</td>
                    <td className="fw-semibold">{log.action}</td>
                    <td>{log.targetType} · {log.targetId}</td>
                    <td><code className="small">{log.detailsJson}</code></td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </div>
          <PaginationControls {...data} label="audit records" onPageChange={setPage} />
        </>
      ) : (
        <EmptyState
          title="No audit records"
          message={search ? 'No administrative actions match this search.' : 'Administrative actions will appear here.'}
        />
      )}
    </>
  )
}
