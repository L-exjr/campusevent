import { useCallback, useState } from 'react'
import Badge from 'react-bootstrap/Badge'
import Alert from 'react-bootstrap/Alert'
import Card from 'react-bootstrap/Card'
import { Navigate, useParams } from 'react-router-dom'
import { api } from '../../api'
import AttendanceChecklist from '../../components/organizer/AttendanceChecklist'
import QrTicketScanner from '../../components/organizer/QrTicketScanner'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { canManageEvent } from '../../utils/permissions'

export default function AttendancePage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const [page, setPage] = useState(1)
  const [scanBusy, setScanBusy] = useState(false)
  const [scanNotice, setScanNotice] = useState<string | null>(null)
  const [scanError, setScanError] = useState<string | null>(null)
  const loadData = useCallback(
    async () => ({
      event: await api.getManagementEvent(id),
      registrants: await api.getEventRegistrants(id, page, 50),
    }),
    [id, page],
  )
  const { data, loading, error, reload } = useApiResource(loadData)

  if (loading) return <LoadingState label="Loading attendance sheet" />
  if (error || !data) return <ErrorState message={error ?? 'No data returned.'} onRetry={() => void reload()} />
  if (!user || !canManageEvent(user, data.event)) return <Navigate to="/unauthorized" replace />

  const checkIn = async (token: string) => {
    setScanBusy(true)
    setScanNotice(null)
    setScanError(null)
    try {
      const result = await api.checkInTicket(id, token)
      setScanNotice(`${result.studentName} checked in successfully.`)
      await reload()
    } catch (caught) {
      setScanError(caught instanceof Error ? caught.message : 'The ticket could not be checked in.')
    } finally {
      setScanBusy(false)
    }
  }

  return (
    <>
      <LinkButton to="/organizer/events" variant="link" className="px-0 text-decoration-none mb-2">← Back to events</LinkButton>
      <PageHeader
        eyebrow="Attendance"
        title={data.event.title}
        description="Check each student who attended, then save the completed list."
        action={<Badge bg="success" className="summary-badge">{data.registrants.items.filter((item) => item.attended).length} present on this page</Badge>}
      />
      {scanNotice && <Alert variant="success" dismissible onClose={() => setScanNotice(null)}>{scanNotice}</Alert>}
      {scanError && <Alert variant="danger" dismissible onClose={() => setScanError(null)}>{scanError}</Alert>}
      <Card className="detail-card border-0 mb-4">
        <Card.Body className="p-3 p-md-4">
          <h2 className="h4">Scan attendee ticket</h2>
          <p className="text-secondary">Use the rear camera at the entrance. Each signed ticket can check in only once.</p>
          <QrTicketScanner busy={scanBusy} onToken={checkIn} />
        </Card.Body>
      </Card>
      {data.registrants.items.length ? (
        <>
        <AttendanceChecklist
          key={data.registrants.items.map((item) => `${item.registrationId}-${item.attended}`).join('|')}
          eventId={id}
          registrants={data.registrants.items}
          onSaved={reload}
        />
        <PaginationControls {...data.registrants} label="registrants" onPageChange={setPage} />
        </>
      ) : (
        <EmptyState title="No one to mark yet" message="Attendance becomes available after students register." />
      )}
    </>
  )
}
