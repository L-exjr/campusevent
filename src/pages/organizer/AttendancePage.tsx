import { useCallback } from 'react'
import Badge from 'react-bootstrap/Badge'
import { Navigate, useParams } from 'react-router-dom'
import { api } from '../../api'
import AttendanceChecklist from '../../components/organizer/AttendanceChecklist'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { canManageEvent } from '../../utils/permissions'

export default function AttendancePage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const loadData = useCallback(
    async () => ({
      event: await api.getEvent(id),
      registrants: await api.getEventRegistrants(id),
    }),
    [id],
  )
  const { data, loading, error, reload } = useApiResource(loadData)

  if (loading) return <LoadingState label="Loading attendance sheet" />
  if (error || !data) return <ErrorState message={error ?? 'No data returned.'} onRetry={() => void reload()} />
  if (!user || !canManageEvent(user, data.event)) return <Navigate to="/unauthorized" replace />

  return (
    <>
      <LinkButton to="/organizer/events" variant="link" className="px-0 text-decoration-none mb-2">← Back to events</LinkButton>
      <PageHeader
        eyebrow="Attendance"
        title={data.event.title}
        description="Check each student who attended, then save the completed list."
        action={<Badge bg="success" className="summary-badge">{data.registrants.filter((item) => item.attended).length} present</Badge>}
      />
      {data.registrants.length ? (
        <AttendanceChecklist
          key={data.registrants.map((item) => `${item.registrationId}-${item.attended}`).join('|')}
          eventId={id}
          registrants={data.registrants}
          onSaved={reload}
        />
      ) : (
        <EmptyState title="No one to mark yet" message="Attendance becomes available after students register." />
      )}
    </>
  )
}
