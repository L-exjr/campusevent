import { useCallback, useEffect, useState } from 'react'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { Navigate, useParams } from 'react-router-dom'
import { api } from '../../api'
import RegistrantTable from '../../components/organizer/RegistrantTable'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { useAuth } from '../../hooks/useAuth'
import { canManageEvent } from '../../utils/permissions'

export default function RegistrantsPage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [attendance, setAttendance] = useState('')
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebouncedValue(search)
  const loadData = useCallback(
    async (signal: AbortSignal) => {
      const event = await api.getManagementEvent(id)
      if (!user || !canManageEvent(user, event)) {
        return { event, registrants: null }
      }
      const attended = attendance ? attendance === 'attended' : undefined
      return { event, registrants: await api.getEventRegistrants(id, page, 50, debouncedSearch, attended, signal) }
    },
    [attendance, debouncedSearch, id, page, user],
  )
  const { data, loading, error, reload } = useApiResource(loadData)
  const registrants = data?.registrants?.items ?? []

  useEffect(() => {
    if (!data?.event.title) return
    document.title = `${data.event.title} registrants | Campus Events`
    return () => {
      document.title = 'Campus Events'
    }
  }, [data?.event.title])

  const clearFilters = () => {
    setSearch('')
    setAttendance('')
    setPage(1)
  }

  const exportCsv = async () => {
    const blob = await api.exportEventRegistrants(id)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `${data?.event.title ?? 'event'}-registrants.csv`
    link.click()
    URL.revokeObjectURL(url)
  }

  if (loading) return <LoadingState label="Loading registrants" />
  if (error || !data) return <ErrorState message={error ?? 'No data returned.'} onRetry={() => void reload()} />
  if (!user || !canManageEvent(user, data.event) || !data.registrants) {
    return <Navigate to="/unauthorized" replace />
  }

  const attendedCount = registrants.filter((registrant) => registrant.attended).length
  const backPath = user.role === 'admin' ? '/admin/events' : '/organizer/events'

  return (
    <>
      <LinkButton to={backPath} variant="link" className="px-0 text-decoration-none mb-2">← Back to events</LinkButton>
      <PageHeader
        eyebrow="Registrant list"
        title={data.event.title}
        description="Review everyone currently registered for this event."
        action={(
          <div className="d-flex flex-wrap gap-2">
            <Badge bg="primary" className="summary-badge">{data.event.registeredCount} registered</Badge>
            <Badge bg="success" className="summary-badge">{attendedCount} attended on this page</Badge>
            <Button variant="outline-primary" onClick={() => void exportCsv()}>Download CSV</Button>
          </div>
        )}
      />
      {registrants.length ? (
        <>
          <Card className="filter-card border-0 mb-4">
            <Card.Body>
              <Row className="g-3 align-items-end">
                <Col md={6}>
                  <Form.Group controlId="registrant-search">
                    <Form.Label>Search registrants</Form.Label>
                    <Form.Control
                      value={search}
                      onChange={(event) => { setSearch(event.target.value); setPage(1) }}
                      placeholder="Name or email address"
                    />
                  </Form.Group>
                </Col>
                <Col md={4}>
                  <Form.Group controlId="registrant-attendance-filter">
                    <Form.Label>Attendance status</Form.Label>
                    <Form.Select
                      value={attendance}
                      onChange={(event) => { setAttendance(event.target.value); setPage(1) }}
                    >
                      <option value="">All registrants</option>
                      <option value="attended">Attended</option>
                      <option value="not-marked">Not marked</option>
                    </Form.Select>
                  </Form.Group>
                </Col>
                <Col md={2}>
                  <Button
                    variant="light"
                    className="w-100 text-nowrap"
                    disabled={!search && !attendance}
                    onClick={clearFilters}
                  >
                    Reset
                  </Button>
                </Col>
              </Row>
            </Card.Body>
          </Card>
          {registrants.length ? (
            <>
              <RegistrantTable registrants={registrants} />
              <PaginationControls {...data.registrants} label="registrants" onPageChange={setPage} />
            </>
          ) : (
            <EmptyState
              title="No matching registrants"
              message="Try a different name, email address, or attendance status."
              action={<Button onClick={clearFilters}>Clear all filters</Button>}
            />
          )}
        </>
      ) : (
        <EmptyState title="No registrations yet" message="Student names will appear here when they register." />
      )}
    </>
  )
}
